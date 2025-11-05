using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HoleSpawner spawner;
    [SerializeField] private GameConfig gameConfig;

    [Header("Networking & Leaderboard")]
    [SerializeField] private ApiClient api;
    [SerializeField] private LeaderboardPanel leaderboardPanel;

    [Header("HUD")]
    [SerializeField] private TMP_Text txtScore;
    [SerializeField] private TMP_Text txtTimer;
    [SerializeField] private TMP_Text txtCombo;
    [SerializeField] private TMP_Text txtKills;
    [SerializeField] private TMP_Text txtPlayer;

    [Header("Lives UI")]
    [SerializeField] private UIHearts heartsUI;

    [Header("Result UI Root")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultName;
    [SerializeField] private TMP_Text resultScore;
    [SerializeField] private TMP_Text resultKills;
    [SerializeField] private TMP_Text resultMaxCombo;
    [SerializeField] private TMP_Text resultAccuracy;

    [Header("Click Raycast")]
    [SerializeField] private LayerMask clickableMask;

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Pause")]
    [SerializeField] private GameObject pausePanel;
    private bool isPaused = false;

    private GameStats stats;
    private int timeLeftSec;
    private bool running;

    // lives
    private int heartsCurrent = 3;

    // phase state
    private readonly HashSet<MoleController> aliveMoles = new();
    private int spawnedThisPhase = 0;

    // control: pakai timer atau full-endless?
    private bool useTimer = false;

    // ==== Helpers (dibaca HeartPickupController) ====
    public bool ConfigExists() => gameConfig != null;
    public float GetHeartHoldSeconds() => gameConfig ? gameConfig.heartHoldSeconds : 1.0f;

    // ==== Resume (lokal + runtime) ====
    private const string KEY_PENDING_RESUME = "PendingResume";

    [System.Serializable]
    private class ResumeSnapshot
    {
        public string playerName;
        public int score;
        public int kills;
        public int maxCombo;
        public int validHits;
        public int missClicks;
        public int punishmentHits;
        public int hearts;
        public int timeLeftSec;
    }

    // Kumpulkan snapshot dari state saat ini
    private ResumeSnapshot BuildSnapshot()
    {
        return new ResumeSnapshot
        {
            playerName = stats.playerName,
            score = stats.score,
            kills = stats.kills,
            maxCombo = stats.maxCombo,
            validHits = stats.validHits,
            missClicks = stats.missClicks,
            punishmentHits = stats.punishmentHits,
            hearts = heartsCurrent,
            timeLeftSec = useTimer ? timeLeftSec : 0
        };
    }

    // Simpan ke PlayerPrefs sebagai JSON
    private void SaveSnapshotLocal(ResumeSnapshot snap)
    {
        var json = JsonUtility.ToJson(snap);
        PlayerPrefs.SetString(KEY_PENDING_RESUME, json);
        PlayerPrefs.Save();
    }

    // Coba ambil snapshot dari PlayerPrefs (nama harus cocok)
    private bool TryLoadResume(out ResumeSnapshot snap)
    {
        snap = null;
        var json = PlayerPrefs.GetString(KEY_PENDING_RESUME, "");
        if (string.IsNullOrEmpty(json)) return false;

        try
        {
            var s = JsonUtility.FromJson<ResumeSnapshot>(json);
            if (s == null) return false;

            var nameNow = PlayerPrefs.GetString("PlayerName", "");
            if (!string.IsNullOrEmpty(nameNow) && nameNow != s.playerName) return false;

            snap = s;
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Terapkan snapshot ke game
    private void ApplyResume(ResumeSnapshot s)
    {
        // stats
        stats.playerName = s.playerName;
        stats.score = s.score;
        stats.kills = s.kills;
        stats.maxCombo = s.maxCombo;
        stats.validHits = s.validHits;
        stats.missClicks = s.missClicks;
        stats.punishmentHits = s.punishmentHits;

        // hearts
        int maxH = gameConfig ? Mathf.Max(1, gameConfig.heartsMax) : 3;
        heartsCurrent = Mathf.Clamp(s.hearts, 0, maxH);
        UpdateHeartsUI();

        // timer (kalau dipakai)
        if (useTimer) timeLeftSec = Mathf.Max(0, s.timeLeftSec);

        UpdateHUD();
    }

    private void Start()
    {
        if (resultPanel) resultPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        Time.timeScale = 1f;

        stats = new GameStats();
        stats.playerName = PlayerPrefs.GetString("PlayerName", "Player");

        // Hearts init (default penuh)
        int maxH = gameConfig ? Mathf.Max(1, gameConfig.heartsMax) : 3;
        heartsCurrent = maxH;
        UpdateHeartsUI();

        // Grid & holes (mulai hidden, akan diaktifkan per-phase)
        spawner.BuildGrid();
        spawner.VisualizeActiveHoles(null);

        // Timer policy: kalau GameConfig.gameDurationSec <= 0 → endless (timer disembunyikan)
        useTimer = gameConfig && gameConfig.gameDurationSec > 0;
        timeLeftSec = useTimer ? gameConfig.gameDurationSec : 0;
        if (txtTimer) txtTimer.gameObject.SetActive(useTimer);

        running = true;
        UpdateHUD();

        // === Coba terapkan resume bila tersedia ===
        if (TryLoadResume(out var snap))
        {
            ApplyResume(snap);
            // Hapus agar tidak auto-resume lagi di play berikutnya
            PlayerPrefs.DeleteKey(KEY_PENDING_RESUME);
            PlayerPrefs.Save();
        }

        if (useTimer) StartCoroutine(GameTimer());   // hanya kalau pakai timer
        StartCoroutine(PhaseSequence());
    }

    private void OnDestroy()
    {
        if (Time.timeScale != 1f) Time.timeScale = 1f;
    }

    private void Update()
    {
        // Hotkey Pause
        if (Input.GetKeyDown(KeyCode.Space))
            TogglePause();

        if (isPaused) return;
        if (!running) return;

        if (Input.GetMouseButtonDown(0))
            HandleClick();
    }

    // ===== Pause API =====
    public void TogglePause() { if (isPaused) ResumeGame(); else PauseGame(); }
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel) pausePanel.SetActive(true);
    }
    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
    }

    // === Save & Back to Menu (pakai API kalau tersedia; fallback ke lokal) ===
    public void OnClickSaveAndBack()
    {
        // unpause dulu biar transisi normal
        isPaused = false;
        Time.timeScale = 1f;

        // 1) Simpan snapshot lokal dulu (agar resume tetap jalan meski server mati)
        var snap = BuildSnapshot();
        SaveSnapshotLocal(snap);

        // 2) Opsional: kirim ke API bila tersedia
        if (api != null)
        {
            var dto = new PlayerSaveCreateDto
            {
                playerName = snap.playerName,
                score = snap.score,
                kills = snap.kills,
                maxCombo = snap.maxCombo,
                hearts = snap.hearts,
                timeLeftSec = snap.timeLeftSec
            };

            StartCoroutine(api.PostSave(
                dto,
                onSuccess: _ =>
                {
                    Debug.Log("[Game] Save OK (server). Back to menu.");
                    LoadMainMenu();
                },
                onError: err =>
                {
                    Debug.LogWarning("[Game] Save server failed: " + err + " — fallback pakai local snapshot.");
                    LoadMainMenu();
                }
            ));
        }
        else
        {
            Debug.LogWarning("[Game] ApiClient null. Back to menu dengan local snapshot saja.");
            LoadMainMenu();
        }
    }

    public void OnClickPlayAgain()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        LoadMainMenu();
    }

    private void LoadMainMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName) &&
            Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            SceneManager.LoadScene(0);
    }

    public void OnClickLeaderboard()
    {
        if (!leaderboardPanel || !api)
        {
            Debug.LogWarning("[GameManager] LeaderboardPanel atau ApiClient belum di-assign.");
            return;
        }
        leaderboardPanel.Init(api);
        leaderboardPanel.ShowAndRefresh();
    }

    private void HandleClick()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 1000f, clickableMask))
            return;

        var go = hit.collider.gameObject;

        // Heart → pickup via hold, jadi abaikan single click
        if (go.CompareTag("Heart") || go.GetComponent<HeartPickupController>())
            return;

        // Punishment?
        if (go.CompareTag("Punishment"))
        {
            var mcPun = go.GetComponent<MoleController>();
            if (mcPun) mcPun.Hit(null, null);
            else stats.OnPunishmentClicked();

            if (gameConfig && gameConfig.lifeLossOnPunishment)
                LoseHeart("punishment");
            UpdateHUD();
            return;
        }

        // Mole normal
        if (go.CompareTag("Mole"))
        {
            var mc = go.GetComponent<MoleController>();
            if (mc) mc.Hit(null, null);
            else stats.OnMissGround();
            UpdateHUD();
            return;
        }

        // Mole armored
        if (go.CompareTag("MoleArmored"))
        {
            var mc = go.GetComponent<MoleController>();
            if (mc)
            {
                mc.Hit(
                    onArmorBreakVfx: () => { stats.OnHitNonKill(); UpdateHUD(); },
                    onKillVfx: () => { /* kill via OnMoleDespawn */ }
                );
            }
            else
            {
                stats.OnMissGround();
            }
            UpdateHUD();
            return;
        }

        // Ground (environment) → miss
        if (go.CompareTag("Ground"))
        {
            stats.OnMissGround();
            if (gameConfig && gameConfig.lifeLossOnMiss)
                LoseHeart("miss");
            UpdateHUD();
            return;
        }

        // Objek lain non-target → miss
        stats.OnMissGround();
        if (gameConfig && gameConfig.lifeLossOnMiss)
            LoseHeart("miss");
        UpdateHUD();
    }

    private IEnumerator GameTimer()
    {
        while (running && timeLeftSec > 0)
        {
            yield return new WaitForSeconds(1f);
            timeLeftSec = Mathf.Max(0, timeLeftSec - 1);
            UpdateHUD();
        }

        if (!running) yield break;

        running = false;
        yield return new WaitForSeconds(0.3f);
        ShowResult();
    }

    private IEnumerator PhaseSequence()
    {
        yield return RunPhase(gameConfig.phase1);
        if (!running) yield break;

        yield return RunPhase(gameConfig.phase2);
        if (!running) yield break;

        yield return RunPhase(gameConfig.phase3);
    }

    private IEnumerator RunPhase(PhaseConfig phase)
    {
        if (spawner != null)
            spawner.VisualizeActiveHoles(phase.activeHoles);

        spawnedThisPhase = 0;
        var active = phase.activeHoles;
        var interval = Mathf.Max(0.05f, phase.spawnInterval);
        var lifetime = Mathf.Max(0.1f, phase.lifetime);

        // normalisasi bobot (termasuk Heart)
        float sum = Mathf.Max(0.0001f,
            phase.weightNormal + phase.weightArmored + phase.weightPunishment + phase.weightHeart);

        float wN = phase.weightNormal / sum;
        float wA = phase.weightArmored / sum;
        float wP = phase.weightPunishment / sum;
        float wH = phase.weightHeart / sum;

        float timer = 0f;

        while (running)
        {
            if (!phase.IsInfinite && spawnedThisPhase >= phase.quota)
            {
                if (aliveMoles.Count == 0) yield break;
            }

            timer += Time.deltaTime;
            if (timer >= interval)
            {
                timer = 0f;

                if (aliveMoles.Count < Mathf.Max(1, phase.maxConcurrent))
                {
                    var hole = spawner.GetRandomActiveHole(active);
                    if (hole != null)
                    {
                        float r = Random.value;
                        MoleType t = (r < wN) ? MoleType.Normal
                                   : (r < wN + wA) ? MoleType.Armored
                                   : (r < wN + wA + wP) ? MoleType.Punishment
                                                        : MoleType.Heart;

                        var mc = spawner.SpawnMole(t, hole);
                        if (mc != null)
                        {
                            aliveMoles.Add(mc);
                            spawnedThisPhase++;
                            mc.Init(t, lifetime, OnMoleDespawn);
                        }
                    }
                }
            }

            if (!phase.IsInfinite && spawnedThisPhase >= phase.quota && aliveMoles.Count == 0)
                break;

            yield return null;
        }
    }

    private void OnMoleDespawn(MoleController mc, bool killed, bool punishmentClicked)
    {
        if (mc != null && aliveMoles.Contains(mc))
            aliveMoles.Remove(mc);

        // Heart: consume via hold → nyawa ditambah di OnHeartCollected(); tak ada life-loss.
        if (mc != null && mc.type == MoleType.Heart)
            return;

        if (punishmentClicked)
        {
            stats.OnPunishmentClicked();
            // life loss untuk punishment diproses saat click
            UpdateHUD();
            return;
        }

        if (killed)
        {
            stats.OnKill();
            UpdateHUD();
            return;
        }

        // expired
        if (gameConfig && gameConfig.lifeLossOnExpire)
            LoseHeart("expire");
    }

    private void UpdateHUD()
    {
        if (txtPlayer) txtPlayer.text = stats.playerName;
        if (txtScore) txtScore.text = $"Score: {stats.score}";
        if (txtKills) txtKills.text = $"Kills: {stats.kills}";
        if (txtCombo) txtCombo.text = $"Combo: {stats.combo}  (x{stats.CurrentMultiplier():0.0})";
        if (txtTimer && useTimer) txtTimer.text = $"Time: {timeLeftSec}s";
    }

    private void UpdateHeartsUI()
    {
        int maxH = gameConfig ? Mathf.Max(1, gameConfig.heartsMax) : 3;
        if (heartsUI) heartsUI.SetHearts(heartsCurrent, maxH);
    }

    public void OnHeartCollected()
    {
        int maxH = gameConfig ? Mathf.Max(1, gameConfig.heartsMax) : 3;
        if (heartsCurrent < maxH)
        {
            heartsCurrent++;
            UpdateHeartsUI();
        }
    }

    private void LoseHeart(string reason)
    {
        if (heartsCurrent <= 0) return;
        heartsCurrent--;
        UpdateHeartsUI();

        if (heartsCurrent <= 0 && running)
        {
            running = false;
            StartCoroutine(EndFromHearts());
        }
    }

    private IEnumerator EndFromHearts()
    {
        yield return new WaitForSeconds(0.2f);
        ShowResult();
    }

    private void ShowResult()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (resultPanel) resultPanel.SetActive(true);
        if (resultName) resultName.text = stats.playerName;
        if (resultScore) resultScore.text = $"Score: {stats.score}";
        if (resultKills) resultKills.text = $"Kills: {stats.kills}";
        if (resultMaxCombo) resultMaxCombo.text = $"Max Combo: {stats.maxCombo}";
        if (resultAccuracy) resultAccuracy.text = $"Accuracy: {(stats.Accuracy * 100f):0}%";

        // submit ke API (seperti sebelumnya)
        if (api)
        {
            var played = useTimer
                ? Mathf.Clamp(gameConfig.gameDurationSec - timeLeftSec, 0, gameConfig.gameDurationSec)
                : 0;

            var dto = new ScoreCreateDto
            {
                playerName = stats.playerName,
                score = Mathf.Max(0, stats.score),
                kills = stats.kills,
                maxCombo = stats.maxCombo,
                durationSec = played,
                validHits = stats.validHits,
                missClicks = stats.missClicks,
                punishmentHits = stats.punishmentHits
            };

            StartCoroutine(api.PostScore(dto,
                onSuccess: (resp) => Debug.Log($"[API] Score submitted. id={resp.id}, acc={resp.accuracy:0.00}"),
                onError: (err) => Debug.LogError("[API] Submit failed: " + err)
            ));
        }
    }
}
