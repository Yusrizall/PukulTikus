using UnityEngine;

public class GridHole : MonoBehaviour
{
    [Header("Assign: only the visible mesh of the HOLE (no collider)")]
    public GameObject holeVisual;

    [Header("Optional Anchor")]
    [Tooltip("Titik tepat tempat mole harus muncul. Kalau null, fallback ke Transform hole.")]
    public Transform spawnAnchor;

    public Vector2Int GridXY { get; private set; }

    public Vector3 SpawnPositionWorld
        => spawnAnchor ? spawnAnchor.position : transform.position;

    public float SpawnAnchorY
        => spawnAnchor ? spawnAnchor.position.y : transform.position.y;

    public void Init(Vector2Int xy, bool startActive)
    {
        GridXY = xy;
        SetActiveHole(startActive);
    }

    // true = hole terlihat, false = hole tersembunyi
    public void SetActiveHole(bool active)
    {
        if (holeVisual) holeVisual.SetActive(active);
    }
}
