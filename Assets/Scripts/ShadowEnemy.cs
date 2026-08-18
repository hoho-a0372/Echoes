using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

interface IRevealable
{
    void Reveal(float duration);
}


public class ShadowEnemy : MonoBehaviour, IRevealable
{
    // ChasePlayer (triggered by a ping hit) actively tracks the player's live
    // position for a fixed duration. ChaseDecoy (triggered by a decoy) paths to
    // the decoy's fixed drop point instead - decoys exist specifically to pull
    // aggro away from the player, so they redirect the chase target rather than
    // just being "another noise" of the same kind.
    enum State { Wander, ChasePlayer, ChaseDecoy, ReturnToStart }

    [SerializeField] float wanderRadius = 5f;
    [SerializeField] float wanderInterval = 3f;
    [SerializeField] float hearingRadius = 8f;
    [SerializeField] float chasePlayerDuration = 3f;
    [SerializeField] float chaseTimeout = 4f;

    // Day 8.3 - Wander reinforcement tuning (feel-based, logged in the Day 8
    // checklist entry). wanderPointSeparationFactor is relative to
    // wanderRadius (not an absolute distance) so it scales sensibly across
    // this project's very different per-stage wanderRadius values (2f-6f).
    [SerializeField] float wanderPointSeparationFactor = 0.4f;
    [SerializeField] float proximityHearingMultiplier = 1.4f;
    [SerializeField] float proximityHearingTriggerFactor = 2f;
    [SerializeField] float idleLookChance = 0.4f;
    [SerializeField] float idleLookMinDuration = 0.5f;
    [SerializeField] float idleLookMaxDuration = 1.5f;

    // Day 8.4 - tiered visibility tuning. Both are fixed values, not
    // dynamically computed/animated - see the "fixed, not adjusted" revision
    // in the Day 8 checklist entry.
    [SerializeField] float dimAlpha = 0.35f;

    const int WanderHistorySize = 3;
    const int WanderResampleAttempts = 5;
    const float HearingCheckInterval = 0.3f;
    
    Coroutine revealCoroutine;

    NavMeshAgent agent;
    SpriteRenderer sr;
    Color baseColor;
    State state = State.Wander;
    Vector3 spawnPosition;
    float nextWanderTime;
    float chaseStartTime;

    GameObject player;

    readonly List<Vector3> wanderHistory = new List<Vector3>(WanderHistorySize);
    bool isIdleLooking;
    Coroutine idleLookCoroutine;

