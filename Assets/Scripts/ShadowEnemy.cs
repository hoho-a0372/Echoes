using UnityEngine;
using System.Collections;

public class ShadowEnemy : MonoBehaviour
{
    [SerializeField] Transform[] patrolPoints;
    [SerializeField] float moveSpeed = 2f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    int currentPointIndex;
    Coroutine revealCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = false;
        }
    }

    void FixedUpdate()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform target = patrolPoints[currentPointIndex];
        Vector2 toTarget = (Vector2)target.position - rb.position;
        if (toTarget.magnitude < 0.1f)
        {
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            target = patrolPoints[currentPointIndex];
            toTarget = (Vector2)target.position - rb.position;
        }

        rb.linearVelocity = toTarget.normalized * moveSpeed;
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
        if (!col.gameObject.CompareTag("Player")) return;

        HybridPlayerController controller = col.gameObject.GetComponent<HybridPlayerController>();
        if (controller != null)
        {
            controller.Die();
        }
    }
}
