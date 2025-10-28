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
