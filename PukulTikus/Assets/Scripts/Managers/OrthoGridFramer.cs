using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrthoGridFramer : MonoBehaviour
{
    [Header("Config")]
    public GameConfig config;

    [Header("Framing")]
    [Tooltip("Bantalan di sekeliling grid agar tidak mepet ke tepi layar")]
    public float padding = 0.75f;

    [Tooltip("Tambahan span untuk memperhitungkan diameter lubang/mesh")]
    public float extraSpan = 1.0f;

    [Tooltip("Ketinggian kamera dari grid (tidak mempengaruhi framing orthographic)")]
    public float height = 12f;

    [Tooltip("Update otomatis saat Play (nyaman untuk tweak)")]
    public bool continuousUpdate = true;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        Apply();
    }

    void Update()
    {
        if (continuousUpdate) Apply();
    }

    public void Apply()
    {
        if (!config) return;

        cam.orthographic = true;
        // pusat grid (antara lubang pertama & terakhir)
        Vector3 center = config.gridOrigin + new Vector3(
            (config.gridWidth - 1) * config.cellSize.x * 0.5f,
            0f,
            (config.gridHeight - 1) * config.cellSize.y * 0.5f
        );

        // posisikan kamera tepat di atas pusat grid (top-down)
        transform.position = center + Vector3.up * height;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // hitung bentang dunia (X = lebar, Z = tinggi)
        float spanX = (config.gridWidth - 1) * config.cellSize.x + extraSpan + 2f * padding;
        float spanZ = (config.gridHeight - 1) * config.cellSize.y + extraSpan + 2f * padding;

        // orthoSize = setengah tinggi; untuk lebar, bagi aspect
        float sizeByHeight = spanZ * 0.5f;
        float sizeByWidth = (spanX * 0.5f) / Mathf.Max(0.01f, cam.aspect);

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}
