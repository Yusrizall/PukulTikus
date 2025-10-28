using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ApiClient api;
    [SerializeField] private LeaderboardPanel leaderboardPanel;

    [Header("UI - Name & Buttons")]
    [SerializeField] private TMP_InputField inputName;
    [SerializeField] private Button btnPlay;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnLeaderboard;

    [Header("UI - Highscore")]
    [SerializeField] private TMP_Text lblHighscoreName;
    [SerializeField] private TMP_Text lblHighscoreScore;

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsRoot;
    [SerializeField] private Slider sliderMusic;
    [SerializeField] private Slider sliderSfx;

    [Header("Config")]
    [SerializeField] private string gameSceneName = "Game";

    private const string KEY_PLAYER_NAME = "PlayerName";
    private const string KEY_VOL_MUSIC = "Vol_Music";
    private const string KEY_VOL_SFX = "Vol_SFX";

    // ===== lifecycle =====
    private void Awake()
    {
        ResolveRefs();

        // Prefill name
        var saved = PlayerPrefs.GetString(KEY_PLAYER_NAME, "");
        if (inputName) inputName.text = saved;

        // Play button state
        ValidateName(inputName ? inputName.text : "");

        // Load volumes
        float vMusic = PlayerPrefs.GetFloat(KEY_VOL_MUSIC, 0.8f);
        float vSfx = PlayerPrefs.GetFloat(KEY_VOL_SFX, 0.8f);
        if (sliderMusic) sliderMusic.value = vMusic;
        if (sliderSfx) sliderSfx.value = vSfx;
        ApplyVolumes(vMusic, vSfx);

        // Hook listeners (dengan log supaya kelihatan kalau gagal)
        if (inputName) inputName.onValueChanged.AddListener(ValidateName);

        if (btnPlay) btnPlay.onClick.AddListener(OnClickPlay);
        else Debug.LogWarning("[MainMenu] btnPlay belum di-assign.");

        if (btnSettings) btnSettings.onClick.AddListener(() => SetSettingsVisible(true));
        else Debug.LogWarning("[MainMenu] btnSettings belum di-assign.");

        if (btnLeaderboard)
        {
            btnLeaderboard.onClick.RemoveAllListeners();
            btnLeaderboard.onClick.AddListener(OpenLeaderboard);
            Debug.Log("[MainMenu] Hooked Btn_Leaderboard → OpenLeaderboard()");
        }
        else
        {
            Debug.LogWarning("[MainMenu] btnLeaderboard belum di-assign.");
        }
    }

    private void Start()
    {
        // Fetch Top-1 untuk header
        if (api)
        {
            StartCoroutine(api.GetHighscore(
                onSuccess: (top1) =>
                {
                    if (top1 == null)
                    {
                        if (lblHighscoreName) lblHighscoreName.text = "Top 1: —";
                        if (lblHighscoreScore) lblHighscoreScore.text = "Score: —";
                        return;
                    }
                    if (lblHighscoreName) lblHighscoreName.text = $"Top 1: {top1.playerName}";
                    if (lblHighscoreScore) lblHighscoreScore.text = $"Score: {top1.score}";
                },
                onError: (err) =>
                {
                    if (lblHighscoreName) lblHighscoreName.text = "Top 1: (error)";
                    if (lblHighscoreScore) lblHighscoreScore.text = err;
                    Debug.LogError("[MainMenu] Highscore error: " + err);
                }
            ));
        }
        else
        {
            Debug.LogWarning("[MainMenu] ApiClient belum tersedia saat Start().");
        }
    }

    // ===== public UI wrappers (enak di-assign dari Inspector) =====
    public void OpenSettings() => SetSettingsVisible(true);
    public void CloseSettings() => SetSettingsVisible(false);

    public void OpenLeaderboard()
    {
        ResolveRefs();
        Debug.Log("[MainMenu] OpenLeaderboard pressed");

        if (!leaderboardPanel)
        {
            Debug.LogError("[MainMenu] leaderboardPanel NULL. Pastikan drag LeaderboardPanelRoot (komponen LeaderboardPanel) ke field 'leaderboardPanel'.");
            return;
        }
        if (!api)
        {
            Debug.LogError("[MainMenu] ApiClient NULL. Pastikan drag GameObject 'ApiClient' ke field 'api'.");
            return;
        }

        // panelRoot harus diisi di komponen LeaderboardPanel
        leaderboardPanel.Init(api);
        leaderboardPanel.ShowAndRefresh();
    }

    public void CloseLeaderboard()
    {
        if (leaderboardPanel) leaderboardPanel.Hide();
    }

    // ===== buttons =====
    private void OnClickPlay()
    {
        string name = (inputName ? inputName.text : "").Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 20) return;

        PlayerPrefs.SetString(KEY_PLAYER_NAME, name);
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            SceneManager.LoadScene(1);
    }

    // ===== helpers =====
    private void SetSettingsVisible(bool visible)
    {
        if (settingsRoot) settingsRoot.SetActive(visible);
        else Debug.LogWarning("[MainMenu] settingsRoot belum di-assign.");
    }

    private void ValidateName(string s)
    {
        s = (s ?? "").Trim();
        bool ok = s.Length > 0 && s.Length <= 20;
        if (btnPlay) btnPlay.interactable = ok;
    }

    private void ApplyVolumes(float vMusic, float vSfx)
    {
        AudioListener.volume = Mathf.Clamp01((vMusic + vSfx) * 0.5f);
    }

    public void OnMusicVolumeChanged(float v)
    {
        float sfx = sliderSfx ? sliderSfx.value : 0.8f;
        ApplyVolumes(v, sfx);
        PlayerPrefs.SetFloat(KEY_VOL_MUSIC, v);
    }

    public void OnSfxVolumeChanged(float v)
    {
        float music = sliderMusic ? sliderMusic.value : 0.8f;
        ApplyVolumes(music, v);
        PlayerPrefs.SetFloat(KEY_VOL_SFX, v);
    }

    public void OnClickSettingsBack()
    {
        PlayerPrefs.Save();
        SetSettingsVisible(false);
    }

    private void ResolveRefs()
    {
        if (!api) api = FindObjectOfType<ApiClient>(true);
        if (!leaderboardPanel) leaderboardPanel = FindObjectOfType<LeaderboardPanel>(true);
    }
}
