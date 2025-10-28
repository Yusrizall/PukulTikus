using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "WhackAMole/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Timer (detik)")]
    public int gameDurationSec = 60;

    [Header("Fases")]
    public PhaseConfig phase1;
    public PhaseConfig phase2;
    public PhaseConfig phase3;

    [Header("Grid (tetap 4x3)")]
    public int gridWidth = 4;
    public int gridHeight = 3;

    [Header("Prefabs")]
    public GameObject holePrefab;
    public GameObject moleNormalPrefab;
    public GameObject moleArmoredPrefab;
    public GameObject punishmentPrefab;

    [Header("Offsets")]
    public Vector3 gridOrigin = Vector3.zero;
    public Vector2 cellSize = new(2f, 2f); // jarak antar lubang di dunia

    [Header("Spawn Visual")]
    [Tooltip("Naikkan mole dari permukaan lubang supaya terlihat (unit dunia).")]
    public float moleSpawnYOffset = 0.6f;

    public Vector3 GridToWorld(Vector2Int xy)
    {
        return gridOrigin + new Vector3(xy.x * cellSize.x, 0f, xy.y * cellSize.y);
    }
}
