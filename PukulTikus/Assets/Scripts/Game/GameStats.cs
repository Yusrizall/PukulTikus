using UnityEngine;

public class GameStats
{
    public string playerName = "Player";
    public int score = 0;
    public int kills = 0;
    public int combo = 0;
    public int maxCombo = 0;

    public int validHits = 0;
    public int missClicks = 0;
    public int punishmentHits = 0;

    // Lives
    public int heartsMax = 3;
    public int hearts = 3;

    // === Combo & Score ===
    public float CurrentMultiplier()
    {
        // tiap 10 kill: +0.5; cap 5x
        float mult = 1f + (kills / 10) * 0.5f;
        return Mathf.Min(mult, 5f);
    }

    public float Accuracy
        => (validHits + missClicks + punishmentHits) == 0
           ? 0f
           : (float)validHits / (validHits + missClicks + punishmentHits);

    public void OnKill()
    {
        kills++;
        validHits++;
        combo++;
        maxCombo = Mathf.Max(maxCombo, combo);
        score += Mathf.RoundToInt(100f * CurrentMultiplier());
    }

    public void OnHitNonKill()
    {
        validHits++;
        // no score change, combo tidak reset
    }

    public void OnMissGround()
    {
        missClicks++;
        combo = 0;
        score = Mathf.Max(0, score - 100);
    }

    public void OnPunishmentClicked()
    {
        punishmentHits++;
        combo = 0;
        score = Mathf.Max(0, score - 200);
    }

    public bool LoseHeart()
    {
        hearts = Mathf.Max(0, hearts - 1);
        return hearts <= 0;
    }

    public void GainHeart()
    {
        hearts = Mathf.Min(heartsMax, hearts + 1);
    }
}
