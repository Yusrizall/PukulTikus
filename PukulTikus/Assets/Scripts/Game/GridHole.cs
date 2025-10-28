using UnityEngine;

public class GridHole : MonoBehaviour
{
    [Header("Assign: only the visible mesh of the HOLE (no collider)")]
    public GameObject holeVisual;

    public Vector2Int GridXY { get; private set; }

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
