using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrthoTiltFramer : MonoBehaviour
{
    [Header("Config")]
    public GameConfig config;

    [Header("View")]
    [Tooltip("Yaw (derajat) memutar di sumbu Y (0 = lihat dari +X)")]
    public float yawDeg = 30f;

    [Tooltip("Tilt dari TOP (0 = top-down persis, 90 = horisontal). Contoh 35–60 agar agak miring.")]
    public float tiltFromTopDeg = 35f;

    [Tooltip("Jarak kamera dari pusat grid (tidak mempengaruhi framing ortho, hanya posisi)")]
    public float radius = 12f;

    [Header("Framing")]
    [Tooltip("Padding dunia (unit) di kanan/kiri/atas/bawah agar tidak mepet tepi layar")]
    public float paddingWorld = 0.5f;

    [Tooltip("Tambahan bentang untuk memperhitungkan diameter lubang/mesh")]
    public float extraSpan = 1.0f;

    [Tooltip("Update otomatis setiap frame saat Play (nyaman untuk tweaking)")]
    public bool continuousUpdate = true;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
    }

    void Start() => Apply();

    void Update()
    {
        if (continuousUpdate) Apply();
    }

    public void Apply()
    {
        if (!config) return;

        // 1) Hitung pusat grid
        Vector3 center = config.gridOrigin + new Vector3(
            (config.gridWidth - 1) * config.cellSize.x * 0.5f,
            0f,
            (config.gridHeight - 1) * config.cellSize.y * 0.5f
        );

        // 2) Posisi & rotasi kamera (tilt + yaw), lalu lihat ke center
        float theta = Mathf.Deg2Rad * Mathf.Clamp(tiltFromTopDeg, 0f, 89.9f); // 0..~90
        float phi = Mathf.Deg2Rad * yawDeg;

        // Spherical (dari center ke kamera)
        Vector3 offset = new Vector3(
            Mathf.Sin(theta) * Mathf.Cos(phi),
            Mathf.Cos(theta),
            Mathf.Sin(theta) * Mathf.Sin(phi)
        ) * Mathf.Max(0.1f, radius);

        transform.position = center + offset;
        transform.LookAt(center, Vector3.up);

        // 3) Hitung bentang grid di dunia
        float minX = config.gridOrigin.x - extraSpan * 0.5f;
        float maxX = config.gridOrigin.x + (config.gridWidth - 1) * config.cellSize.x + extraSpan * 0.5f;
        float minZ = config.gridOrigin.z - extraSpan * 0.5f;
        float maxZ = config.gridOrigin.z + (config.gridHeight - 1) * config.cellSize.y + extraSpan * 0.5f;
        float y = config.gridOrigin.y; // asumsi grid di y ini (umumnya 0)

        // 4) Proyeksikan 4 sudut ke ruang kamera (ortho → cukup X/Y)
        Vector3[] cornersWorld =
        {
            new Vector3(minX, y, minZ),
            new Vector3(maxX, y, minZ),
            new Vector3(minX, y, maxZ),
            new Vector3(maxX, y, maxZ),
        };

        Matrix4x4 M = cam.worldToCameraMatrix;
        float minCX = float.PositiveInfinity, maxCX = float.NegativeInfinity;
        float minCY = float.PositiveInfinity, maxCY = float.NegativeInfinity;

        for (int i = 0; i < cornersWorld.Length; i++)
        {
            Vector3 c = M.MultiplyPoint3x4(cornersWorld[i]);
            if (c.x < minCX) minCX = c.x;
            if (c.x > maxCX) maxCX = c.x;
            if (c.y < minCY) minCY = c.y;
            if (c.y > maxCY) maxCY = c.y;
        }

        float dx = (maxCX - minCX) + 2f * paddingWorld;
        float dy = (maxCY - minCY) + 2f * paddingWorld;

        // 5) Ortho size = setengah tinggi di ruang kamera,
        //    juga pastikan lebar muat dengan memperhitungkan aspect ratio.
        float sizeByHeight = dy * 0.5f;
        float sizeByWidth = (dx * 0.5f) / Mathf.Max(0.01f, cam.aspect);

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}
