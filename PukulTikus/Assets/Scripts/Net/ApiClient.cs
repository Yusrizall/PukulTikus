using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    [Header("Server")]
    [Tooltip("Contoh: https://localhost:7284 atau http://localhost:5229")]
    public string baseUrl = "https://localhost:7284";

    [Tooltip("DEV only: bypass verifikasi sertifikat HTTPS di Editor/Standalone")]
    public bool devBypassCertificate = true;

    // ===================== PUBLIC API =====================

    // POST /api/scores
    public IEnumerator PostScore(ScoreCreateDto dto, Action<ScoreDto> onSuccess, Action<string> onError)
    {
        string path = "api/scores";
        string json = JsonUtility.ToJson(dto);

        yield return SendWithFallback(
            path,
            makeRequest: (fullUrl) =>
            {
                var req = new UnityWebRequest(fullUrl, "POST");
                byte[] body = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                return req;
            },
            handleSuccess: (req) =>
            {
                var text = req.downloadHandler.text;
                var resp = JsonUtility.FromJson<ScoreDto>(text);
                onSuccess?.Invoke(resp);
            },
            onError
        );
    }

    // GET /api/scores/top/{n}
    public IEnumerator GetTop(int n, Action<LeaderboardEntryDto[]> onSuccess, Action<string> onError)
    {
        string path = $"api/scores/top/{Mathf.Clamp(n, 1, 100)}";

        yield return SendWithFallback(
            path,
            makeRequest: (fullUrl) =>
            {
                var req = UnityWebRequest.Get(fullUrl);
                req.downloadHandler = new DownloadHandlerBuffer();
                return req;
            },
            handleSuccess: (req) =>
            {
                var text = req.downloadHandler.text;
                var arr = JsonArrayHelper.FromJsonArray<LeaderboardEntryDto>(text);
                onSuccess?.Invoke(arr);
            },
            onError
        );
    }
    public IEnumerator PostSave(
    PlayerSaveCreateDto dto,
    System.Action<PlayerSaveDto> onSuccess,
    System.Action<string> onError)
    {
        // Pastikan kamu sudah set baseUrl di Inspector, misal: http://localhost:5237
        string baseUrlSafe = (baseUrl ?? "").TrimEnd('/');   // sesuaikan jika variabelmu bernama lain
        string url = baseUrlSafe + "/api/saves";              // ganti jika route-mu beda (mis. /api/playersaves)

        var json = JsonUtility.ToJson(dto);
        var req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success
                  || (req.responseCode >= 200 && req.responseCode < 300);

        if (!ok)
        {
            onError?.Invoke($"HTTP {(int)req.responseCode} {req.error}");
            yield break;
        }

        PlayerSaveDto resp = null;
        try
        {
            var txt = req.downloadHandler.text;
            if (!string.IsNullOrEmpty(txt))
                resp = JsonUtility.FromJson<PlayerSaveDto>(txt);
        }
        catch { /* ignore parse error */ }

        onSuccess?.Invoke(resp);
    }

    // GET /api/highscore (Top-1)
    public IEnumerator GetHighscore(Action<LeaderboardEntryDto> onSuccess, Action<string> onError)
    {
        string path = "api/highscore";

        yield return SendWithFallback(
            path,
            makeRequest: (fullUrl) =>
            {
                var req = UnityWebRequest.Get(fullUrl);
                req.downloadHandler = new DownloadHandlerBuffer();
                return req;
            },
            handleSuccess: (req) =>
            {
                // 204 No Content → tidak ada data
                if (req.responseCode == 204 || string.IsNullOrWhiteSpace(req.downloadHandler.text))
                {
                    onSuccess?.Invoke(null);
                    return;
                }
                var dto = JsonUtility.FromJson<LeaderboardEntryDto>(req.downloadHandler.text);
                onSuccess?.Invoke(dto);
            },
            onError
        );
    }
    // ====== SAVE/RESUME ======
    public IEnumerator GetSave(string playerName, System.Action<SaveResponseDto> onSuccess, System.Action<string> onError)
    {
        string url = $"{baseUrl}/api/saves/{UnityWebRequest.EscapeURL(playerName)}";
        using var www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            // 404 → tidak ada save
            if (www.responseCode == 404) { onSuccess?.Invoke(null); yield break; }
            onError?.Invoke($"HTTP {(int)www.responseCode} {www.error}");
            yield break;
        }

        var json = www.downloadHandler.text;
        var data = JsonUtility.FromJson<SaveResponseDto>(json);
        onSuccess?.Invoke(data);
    }

    public IEnumerator PostSave(SaveSnapshotDto payload, System.Action<SaveResponseDto> onSuccess, System.Action<string> onError)
    {
        string url = $"{baseUrl}/api/saves";
        string json = JsonUtility.ToJson(payload);

        var body = new System.Text.UTF8Encoding().GetBytes(json);
        using var www = new UnityWebRequest(url, "POST");
        www.uploadHandler = new UploadHandlerRaw(body);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"HTTP {(int)www.responseCode} {www.error}");
            yield break;
        }

        var resp = JsonUtility.FromJson<SaveResponseDto>(www.downloadHandler.text);
        onSuccess?.Invoke(resp);
    }

    public IEnumerator DeleteSave(string playerName, System.Action onSuccess, System.Action<string> onError)
    {
        string url = $"{baseUrl}/api/saves/{UnityWebRequest.EscapeURL(playerName)}";
        using var www = UnityWebRequest.Delete(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            // 404 juga dianggap selesai (tidak ada yang dihapus)
            if (www.responseCode == 404) { onSuccess?.Invoke(); yield break; }
            onError?.Invoke($"HTTP {(int)www.responseCode} {www.error}");
            yield break;
        }
        onSuccess?.Invoke();
    }


    // ===================== CORE FALLBACK =====================

    private IEnumerator SendWithFallback(
        string relativePath,
        Func<string, UnityWebRequest> makeRequest,
        Action<UnityWebRequest> handleSuccess,
        Action<string> onError)
    {
        var tried = new List<string>();
        string firstErr = null;

        foreach (var fullUrl in BuildCandidateUrls(relativePath))
        {
            tried.Add(fullUrl);
            using (var req = makeRequest(fullUrl))
            {
                if (devBypassCertificate &&
                    fullUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    req.certificateHandler = new DevCertBypass();
                }

                yield return req.SendWebRequest();

                bool ok = req.result == UnityWebRequest.Result.Success ||
                          (req.responseCode >= 200 && req.responseCode < 300);

                if (ok)
                {
                    try
                    {
                        handleSuccess(req);
                        yield break;
                    }
                    catch (Exception ex)
                    {
                        firstErr = $"Parse error: {ex.Message}\nBody: {req.downloadHandler?.text}";
                        onError?.Invoke(firstErr);
                        yield break;
                    }
                }
                else
                {
                    if (firstErr == null)
                        firstErr = $"HTTP {(int)req.responseCode} {req.error}\n{req.downloadHandler?.text}";
                }
            }
        }

        onError?.Invoke(firstErr ?? $"Cannot connect. Tried: {string.Join(", ", tried)}");
    }

    private IEnumerable<string> BuildCandidateUrls(string relativePath)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string path = relativePath.TrimStart('/');

        void add(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) return;
            string u = $"{root.TrimEnd('/')}/{path}";
            if (set.Add(u)) { /* added */ }
        }

        // kandidat 1: base apa adanya
        add(baseUrl);

        // kandidat lain: tukar scheme & host
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            string otherScheme = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "http" : "https";
            string otherHost = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : "localhost";

            add(With(uri, null, null));      // as-is (redundant safe)
            add(With(uri, otherScheme, null));      // scheme swapped
            add(With(uri, null, otherHost)); // host swapped
            add(With(uri, otherScheme, otherHost)); // scheme+host swapped
        }

        return set;
    }

    private string With(Uri uri, string scheme, string host)
    {
        try
        {
            var ub = new UriBuilder(uri)
            {
                Scheme = string.IsNullOrEmpty(scheme) ? uri.Scheme : scheme,
                Host = string.IsNullOrEmpty(host) ? uri.Host : host,
                Port = uri.Port
            };
            return ub.Uri.GetLeftPart(UriPartial.Authority);
        }
        catch { return null; }
    }

    // DEV ONLY: bypass cert untuk HTTPS lokal
    private class DevCertBypass : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
