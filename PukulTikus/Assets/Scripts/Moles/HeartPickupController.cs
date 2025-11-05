using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HeartPickupController : MonoBehaviour
{
    private MoleController mc;
    private Camera cam;
    private float holdTimer;

    [SerializeField] private float holdSeconds = 1.0f; // akan di-override dari GameConfig

    private void Awake()
    {
        mc = GetComponent<MoleController>();
        cam = Camera.main;
    }

    private void Start()
    {
        var gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            float s = gm.GetHeartHoldSeconds();
            if (s > 0f) holdSeconds = s;
        }
    }

    private void Update()
    {
        if (!cam) cam = Camera.main;

        if (Input.GetMouseButton(0))
        {
            if (PointerOnThis())
            {
                holdTimer += Time.unscaledDeltaTime; // tetap jalan saat Time.timeScale=0
                if (holdTimer >= holdSeconds)
                {
                    var gm = FindObjectOfType<GameManager>();
                    if (gm) gm.OnHeartCollected();

                    if (mc) mc.ConsumeHeartAndDespawn();
                    else Destroy(gameObject);
                }
            }
            else
            {
                holdTimer = 0f;
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    private bool PointerOnThis()
    {
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 1000f))
            return hit.collider && hit.collider.gameObject == gameObject;
        return false;
    }
}
