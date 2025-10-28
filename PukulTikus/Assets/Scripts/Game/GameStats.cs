using UnityEngine;

public class GameStats
{
    public string playerName = "";
    public int score = 0;
    public int kills = 0;
    public int maxCombo = 0;
    public int combo = 0;

    public int validHits = 0;
    public int missClicks = 0;
    public int punishmentHits = 0;

    public float Accuracy
    {
        get
        {
            int den = validHits + missClicks + punishmentHits;
            if (den <= 0) return 0f;
            return (float)validHits / den;
        }
    }

    // Multiplier: tiap 10 kill (streak), naik 0.5, cap 5x
    public float CurrentMultiplier()
    {
        int tier = combo / 10;          // 0..∞
        float mult = 1f + 0.5f * tier;  // 1.0, 1.5, 2.0, ...
        return Mathf.Clamp(mult, 1f, 5f);
    }

    public void OnMissGround() // klik tanah/objek non-target
    {
        missClicks++;
        combo = 0; // reset combo
        // penalty -100 but clamp >= 0
        score = Mathf.Max(0, score - 100);
    }

    public void OnPunishmentClicked()
    {
        punishmentHits++;
        combo = 0;
        score = Mathf.Max(0, score - 200);
    }

    public void OnHitNonKill() // armor break
    {
        validHits++;
        // tidak tambah score; tidak reset combo
    }

    public void OnKill()
    {
        validHits++;
        kills++;
        combo++;
        if (combo > maxCombo) maxCombo = combo;

        float m = CurrentMultiplier();
        score += Mathf.RoundToInt(100f * m);
    }
}
