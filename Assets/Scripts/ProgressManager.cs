using System.Collections.Generic;
using UnityEngine;

// Cross-scene progress tracking: which stage the player has reached, and
// (from 7.4 on) which collectibles they've found. Built as a DontDestroyOnLoad
// singleton to match this project's existing convention (GameManager,
// SceneTransition, AudioManager, CameraShake all follow the same pattern) -
// a bare PlayerPrefs-only static class would've worked too, but the singleton
// keeps this consistent with the rest of the persistent-object bootstrap in
// TitleScreen. See the Day 7 checklist entry.
public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    const string HighestClearedKey = "HighestClearedStage";
    const string CollectiblesKey = "FoundCollectibles"; // comma-separated ids

    // Per-stage collectible totals. Level-design constants (how many
    // Collectible instances were actually placed in each stage - see the Day 7
    // checklist's placement list) rather than derived at runtime, since Stage
    // Select needs every stage's total while none of Stage1-5 are loaded.
    static readonly Dictionary<int, int> StageTotals = new Dictionary<int, int>
    {
        { 1, 2 }, { 2, 2 }, { 3, 3 }, { 4, 2 }, { 5, 3 },
    };

    int highestClearedStage;
    readonly HashSet<string> foundCollectibles = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        highestClearedStage = PlayerPrefs.GetInt(HighestClearedKey, 0);

        string saved = PlayerPrefs.GetString(CollectiblesKey, "");
        if (!string.IsNullOrEmpty(saved))
        {
            foreach (var id in saved.Split(','))
            {
                if (!string.IsNullOrEmpty(id)) foundCollectibles.Add(id);
            }
        }
    }

    // Stage 1 (index 1) is always unlocked (0 <= 0+1). Stage N unlocks once
    // stage N-1 has been cleared at least once.
    public bool IsStageUnlocked(int stageIndex)
    {
        return stageIndex <= highestClearedStage + 1;
    }

    public void MarkStageCleared(int stageIndex)
    {
        if (stageIndex <= highestClearedStage) return;

        highestClearedStage = stageIndex;
        PlayerPrefs.SetInt(HighestClearedKey, highestClearedStage);
        PlayerPrefs.Save();
    }

    public void MarkCollectibleFound(string collectibleId)
    {
        if (string.IsNullOrEmpty(collectibleId)) return;
        if (!foundCollectibles.Add(collectibleId)) return; // already found

        PlayerPrefs.SetString(CollectiblesKey, string.Join(",", foundCollectibles));
        PlayerPrefs.Save();
    }

    // Wipes saved progress back to a fresh save. Clears the in-memory mirrors
    // too, not just PlayerPrefs, since this singleton is already alive
    // (DontDestroyOnLoad) when called and never re-reads PlayerPrefs after Awake().
    public void ResetProgress()
    {
        highestClearedStage = 0;
        foundCollectibles.Clear();

        PlayerPrefs.DeleteKey(HighestClearedKey);
        PlayerPrefs.DeleteKey(CollectiblesKey);
        PlayerPrefs.Save();
    }

    public bool IsCollectibleFound(string collectibleId)
    {
        return foundCollectibles.Contains(collectibleId);
    }

    // Naming convention: "stage{N}_item{M}" - counts found ids with the
    // "stage{N}_" prefix, so placement scripts don't need to register
    // anything beyond giving each Collectible a ID that follows the pattern.
    public int GetCollectibleFoundCountForStage(int stageIndex)
    {
        string prefix = "stage" + stageIndex + "_";
        int count = 0;
        foreach (var id in foundCollectibles)
        {
            if (id.StartsWith(prefix)) count++;
        }
        return count;
    }

    public int GetCollectibleTotalForStage(int stageIndex)
    {
        return StageTotals.TryGetValue(stageIndex, out int total) ? total : 0;
    }
}