    float hearingMultiplier = 1f;
    float nextHearingCheckTime;


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
            baseColor = sr.color;
            SetAlpha(dimAlpha);
        }

        // The installed com.unity.ai.navigation version has no "rotate to XY"
        // baking option, so the NavMesh is baked in the standard XZ plane while
        // this is a 2D XY game - agent.updatePosition is turned off and this
        // script does the game(x,y) <-> nav(x,0,z) conversion itself every frame
        // (see GameToNav/NavToGame and the Update() sync below).
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.updatePosition = false;
        }
    }

    void Start()
    {
        spawnPosition = transform.position;
        if (agent != null)
        {
            agent.Warp(GameToNav(transform.position));
        }

        player = GameObject.FindWithTag("Player");
    }

    static Vector3 GameToNav(Vector2 gamePos) => new Vector3(gamePos.x, 0f, gamePos.y);
    static Vector2 NavToGame(Vector3 navPos) => new Vector2(navPos.x, navPos.z);

    void OnEnable()
    {
        PingProjectile.OnPingHit += HandlePingNoise;
        DecoyObject.OnDecoySpawned += HandleDecoyNoise;
    }

    void OnDisable()
    {
        PingProjectile.OnPingHit -= HandlePingNoise;
        DecoyObject.OnDecoySpawned -= HandleDecoyNoise;
    }

    bool InHearingRange(Vector2 position, float noiseRadius)
    {
        float distance = Vector2.Distance(transform.position, position);
        // Effective hearing distance for a given sound is capped by BOTH how far
        // that particular sound carries (noiseRadius) and this enemy's own
        // hearing capability (hearingRadius, boosted by hearingMultiplier while
        // Wandering near the player - Day 8.3) - whichever is smaller wins.
        float effectiveRadius = Mathf.Min(noiseRadius, hearingRadius * hearingMultiplier);
        return distance <= effectiveRadius;
    }

    void HandlePingNoise(Vector2 position, float noiseRadius)
    {
        if (InHearingRange(position, noiseRadius))
        {
            EnterChasePlayer();
        }
    }

    void HandleDecoyNoise(Vector2 position, float noiseRadius)
    {
        // A decoy always redirects aggro if heard, even mid-chase of the player -
        // that's the entire point of throwing one.
        if (InHearingRange(position, noiseRadius))
        {
            EnterChaseDecoy(position);
        }
    }

    void EnterChasePlayer()
    {
        CancelIdleLook();
        state = State.ChasePlayer;
        chaseStartTime = Time.time;
    }

    void EnterChaseDecoy(Vector2 decoyPosition)
    {
        CancelIdleLook();
        state = State.ChaseDecoy;
        chaseStartTime = Time.time;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(GameToNav(decoyPosition));
        }
    }

    void EnterReturnToStart()
    {
        CancelIdleLook();
        state = State.ReturnToStart;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(GameToNav(spawnPosition));
        }
    }

    void EnterWander()
    {
        state = State.Wander;
        nextWanderTime = Time.time;
        hearingMultiplier = 1f;
        nextHearingCheckTime = Time.time;
    }

    void Update()
    {
        switch (state)
        {
            case State.Wander:
                UpdateWander();
                TickProximityHearing();
                break;
            case State.ChasePlayer:
                UpdateChasePlayer();
                break;
            case State.ChaseDecoy:
                UpdateChaseDecoy();
                break;
            case State.ReturnToStart:
                UpdateReturn();
                break;
        }

        // agent.updatePosition is off (see Awake) so this project can keep the
        // NavMesh baked in its native XZ plane while the game itself is XY -
        // pull the agent's computed position back into game space every frame.
        if (agent != null && agent.isOnNavMesh)
        {
            transform.position = NavToGame(agent.nextPosition);
        }
    }

    void UpdateWander()
    {
        if (isIdleLooking) return; // the idle-look coroutine owns state until it finishes
        if (Time.time < nextWanderTime) return;

        Vector3 navTarget = PickWanderPoint();
        nextWanderTime = Time.time + wanderInterval;

        // Day 8.3 - "more frequent stop/turn behavior": an occasional brief
        // pause-and-turn before actually moving, so wandering reads as "alive"
        // rather than a shape sliding between points.
        if (Random.value < idleLookChance)
        {
            idleLookCoroutine = StartCoroutine(IdleLookThenMove(navTarget));
        }
        else
        {
            MoveTo(navTarget);
        }
    }

    // Day 8.3 - "more waypoints, less regular paths": samples a random point
    // in wanderRadius, snaps it onto the actual NavMesh (upgrade from the
    // previous unvalidated Random.insideUnitCircle target), and rejects
    // candidates too close to the last WanderHistorySize points actually
    // visited so patrol patterns don't become memorizable within one
    // playthrough. Falls back to spawnPosition if sampling never finds a
    // valid, sufficiently-different point within WanderResampleAttempts tries.
    Vector3 PickWanderPoint()
    {
        Vector3 chosen = GameToNav(spawnPosition);
        float minSeparation = wanderRadius * wanderPointSeparationFactor;

        for (int attempt = 0; attempt < WanderResampleAttempts; attempt++)
        {
            Vector2 candidate2D = (Vector2)spawnPosition + Random.insideUnitCircle * wanderRadius;
            Vector3 candidateNav = GameToNav(candidate2D);

            if (!NavMesh.SamplePosition(candidateNav, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                continue;
            }

            bool tooCloseToHistory = false;
            foreach (Vector3 previous in wanderHistory)
            {
                if (Vector3.Distance(hit.position, previous) < minSeparation)
                {
                    tooCloseToHistory = true;
                    break;
                }
            }
            if (tooCloseToHistory) continue;

            chosen = hit.position;
            break;
        }

        wanderHistory.Add(chosen);
        if (wanderHistory.Count > WanderHistorySize)
        {
            wanderHistory.RemoveAt(0);
        }

        return chosen;
    }

    void MoveTo(Vector3 navTarget)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(navTarget);
        }
    }

    IEnumerator IdleLookThenMove(Vector3 navTarget)
    {
        isIdleLooking = true;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

        // No directional sprite system in this project yet - a flipX toggle is
        // a simple placeholder for "turned to look elsewhere" (Day 8.3 log).
        if (sr != null) sr.flipX = !sr.flipX;

        float duration = Random.Range(idleLookMinDuration, idleLookMaxDuration);
        yield return new WaitForSeconds(duration);

        isIdleLooking = false;
        idleLookCoroutine = null;
        MoveTo(navTarget);
    }

    void CancelIdleLook()
    {
        if (idleLookCoroutine != null)
        {
            StopCoroutine(idleLookCoroutine);
            idleLookCoroutine = null;
        }
        isIdleLooking = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }

    // Day 8.3 - passive proximity tension: while Wandering (only), an enemy's
    // effective hearing radius temporarily grows when the player lingers
    // nearby, even without an explicit ping/decoy noise event. Ticked on a
    // timer (not every frame) since this project may have several enemies per
    // stage - HearingCheckInterval=0.3s keeps the cost negligible at that
    // enemy count while still reading as responsive.
    void TickProximityHearing()
    {
        if (Time.time < nextHearingCheckTime) return;
        nextHearingCheckTime = Time.time + HearingCheckInterval;

        if (player == null) player = GameObject.FindWithTag("Player");
        if (player == null) { hearingMultiplier = 1f; return; }

        float distance = Vector2.Distance(transform.position, player.transform.position);
        hearingMultiplier = distance <= hearingRadius * proximityHearingTriggerFactor
            ? proximityHearingMultiplier
            : 1f;
    }

    void UpdateChasePlayer()
    {
        // Actually catching the player is still handled entirely by the existing
        // physical OnCollisionEnter2D->Die() path below, unchanged from Day 3.
        GameObject livePlayer = player != null ? player : GameObject.FindWithTag("Player");
        if (livePlayer != null && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(GameToNav(livePlayer.transform.position));
        }

        if (Time.time - chaseStartTime >= chasePlayerDuration)
        {
            EnterReturnToStart();
        }
    }

    void UpdateChaseDecoy()
    {
        // Decoy chase paths to a fixed point once (set in EnterChaseDecoy) rather
        // than re-tracking every frame - this state only tracks how long to keep
        // pursuing a (possibly stale) decoy position before giving up.
        if (Time.time - chaseStartTime >= chaseTimeout)
        {
            EnterReturnToStart();
        }
    }

    void UpdateReturn()
    {
        if (agent != null && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            EnterWander();
        }
    }

    // Day 8.4, revised: alpha is fixed, not continuously computed/animated -
    // dimAlpha is now the enemy's constant baseline (always visible at that
    // dim level, no longer proximity-gated to the player's ambient glow), and
    // a ping's Reveal() sets a fixed alpha=1 once, then reverts to the fixed
    // dimAlpha once via a coroutine after the duration - neither value is
    // recomputed on a per-frame tick anymore.
    void SetAlpha(float alpha)
    {
        if (sr == null) return;
        Color c = baseColor;
        c.a = alpha;
        sr.color = c;
    }

    public void Reveal(float duration)
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
        }
        SetAlpha(1f);
        revealCoroutine = StartCoroutine(DimAfter(duration));

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyReveal();
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.1f, 0.1f);
        }
    }

    IEnumerator DimAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetAlpha(dimAlpha);
        revealCoroutine = null;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("CrackedWall"))
        {
            CrackedWall crackedWall = col.gameObject.GetComponent<CrackedWall>();
            if (crackedWall != null)
            {
                crackedWall.BreakImmediately();
            }
            return;
        }

        if (!col.gameObject.CompareTag("Player")) return;

        HybridPlayerController controller = col.gameObject.GetComponent<HybridPlayerController>();
        if (controller != null)
        {
            controller.Die();
        }
    }
}
