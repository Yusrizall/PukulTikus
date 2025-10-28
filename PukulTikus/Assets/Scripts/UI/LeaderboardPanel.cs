using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPanel : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panelRoot;
    public Transform contentRoot;      // parent dari baris-baris
    public GameObject rowPrefab;       // prefab berisi LeaderboardRow
    public TMP_Text statusText;        // "Loading... / Error"

    [Header("Config")]
    public int topN = 10;

    private ApiClient api;

    void Awake()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    public void Init(ApiClient apiClient)
    {
        api = apiClient;
    }

    public void ShowAndRefresh()
    {
        if (!api) { Debug.LogError("[LeaderboardPanel] ApiClient belum di-init."); return; }
        if (panelRoot) panelRoot.SetActive(true);

        // bersihkan konten lama
        foreach (Transform c in contentRoot) Destroy(c.gameObject);

        if (statusText) statusText.gameObject.SetActive(true);
        if (statusText) statusText.text = "Loading...";

        StartCoroutine(LoadTop());
    }

    private IEnumerator LoadTop()
    {
        yield return api.GetTop(topN,
            onSuccess: (arr) =>
            {
                if (statusText) statusText.gameObject.SetActive(false);

                foreach (var e in arr)
                {
                    var go = Instantiate(rowPrefab, contentRoot);
                    var row = go.GetComponent<LeaderboardRow>();
                    if (row) row.Bind(e);
                }

                if (arr.Length == 0 && statusText)
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = "Belum ada data.";
                }
            },
            onError: (err) =>
            {
                if (statusText)
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = "Error: " + err;
                }
                Debug.LogError("[LeaderboardPanel] " + err);
            }
        );
    }

    // dipakai tombol Close/Back di panel leaderboard
    public void Hide()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }
}
