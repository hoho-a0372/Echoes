using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ShadowEnemy : MonoBehaviour
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

    NavMeshAgent agent;
    SpriteRenderer sr;
    State state = State.Wander;
    Vector3 spawnPosition;
    float nextWanderTime;
    float chaseStartTime;
    Coroutine revealCoroutine;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = false;
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
        // hearing capability (hearingRadius) - whichever is smaller wins.
        float effectiveRadius = Mathf.Min(noiseRadius, hearingRadius);
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
        state = State.ChasePlayer;
        chaseStartTime = Time.time;
    }

    void EnterChaseDecoy(Vector2 decoyPosition)
    {
        state = State.ChaseDecoy;
        chaseStartTime = Time.time;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(GameToNav(decoyPosition));
        }
    }

    void EnterReturnToStart()
    {
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
    }

    void Update()
    {
        switch (state)
        {
            case State.Wander:
                UpdateWander();
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
        if (Time.time < nextWanderTime) return;

        Vector2 randomPoint = (Vector2)spawnPosition + Random.insideUnitCircle * wanderRadius;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(GameToNav(randomPoint));
        }
        nextWanderTime = Time.time + wanderInterval;
    }

    void UpdateChasePlayer()
    {
        // Actually catching the player is still handled entirely by the existing
        // physical OnCollisionEnter2D->Die() path below, unchanged from Day 3.
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(GameToNav(player.transform.position));
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

    public void Reveal(float duration)
    {
        if (sr != null)
        {
            sr.enabled = true;
        }

        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
        }
        revealCoroutine = StartCoroutine(HideAfter(duration));

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyReveal();
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.1f, 0.1f);
        }
    }

    IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (sr != null)
        {
            sr.enabled = false;
        }
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
