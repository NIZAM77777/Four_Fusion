using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

// Executed only by the explicit batch validation entry point, never on import.
public static class FusionValidation
{
    public static void BuildAndroidValidation()
    {
        FusionProjectTools.ConfigureText(); FusionProjectTools.CreateServicePrefabs();
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.mn7studio.fourfusion.validation");
        PlayerSettings.productName = "Four Fusion Validation";
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        FusionProjectTools.ValidateRelease();
        AssetDatabase.SaveAssets();
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), Path.GetFullPath("../.utmp/FourFusion-validation.aab"), BuildTarget.Android, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Android validation bundle failed");
    }
    public static void BuildWindows()
    {
        // Keep validation preferences separate from the user's real game settings.
        PlayerSettings.productName = "Four Fusion Validation";
        FusionProjectTools.ConfigureText();
        FusionProjectTools.CreateServicePrefabs();
        AssetDatabase.SaveAssets();
        var directory = Path.GetFullPath("../.utmp/Player");
        Directory.CreateDirectory(directory);
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), Path.Combine(directory, "FourFusion.exe"), BuildTarget.StandaloneWindows64, BuildOptions.Development);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Validation player build failed");
    }
    private static readonly List<string> checks = new List<string>();
    private static string output;
    private static int phase;
    private static double next;
    private static bool failed;
    private static int moveIndex;
    private static readonly int[] WinningSequence = { 0, 1, 0, 1, 0, 1, 0 };

    public static void Run()
    {
        output = Path.GetFullPath(Directory.Exists("Documentation") ? "Documentation/Validation" : "../Documentation/Validation");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += Log;
        try
        {
            FusionProjectTools.ConfigureText();
            FusionProjectTools.CreateServicePrefabs();
            AssetDatabase.SaveAssets();
            RunRules();
            AuditReferences();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            EditorApplication.playModeStateChanged += PlayChanged;
            SessionState.SetBool("FusionValidation", true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (!SessionState.GetBool("FusionValidation", false)) return;
        output = Path.GetFullPath(Directory.Exists("Documentation") ? "Documentation/Validation" : "../Documentation/Validation");
        Application.logMessageReceived -= Log; Application.logMessageReceived += Log;
        EditorApplication.playModeStateChanged -= PlayChanged; EditorApplication.playModeStateChanged += PlayChanged;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        next = EditorApplication.timeSinceStartup + 4;
    }
    private static void PlayChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) { phase = 0; next = EditorApplication.timeSinceStartup + 4; EditorApplication.update -= Tick; EditorApplication.update += Tick; }
    }
    private static void Log(string message, string trace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        failed = true;
        if (output != null) File.AppendAllText(Path.Combine(output, "runtime-errors.txt"), message + "\n" + trace + "\n");
    }
    private static void Check(bool value, string label)
    {
        if (!value) throw new Exception("FAIL: " + label);
        checks.Add("PASS: " + label);
        Debug.Log("PASS: " + label);
    }
    public static void RunRules()
    {
        foreach (var direction in new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, -1) })
        {
            var state = new PieceType[7, 6]; int startY = direction.y < 0 ? 3 : 0;
            for (int i = 0; i < 4; i++) state[i * direction.x, startY + i * direction.y] = PieceType.Player1;
            var board = new AIBoard(state);
            Check(board.CheckWinner(PieceType.Player1), "Win direction " + direction);
            Check(!board.CheckWinner(PieceType.Player2), "No false opponent win " + direction);
        }
        var draw = new PieceType[7, 6];
        for (int c = 0; c < 7; c++) for (int r = 0; r < 6; r++) draw[c,r] = ((c + r / 2) % 2 == 0) ? PieceType.Player1 : PieceType.Player2;
        var full = new AIBoard(draw);
        Check(!full.HasValidMoves() && !full.CheckWinner(PieceType.Player1) && !full.CheckWinner(PieceType.Player2), "Full-board draw");
        Check(AISearch.FindBestMove(draw, CancellationToken.None, 3) == -1, "AI full-board termination");
        Check(AISearch.FindBestMoveTimed(draw, CancellationToken.None) == -1, "Timed Hard AI full-board termination");
        var threat = new PieceType[7, 6];
        threat[0,0] = threat[1,0] = threat[2,0] = PieceType.Player1;
        Check(AISearch.FindBestMove(threat, CancellationToken.None, 3) == 3, "AI blocks immediate loss");
        threat[0,0] = threat[1,0] = threat[2,0] = PieceType.Player2;
        Check(AISearch.FindBestMove(threat, CancellationToken.None, 3) == 3, "AI takes immediate win");
        Check(AISearch.FindBestMoveTimed(threat, CancellationToken.None) == 3, "Timed Hard AI keeps immediate win");
        threat[0,0] = threat[1,0] = threat[2,0] = PieceType.Player1;
        Check(AISearch.FindBestMoveTimed(threat, CancellationToken.None) == 3, "Timed Hard AI keeps immediate block");
        var cancel = new CancellationTokenSource(); cancel.Cancel(); bool cancelled = false;
        try { AISearch.FindBestMoveTimed(threat, cancel.Token); } catch (OperationCanceledException) { cancelled = true; }
        Check(cancelled, "AI cancellation"); cancel.Dispose();
        foreach (bool rewardFirst in new[] { true, false })
        {
            var tx = new RewardTransaction();
            if (rewardFirst) { tx.Earn(); Check(!tx.TryConsume(), "No reward while ad open"); tx.Close(false); }
            else { tx.Close(false); Check(!tx.TryConsume(), "No reward without earning"); tx.Earn(); }
            Check(tx.TryConsume() && !tx.TryConsume(), "Exactly one reward, order=" + rewardFirst);
        }
        var failedReward = new RewardTransaction(); failedReward.Earn(); failedReward.Close(true);
        Check(!failedReward.TryConsume(), "Failed ad cannot reward");
        var evaluationBoard = new AIBoard(new PieceType[7, 6]); evaluationBoard.EvaluateBoard();
        long startBytes = GC.GetAllocatedBytesForCurrentThread(); var watch = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++) evaluationBoard.EvaluateBoard();
        watch.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;
        File.WriteAllText(Path.Combine(output, "evaluator.json"), "{\"iterations\":1000,\"elapsed_ms\":" + watch.Elapsed.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"allocated_bytes_including_stopwatch\":" + allocated + "}");
        File.WriteAllLines(Path.Combine(output, "rule-checks.txt"), checks);
    }
    private static void AuditReferences()
    {
        var lines = new List<string>();
        foreach (string scenePath in new[] { "Assets/Scenes/Splash.unity", "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameScene.unity" })
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                Check(count == 0, scene.name + "/" + transform.name + " no missing scripts");
            }
            lines.Add(scenePath + " dependencies: " + AssetDatabase.GetDependencies(scenePath).Length);
        }
        File.WriteAllLines(Path.Combine(output, "scene-audit.txt"), lines);
    }
    private static void Capture(string name)
    {
        ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 1.5;
        try
        {
            if (phase == 0) { Check(FusionUI.Instance != null, "Main menu presentation boot"); Capture("01-menu-en"); phase++; }
            else if (phase == 1) { Localization.SetLanguage("es"); phase++; }
            else if (phase == 2) { Capture("02-menu-es"); Localization.SetLanguage("fr"); phase++; }
            else if (phase == 3) { Capture("03-menu-fr"); Localization.SetLanguage("pt"); phase++; }
            else if (phase == 4) { Capture("04-menu-pt"); Localization.SetLanguage("ar"); phase++; }
            else if (phase == 5) { Capture("05-menu-ar"); Localization.SetLanguage("hi"); phase++; }
            else if (phase == 6) { Capture("06-menu-hi"); Localization.SetLanguage("en"); GameSettings.GameMode = GameMode.HumanVsHuman; SceneTransition.Load("GameScene"); phase++; next += 3; }
            else if (phase == 7)
            {
                Check(BoardManager.Instance != null && BoardManager.Instance.Columns == 7 && BoardManager.Instance.Rows == 6, "Original board dimensions");
                Check(PieceThemeManager.Instance != null && AudioManager.Instance != null && AdManager.Instance != null, "Persistent services after scene load");
                Check(!BoardManager.Instance.TryDropPiece(-1, false) && !BoardManager.Instance.TryDropPiece(7, false), "Bounds protection");
                Check(BoardManager.Instance.TryDropPiece(0, false), "First move accepted");
                Check(!BoardManager.Instance.TryDropPiece(0, false), "Rapid duplicate drop blocked");
                Check(!BoardManager.Instance.CanUndo(), "Undo blocked during drop");
                moveIndex = 1; phase++;
            }
            else if (phase == 8)
            {
                if (BoardManager.Instance.IsBusy) return;
                if (moveIndex < WinningSequence.Length) { BoardManager.Instance.DropPiece(WinningSequence[moveIndex++]); return; }
                Check(GameManager.Instance.IsGameOver, "Original board detects vertical victory"); phase++; next += 2;
            }
            else if (phase == 9) { Capture("07-result"); BoardManager.Instance.UndoLastTwoMoves(); phase++; }
            else if (phase == 10)
            {
                Check(!GameManager.Instance.IsGameOver && BoardManager.Instance.MoveCount == 5 && BoardManager.Instance.CurrentPlayer == PieceType.Player2, "Result undo restores correct turn and count");
                Capture("08-gameplay"); GameManager.Instance.Home(); phase++; next += 3;
            }
            else if (phase == 11)
            {
                Check(UnityEngine.Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length == 1, "No duplicate audio service");
                Check(UnityEngine.Object.FindObjectsByType<AdManager>(FindObjectsSortMode.None).Length == 1, "No duplicate ad service");
                Check(UnityEngine.Object.FindObjectsByType<PieceThemeManager>(FindObjectsSortMode.None).Length == 1, "No duplicate theme service");
                File.WriteAllLines(Path.Combine(output, "runtime-checks.txt"), checks);
                Finish(failed ? 1 : 0);
            }
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    private static void Finish(int code)
    {
        SessionState.SetBool("FusionValidation", false);
        EditorApplication.update -= Tick;
        File.WriteAllText(Path.Combine(output, "result.txt"), code == 0 ? "PASS" : "FAIL");
        EditorApplication.Exit(code);
    }
}
