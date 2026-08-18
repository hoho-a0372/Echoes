using Unity.AI.Navigation;
using UnityEngine;

// Baking is done manually in-editor (Bake button) rather than
// auto-building on scene load, so any NavMeshSurface left unbaked (or
// stale after level edits) leaves every NavMeshAgent permanently off-mesh
// - agent.Warp/SetDestination fail silently and enemies never move. This
// rebuilds every NavMeshSurface in the active scene in Awake, which Unity
// guarantees completes for all objects before any object's Start runs
// (ShadowEnemy.Start -> agent.Warp included), so this needs no explicit
// script execution order setup.
public class NavMeshRuntimeBaker : MonoBehaviour
{
    void Awake()
    {
        foreach (NavMeshSurface surface in FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
        {
            surface.BuildNavMesh();
        }
    }
}
