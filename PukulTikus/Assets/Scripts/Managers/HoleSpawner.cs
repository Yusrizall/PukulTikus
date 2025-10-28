using System.Collections.Generic;
using UnityEngine;

public class HoleSpawner : MonoBehaviour
{
    [SerializeField] private GameConfig config;

    // Simpan tiap tile (koordinat grid) -> komponen GridHole
    private readonly Dictionary<Vector2Int, GridHole> tiles = new();

    /// <summary>
    /// Membangun grid lubang 4x3 (atau sesuai config) dalam keadaan NON-AKTIF (tidak terlihat).
    /// </summary>
    /// 

    public void BuildGrid()
    {
        tiles.Clear();

        if (config == null || config.holePrefab == null)
        {
            Debug.LogError("[HoleSpawner] Config atau holePrefab belum di-assign.");
            return;
        }

        for (int y = 0; y < config.gridHeight; y++)
        {
            for (int x = 0; x < config.gridWidth; x++)
            {
                var xy = new Vector2Int(x, y);
                var pos = config.GridToWorld(xy);

                var go = Instantiate(config.holePrefab, pos, Quaternion.identity, transform);
                go.name = $"Hole_{x}_{y}";
                // ⚠️ JANGAN beri Tag/Collider di sini — tile lubang hanya visual (ditoggle).
                // Klik miss akan ditangani oleh Ground environment (Tag=Ground, punya Collider).

                var tile = go.GetComponent<GridHole>();
                if (!tile) tile = go.AddComponent<GridHole>();

                // Mulai tersembunyi (lubang non-aktif)
                tile.Init(xy, startActive: false);

                tiles[xy] = tile;
            }
        }
    }

    /// <summary>
    /// Menyalakan hanya lubang yang aktif untuk fase saat ini.
    /// Jika active = null/empty → semua lubang disembunyikan.
    /// </summary>
    public void VisualizeActiveHoles(List<Vector2Int> active)
    {
        var set = (active != null) ? new HashSet<Vector2Int>(active) : new HashSet<Vector2Int>();
        foreach (var kv in tiles)
            kv.Value.SetActiveHole(set.Contains(kv.Key));
    }

    /// <summary>
    /// Ambil transform tile berdasarkan koordinat grid.
    /// </summary>
    public bool TryGetHole(Vector2Int xy, out Transform holeTf)
    {
        if (tiles.TryGetValue(xy, out var tile) && tile != null)
        {
            holeTf = tile.transform;
            return true;
        }
        holeTf = null;
        return false;
    }

    /// <summary>
    /// Ambil transform tile acak dari daftar koordinat aktif.
    /// </summary>
    public Transform GetRandomActiveHole(List<Vector2Int> active)
    {
        if (active == null || active.Count == 0) return null;
        var pick = active[Random.Range(0, active.Count)];
        return tiles.TryGetValue(pick, out var tile) && tile != null ? tile.transform : null;
    }

    /// <summary>
    /// Spawn mole pada tile tertentu, dengan offset Y dari config agar tidak tertelan mesh tanah.
    /// </summary>
    public MoleController SpawnMole(MoleType t, Transform holeTf)
    {
        if (config == null)
        {
            Debug.LogError("[HoleSpawner] Config belum di-assign.");
            return null;
        }
        if (holeTf == null) return null;

        GameObject prefab = t switch
        {
            MoleType.Armored => config.moleArmoredPrefab,
            MoleType.Punishment => config.punishmentPrefab,
            _ => config.moleNormalPrefab
        };

        if (prefab == null)
        {
            Debug.LogError($"[HoleSpawner] Prefab untuk tipe {t} belum di-assign di GameConfig.");
            return null;
        }

        var pos = holeTf.position + Vector3.up * Mathf.Max(0f, config.moleSpawnYOffset);
        var go = Instantiate(prefab, pos, Quaternion.identity, holeTf);

        var mc = go.GetComponent<MoleController>();
        if (!mc) mc = go.AddComponent<MoleController>();
        return mc;
    }

    public GameConfig Config => config;

    // ===== Debug helper (opsional, klik dari Inspector menu titik tiga komponen) =====
    [ContextMenu("Debug: Count Active Holes")]
    private void DebugCountActive()
    {
        int cnt = 0;
        foreach (var t in tiles.Values)
            if (t && t.holeVisual && t.holeVisual.activeSelf) cnt++;
        Debug.Log($"[HoleSpawner] Active hole visuals: {cnt}");
    }

}
