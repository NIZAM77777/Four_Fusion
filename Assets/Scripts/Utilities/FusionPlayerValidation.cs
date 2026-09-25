#if DEVELOPMENT_BUILD && UNITY_STANDALONE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Opt-in development-player QA. This code is absent from release builds.
public sealed class FusionPlayerValidation : MonoBehaviour
{
    private string output;
    private readonly List<string> checks = new List<string>();
    private readonly List<string> errors = new List<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--fusion-qa") < 0) return;
        var driver = new GameObject("Development validation").AddComponent<FusionPlayerValidation>();
        DontDestroyOnLoad(driver.gameObject);
    }
    private void Awake()
    {
        Application.runInBackground = true;
        var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "--qa-output");
        output = i >= 0 ? args[i + 1] : Application.persistentDataPath;
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        StartCoroutine(Run());
        StartCoroutine(Watchdog());
    }
    private IEnumerator Watchdog()
    {
        yield return new WaitForSecondsRealtime(120);
        errors.Add("Validation timed out");
        File.WriteAllLines(Path.Combine(output, "player-checks.txt"), checks);
        File.WriteAllLines(Path.Combine(output, "player-errors.txt"), errors);
        File.WriteAllText(Path.Combine(output, "player-result.txt"), "FAIL");
        Application.Quit(2);
    }
    private void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + trace);
    }
    private void Check(bool value, string name) { checks.Add((value ? "PASS: " : "FAIL: ") + name); if (!value) errors.Add(name); }
    private void Navigate(string page) => typeof(FusionUI).GetMethod("Navigate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(FusionUI.Instance, new object[] { page });
    private IEnumerator Shot(string name)
    {
        yield return new WaitForSecondsRealtime(.6f);
        var root = FusionUI.Instance.GetComponent<UIDocument>().rootVisualElement;
        File.AppendAllText(Path.Combine(output, "layout.txt"), name + " root=" + root.worldBound + " elements=" + root.Query<VisualElement>().ToList().Count + "\n");
        // Hidden Windows swap chains produce black ScreenCapture images. Render the
        // actual scene and panel to an offscreen target for deterministic visual QA.
        var panel = FusionUI.Instance.GetComponent<UIDocument>().panelSettings;
        var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        target.Create(); panel.targetTexture = target;
        yield return null;
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
        RenderPipeline.SubmitRenderRequest(Camera.main, request);
        root.panel.GetType().GetMethod("Repaint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(root.panel, new object[] { new Event { type = EventType.Repaint } });
        root.panel.GetType().GetMethod("Render", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(root.panel, null);
        var previous = RenderTexture.active; RenderTexture.active = target;
        var snapshot = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        snapshot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); snapshot.Apply();
        File.WriteAllBytes(Path.Combine(output, name + ".png"), snapshot.EncodeToPNG());
        RenderTexture.active = previous; panel.targetTexture = null; target.Release(); Destroy(target); Destroy(snapshot);
        yield return new WaitForSecondsRealtime(.4f);
    }
    private IEnumerator Run()
    {
        yield return new WaitUntil(() => FusionUI.Instance != null && SceneManager.GetActiveScene().name == "MainMenu");
        yield return new WaitForSecondsRealtime(2);
        Localization.SetLanguage("en");
        Check(FusionUI.Instance != null, "Main menu starts");
        yield return Shot("menu-en");
        Navigate("mode"); yield return Shot("mode-en");
        Navigate("difficulty"); yield return Shot("difficulty-en");
        Navigate("settings"); yield return Shot("settings-en");
        foreach (var code in Localization.Codes)
        {
            Localization.SetLanguage(code); Navigate("home"); yield return Shot("menu-" + code);
            Navigate("settings"); yield return Shot("settings-" + code);
            var settingsScroll = FusionUI.Instance.GetComponent<UIDocument>().rootVisualElement.Q<ScrollView>();
            settingsScroll.scrollOffset = new Vector2(0, 10000);
            yield return Shot("settings-bottom-" + code);
            Check(PlayerPrefs.GetString("Language") == code, "Persisted language " + code);
        }
        Localization.SetLanguage("en");
        Screen.SetResolution(360, 640, false); Navigate("home"); yield return Shot("small-menu");
        Screen.SetResolution(768, 1024, false); Navigate("settings"); yield return Shot("tablet-settings");
        Screen.SetResolution(480, 900, false);
        GameSettings.GameMode = GameMode.HumanVsHuman;
        SceneTransition.Load("GameScene");
        yield return new WaitForSecondsRealtime(3);
        Check(BoardManager.Instance != null && BoardManager.Instance.Columns == 7 && BoardManager.Instance.Rows == 6, "7x6 board preserved");
        Check(FusionUI.Instance.GetComponent<UIDocument>().rootVisualElement.Q<Button>("column-0").enabledInHierarchy, "Column UI enabled after scene transition");
        Check(!BoardManager.Instance.TryDropPiece(-1, false) && !BoardManager.Instance.TryDropPiece(7, false), "Invalid columns rejected");
        Check(BoardManager.Instance.TryDropPiece(0, false), "First drop accepted");
        Check(!BoardManager.Instance.TryDropPiece(0, false), "Duplicate drop rejected");
        Check(!BoardManager.Instance.CanUndo(), "Undo locked during animation");
        yield return new WaitUntil(() => !BoardManager.Instance.IsBusy);
        foreach (int column in new[] { 1, 0, 1, 0, 1, 0 })
        {
            BoardManager.Instance.DropPiece(column);
            yield return new WaitUntil(() => !BoardManager.Instance.IsBusy);
        }
        yield return new WaitForSecondsRealtime(1.5f);
        Check(GameManager.Instance.IsGameOver, "Win detection");
        yield return Shot("result-en");
        Localization.SetLanguage("ar"); yield return Shot("result-ar");
        BoardManager.Instance.UndoLastTwoMoves();
        Check(!GameManager.Instance.IsGameOver && BoardManager.Instance.MoveCount == 5 && BoardManager.Instance.CurrentPlayer == PieceType.Player2, "Correct result undo");
        yield return Shot("gameplay-ar");
        Localization.SetLanguage("en"); yield return Shot("gameplay-en");
        Screen.SetResolution(360, 640, false); yield return Shot("small-gameplay");
        Screen.SetResolution(900, 480, false); yield return Shot("landscape-gameplay");
        Screen.SetResolution(480, 900, false);
        GameManager.Instance.Home(); yield return new WaitForSecondsRealtime(2);
        Check(FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length == 1, "One music service after return");
        Check(FindObjectsByType<AdManager>(FindObjectsSortMode.None).Length == 1, "One ad service after return");
        Check(FindObjectsByType<PieceThemeManager>(FindObjectsSortMode.None).Length == 1, "One theme service after return");
        GameSettings.GameMode = GameMode.HumanVsAI; GameSettings.Difficulty = Difficulty.Hard;
        SceneTransition.Load("GameScene"); yield return new WaitForSecondsRealtime(2);
        BoardManager.Instance.DropPiece(3); yield return new WaitUntil(() => !BoardManager.Instance.IsBusy);
        Check(!BoardManager.Instance.TryDropPiece(3, false), "Human cannot play AI turn");
        yield return new WaitUntil(() => BoardManager.Instance.CurrentPlayer == PieceType.Player1 && !BoardManager.Instance.IsBusy);
        Check(BoardManager.Instance.MoveCount == 2, "Hard AI plays exactly one reply");
        GameManager.Instance.UndoMove();
        Check(BoardManager.Instance.MoveCount == 0, "AI paired free undo");
        float elapsed = 0, maxFrame = 0;
        for (int frame = 0; frame < 120; frame++) { yield return null; elapsed += Time.unscaledDeltaTime; maxFrame = Mathf.Max(maxFrame, Time.unscaledDeltaTime); }
        File.WriteAllText(Path.Combine(output, "desktop-profile.txt"), "Development Windows player, 120 idle gameplay frames; not Android or baseline comparison.\nMean FPS: " + (120 / elapsed) + "\nMax frame ms: " + (maxFrame * 1000) + "\nUnity allocated bytes: " + UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() + "\nUnity reserved bytes: " + UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong());
        File.WriteAllLines(Path.Combine(output, "player-checks.txt"), checks);
        File.WriteAllLines(Path.Combine(output, "player-errors.txt"), errors);
        File.WriteAllText(Path.Combine(output, "player-result.txt"), errors.Count == 0 ? "PASS" : "FAIL");
        Application.Quit(errors.Count == 0 ? 0 : 1);
    }
}
#endif
