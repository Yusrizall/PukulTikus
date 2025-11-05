using System;
using UnityEngine;

[Serializable]
public class ScoreCreateDto
{
    // pakai camelCase agar mulus dengan System.Text.Json default
    public string playerName;
    public int score;
    public int kills;
    public int maxCombo;
    public int durationSec;
    public int validHits;
    public int missClicks;
    public int punishmentHits;
}

[Serializable]
public class ScoreDto
{
    public int id;
    public string playerName;
    public int score;
    public int kills;
    public int maxCombo;
    public double accuracy;
    public int durationSec;
    public int validHits;
    public int missClicks;
    public int punishmentHits;
    public string createdAt; // ISO string
}

[Serializable]
public class LeaderboardEntryDto
{
    public int rank;
    public int id;
    public string playerName;
    public int score;
    public int kills;
    public int maxCombo;
    public string createdAt; // ISO string
}
[System.Serializable]
public class SaveSnapshotDto
{
    public string playerName;
    public int score;
    public int kills;
    public int maxCombo;
    public int validHits;
    public int missClicks;
    public int punishmentHits;
    public int hearts;
    public int phaseIndex;
    public int timeLeftSec;
}

[System.Serializable]
public class SaveResponseDto
{
    public int id;
    public string playerName;
    public int score;
    public int kills;
    public int maxCombo;
    public int validHits;
    public int missClicks;
    public int punishmentHits;
    public int hearts;
    public int phaseIndex;
    public int timeLeftSec;
    public long createdAt;
    public long updatedAt;
}
// ====== Player Save DTOs ======
[System.Serializable]
public class PlayerSaveCreateDto
{
    public string playerName;
    public int score;
    public int kills;
    public int maxCombo;
    public int hearts;
    public int timeLeftSec; // 0 jika endless
}

[System.Serializable]
public class PlayerSaveDto
{
    public int id;              // kalau backend kirim ID
    public string playerName;
    public int score;
    public int kills;
    public int maxCombo;
    public int hearts;
    public int timeLeftSec;     // 0 jika endless
    public long createdAt;      // optional: epoch detik jika backend kirim
}


// Helper kecil agar bisa parse array JSON dengan JsonUtility
public static class JsonArrayHelper
{
    [Serializable] private class Wrapper<T> { public T[] items; }

    public static T[] FromJsonArray<T>(string json)
    {
        // bungkus: [{"a":1}] -> {"items":[{"a":1}]}
        string wrapped = "{\"items\":" + json + "}";
        return JsonUtility.FromJson<Wrapper<T>>(wrapped).items;
    }
}
