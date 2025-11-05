using UnityEngine;

[System.Serializable]
public class ResumeSnapshot
{
    public string playerName;
    public int score;
    public int kills;
    public int maxCombo;
    public int validHits;
    public int missClicks;
    public int punishmentHits;
    public int hearts;
    public int timeLeftSec; // biarkan 0 jika endless
}

public static class RuntimeResumeCache
{
    public static ResumeSnapshot Pending;
    public static bool Has => Pending != null;

    public static void Clear()
    {
        Pending = null;
    }
}
