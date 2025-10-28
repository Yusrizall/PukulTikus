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

    // >>> Tambahan untuk API & Leaderboard <<<
    [Header("Networking & Leaderboard")]
    [SerializeField] private ApiClient api;                    // drag GO "ApiClient" ke sini
    [SerializeField] private LeaderboardPanel leaderboardPanel; // drag komponen LeaderboardPanel

    [Header("HUD")]
    [SerializeField] private TMP_Text txtScore;
    [SerializeField] private TMP_Text txtTimer;
    [SerializeField] private TMP_Text txtCombo;
    [SerializeField] private TMP_Text txtKills;
    [SerializeField] private TMP_Text txtPlayer;

    [Header("Result UI Root")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultName;
    [SerializeField] private TMP_Text resultScore;
    [SerializeField] private TMP_Text resultKills;
    [SerializeField] private TMP_Text resultMaxCombo;
    [SerializeField] private TMP_Text resultAccuracy;

    [Header("Click Raycast")]
    [SerializeField] private LayerMask clickableMask; // set ke "Default" (ground & mole pakai collider)

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private GameStats stats;
    private int timeLeftSec;
    private bool running;

    // phase state
    private readonly HashSet<MoleController> aliveMoles = new();
    private int spawnedThisPhase = 0;

    private void Start()
    {
        if (resultPanel) resultPanel.SetActive(false);

        stats = new GameStats();
        stats.playerName = PlayerPrefs.GetString("PlayerName", "Player");

        spawner.BuildGrid();

        // ⬇️ baru: sembunyikan semua lubang saat awal game
        spawner.VisualizeActiveHoles(null);

        int cfgDur = (gameConfig ? gameConfig.gameDurationSec : 0);
        timeLeftSec = Mathf.Max(1, cfgDur);
        running = true;
        UpdateHUD();

        StartCoroutine(GameTimer());
        StartCoroutine(PhaseSequence());
    }


    private void Update()
    {
        if (!running) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    public void OnClickPlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickMainMenu()
    {
        // coba load by name (aman kalau kamu ubah nama scene)
        if (!string.IsNullOrEmpty(mainMenuSceneName) &&
            Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        // fallback: load Element 0 (biasanya MainMenu)
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
        {
            // klik ke udara → bukan miss (sesuai spes: miss = klik ground/objek non-target)
            return;
        }

        var go = hit.collider.gameObject;

        // Punishment?
        if (go.CompareTag("Punishment"))
        {
            var mcPun = go.GetComponent<MoleController>();
            if (mcPun)
            {
                mcPun.Hit(onArmorBreakVfx: null, onKillVfx: null); // MoleController akan anggap punishmentClicked
            }
            else
            {
                stats.OnPunishmentClicked();
            }
            UpdateHUD();
            return;
        }

        // Mole normal
        if (go.CompareTag("Mole"))
        {
            var mc = go.GetComponent<MoleController>();
            if (mc)
            {
                mc.Hit(null, null); // langsung kill
            }
            else
            {
                stats.OnMissGround();
            }
            UpdateHUD();
            return;
        }

        // Mole armored
        if (go.CompareTag("MoleArmored"))
        {
            var mc = go.GetComponent<MoleController>();
            if (mc)
            {
                // Hit pertama: pecah armor (non-kill) → valid hit; Kill diproses via OnMoleDespawn
                mc.Hit(
                    onArmorBreakVfx: () => { stats.OnHitNonKill(); UpdateHUD(); },
                    onKillVfx: () => { /* kill stat via OnMoleDespawn */ }
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
            UpdateHUD();
            return;
        }

        // Objek lain non-target → miss
        stats.OnMissGround();
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

        running = false;

        // Tunggu sedikit agar clean-up despawn selesai
        yield return new WaitForSeconds(0.3f);
        ShowResult();
    }

    private IEnumerator PhaseSequence()
    {
        // Fase 1
        yield return RunPhase(gameConfig.phase1);
        if (!running) yield break;

        // Fase 2
        yield return RunPhase(gameConfig.phase2);
        if (!running) yield break;

        // Fase 3 (infinite)
        yield return RunPhase(gameConfig.phase3);
    }

    private IEnumerator RunPhase(PhaseConfig phase)
    {
        // ⬇️ baru: tampilkan lubang aktif untuk fase ini
        if (spawner != null)
            spawner.VisualizeActiveHoles(phase.activeHoles);

        spawnedThisPhase = 0;
        var active = phase.activeHoles;
        var interval = Mathf.Max(0.05f, phase.spawnInterval);
        var lifetime = Mathf.Max(0.1f, phase.lifetime);

        // 🔹 Nyalakan HANYA lubang yang aktif untuk fase ini
        spawner.VisualizeActiveHoles(phase.activeHoles);
        // normalisasi bobot
        float sum = Mathf.Max(0.0001f, phase.weightNormal + phase.weightArmored + phase.weightPunishment);
        float wN = phase.weightNormal / sum;
        float wA = phase.weightArmored / sum;
        float wP = phase.weightPunishment / sum;

        float timer = 0f;

        while (running)
        {
            // berhenti kalau quota terpenuhi (untuk fase finite)
            if (!phase.IsInfinite && spawnedThisPhase >= phase.quota)
            {
                if (aliveMoles.Count == 0) yield break; // selesai fase saat semua mole hilang
            }

            timer += Time.deltaTime;
            if (timer >= interval)
            {
                timer = 0f;

                // cek concurrent (minimal 1)
                if (aliveMoles.Count < Mathf.Max(1, phase.maxConcurrent))
                {
                    var hole = spawner.GetRandomActiveHole(active);
                    if (hole != null)
                    {
                        // pilih tipe berdasar bobot
                        float r = Random.value;
                        MoleType t = r < wN ? MoleType.Normal : (r < wN + wA ? MoleType.Armored : MoleType.Punishment);

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

            // fase finite: break bila quota habis & tak ada mole
            if (!phase.IsInfinite && spawnedThisPhase >= phase.quota && aliveMoles.Count == 0)
                break;

            yield return null;
        }
    }

    private void OnMoleDespawn(MoleController mc, bool killed, bool punishmentClicked)
    {
        if (mc != null && aliveMoles.Contains(mc))
            aliveMoles.Remove(mc);

        if (punishmentClicked)
        {
            stats.OnPunishmentClicked();
            UpdateHUD();
            return;
        }

        if (killed)
        {
            stats.OnKill();
            UpdateHUD();
            return;
        }

        // expired → no-op (bukan miss, tidak memotong combo)
    }

    private void UpdateHUD()
    {
        if (txtPlayer) txtPlayer.text = stats.playerName;
        if (txtScore)  txtScore.text  = $"Score: {stats.score}";
        if (txtKills)  txtKills.text  = $"Kills: {stats.kills}";
        if (txtCombo)  txtCombo.text  = $"Combo: {stats.combo}  (x{stats.CurrentMultiplier():0.0})";
        if (txtTimer)  txtTimer.text  = $"Time: {timeLeftSec}s";
    }

    private void ShowResult()
    {
        if (resultPanel) resultPanel.SetActive(true);
        if (resultName)      resultName.text      = stats.playerName;
        if (resultScore)     resultScore.text     = $"Score: {stats.score}";
        if (resultKills)     resultKills.text     = $"Kills: {stats.kills}";
        if (resultMaxCombo)  resultMaxCombo.text  = $"Max Combo: {stats.maxCombo}";
        if (resultAccuracy)  resultAccuracy.text  = $"Accuracy: {(stats.Accuracy * 100f):0}%";

        // ==== Submit ke API ====
        if (api)
        {
            var played = Mathf.Clamp(gameConfig.gameDurationSec - timeLeftSec, 0, gameConfig.gameDurationSec);

            var dto = new ScoreCreateDto
            {
                playerName     = stats.playerName,
                score          = Mathf.Max(0, stats.score),
                kills          = stats.kills,
                maxCombo       = stats.maxCombo,
                durationSec    = played,
                validHits      = stats.validHits,
                missClicks     = stats.missClicks,
                punishmentHits = stats.punishmentHits
            };

            StartCoroutine(api.PostScore(dto,
                onSuccess: (resp) =>
                {
                    Debug.Log($"[API] Score submitted. id={resp.id}, acc={resp.accuracy:0.00}");
                },
                onError: (err) =>
                {
                    Debug.LogError("[API] Submit failed: " + err);
                }
            ));
        }
    }
}
