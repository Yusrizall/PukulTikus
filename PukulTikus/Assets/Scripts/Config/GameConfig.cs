using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "WhackAMole/GameConfig")]
public class GameConfig : ScriptableObject
{
    // ==== Timer lama (masih dipakai kalau mode countdown aktif) ====
    [Header("Timer (detik)")]
    public int gameDurationSec = 60;

    // ==== Fase ====
    [Header("Fases")]
    public PhaseConfig phase1;
    public PhaseConfig phase2;
    public PhaseConfig phase3;

    // ==== Grid ====
    [Header("Grid (tetap 4x3)")]
    public int gridWidth = 4;
    public int gridHeight = 3;

    // ==== Prefabs ====
    [Header("Prefabs")]
    public GameObject holePrefab;
    public GameObject moleNormalPrefab;
    public GameObject moleArmoredPrefab;
    public GameObject punishmentPrefab;

    // Tambahan heart
    [Header("Prefabs (Tambahan)")]
    public GameObject heartPrefab;

    // ==== Posisi / Visual ====
    [Header("Offsets")]
    public Vector3 gridOrigin = Vector3.zero;
    public Vector2 cellSize = new(2f, 2f); // jarak antar lubang di dunia

    [Header("Spawn Visual")]
    [Tooltip("Naikkan mole dari permukaan lubang supaya terlihat (unit dunia).")]
    public float moleSpawnYOffset = 0.6f;

    // ==== Sistem Nyawa ====
    [Header("Lives / Hearts")]
    [Tooltip("Jumlah nyawa maksimum pemain.")]
    [Range(1, 10)] public int heartsMax = 3;

    [Tooltip("Klik tanah dianggap mengurangi nyawa?")]
    public bool lifeLossOnMiss = true;

    [Tooltip("Klik punishment (bom) mengurangi nyawa?")]
    public bool lifeLossOnPunishment = true;

    [Tooltip("Mole expire (habis waktu) mengurangi nyawa?")]
    public bool lifeLossOnExpire = false;

    [Tooltip("Durasi HOLD untuk mengambil heart (detik).")]
    [Range(0.1f, 3f)] public float heartHoldSeconds = 1.0f;

    // ==== Helper posisi grid -> world ====
    public Vector3 GridToWorld(Vector2Int xy)
    {
        return gridOrigin + new Vector3(xy.x * cellSize.x, 0f, xy.y * cellSize.y);
    }

    // ==== Alias kompatibilitas (kalau ada skrip lama yang menyebut field lama) ====
    // Beberapa kode mungkin refer ke 'maxHearts' / 'loseLifeOn...' — kita sediakan alias read-only.
    public int maxHearts => heartsMax;
    public bool loseLifeOnMiss => lifeLossOnMiss;
    public bool loseLifeOnPunishment => lifeLossOnPunishment;
    public bool loseLifeOnExpire => lifeLossOnExpire;

#if UNITY_EDITOR
    private void OnValidate()
    {
        heartsMax = Mathf.Clamp(heartsMax, 1, 10);
        heartHoldSeconds = Mathf.Clamp(heartHoldSeconds, 0.1f, 3f);
        if (cellSize.x <= 0f) cellSize.x = 0.01f;
        if (cellSize.y <= 0f) cellSize.y = 0.01f;
    }
#endif
}
