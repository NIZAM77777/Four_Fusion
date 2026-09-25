using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

// Presentation only: the original board/managers remain responsible for rules and actions.
public sealed class FusionUI : MonoBehaviour
{
    public static FusionUI Instance { get; private set; }
    private static readonly Dictionary<string, FontAsset> fonts = new Dictionary<string, FontAsset>();
    private UIDocument document;
    private PanelSettings panel;
    private VisualElement root, safe, content, stage, columnTargets, overlay;
    private Label turnLabel, undoCount, toast;
    private Button undo;
    private VisualElement playerOne, playerTwo;
    private readonly List<Button> columns = new List<Button>();
    private string page = "home";
    private bool gameScene;
    private PieceType? result;
    private Rect lastSafe;
    private int lastWidth, lastHeight;
    private Difficulty selectedDifficulty = Difficulty.Easy;
    private Camera boardCamera;
    private AdManager subscribedAds;
    private IVisualElementScheduledItem toastDismiss;
    private static readonly Color Background = new Color(.035f, .067f, .13f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; fonts.Clear(); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register() { SceneManager.sceneLoaded -= Install; SceneManager.sceneLoaded += Install; }
    private static void Install(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu" && scene.name != "GameScene") return;
        if (AudioManager.Instance == null) SpawnService("AudioManager");
        if (AdManager.Instance == null) SpawnService("AdsManager");
        if (PieceThemeManager.Instance == null) SpawnService("PieceThemes");
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.gameObject.scene != scene) continue;
            canvas.enabled = false;
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = false;
        }
        var menu = FindFirstObjectByType<MainMenuManager>();
        if (menu != null) menu.enabled = false;
        new GameObject("Four Fusion UI").AddComponent<FusionUI>();
    }
    private static void SpawnService(string name)
    {
        var prefab = Resources.Load<GameObject>("Fusion/" + name);
        if (prefab != null) Instantiate(prefab);
        else Debug.LogWarning("Service prefab is missing; use Four Fusion > Generate Service Prefabs: " + name);
    }
    private void Awake()
    {
        Instance = this;
        gameScene = SceneManager.GetActiveScene().name == "GameScene";
        boardCamera = Camera.main;
        if (boardCamera != null)
        {
            boardCamera.clearFlags = CameraClearFlags.SolidColor;
            boardCamera.backgroundColor = Background;
            var fit = boardCamera.GetComponent<CameraController>();
            if (fit != null) fit.enabled = false;
        }
        if (gameScene)
        {
            foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (renderer.name == "Bg") renderer.enabled = false;
        }
        var panelTemplate = Resources.Load<PanelSettings>("Fusion/PanelSettings");
        if (panelTemplate == null) { Debug.LogError("Missing Fusion/PanelSettings asset. Generate service prefabs before building."); enabled = false; return; }
        panel = Instantiate(panelTemplate);
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(420, 900);
        panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panel.match = 0;
        panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("Fusion/FusionTheme");
        panel.sortingOrder = 100;
        document = gameObject.AddComponent<UIDocument>();
        document.panelSettings = panel;
        root = document.rootVisualElement;
        root.AddToClassList("fusion");
        root.styleSheets.Add(Resources.Load<StyleSheet>("Fusion/Fusion"));
        Localization.Changed += Rebuild;
        SceneTransition.Loading += ShowLoading;
        subscribedAds = AdManager.Instance;
        if (subscribedAds != null) subscribedAds.StateChanged += AdsChanged;
        Rebuild();
    }
    private void Start() { if (gameScene) AdManager.Instance?.HideBanner(); else AdManager.Instance?.ShowBanner(); }
    private void OnDestroy()
    {
        Localization.Changed -= Rebuild;
        SceneTransition.Loading -= ShowLoading;
        if (subscribedAds != null) subscribedAds.StateChanged -= AdsChanged;
        if (Instance == this) Instance = null;
        if (panel != null) Destroy(panel);
    }
    private void Update()
    {
        if (lastWidth != Screen.width || lastHeight != Screen.height || lastSafe != Screen.safeArea) ApplySafeArea();
        if (Input.GetKeyDown(KeyCode.Escape)) Back();
    }
    private static FontAsset GetFont(string code)
    {
        string name = Localization.FontName(code);
        if (!fonts.TryGetValue(name, out var font))
        {
            var source = Resources.Load<Font>("Fusion/Fonts/" + name + "-Regular");
            if (source == null) { Debug.LogError("Missing font: " + name); return null; }
            font = FontAsset.CreateFontAsset(source);
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.isMultiAtlasTexturesEnabled = true;
            fonts.Add(name, font);
        }
        return font;
    }
    private void Rebuild()
    {
        toastDismiss?.Pause();
        root.Clear();
        root.EnableInClassList("menu-background", !gameScene);
        root.EnableInClassList("rtl", Localization.IsRTL);
        root.languageDirection = Localization.IsRTL ? LanguageDirection.RTL : LanguageDirection.LTR;
        root.style.unityFontDefinition = FontDefinition.FromSDFFont(GetFont(Localization.Language));
        safe = Element(root, "safe");
        content = Element(safe, "content");
        overlay = null; stage = null; columns.Clear();
        toast = new Label(); toast.AddToClassList("toast"); toast.style.display = DisplayStyle.None;
        ApplySafeArea();
        if (gameScene) { BuildGameplay(); if (result.HasValue) ShowResult(result.Value); }
        else BuildMenu();
        root.Add(toast);
    }
    private void ApplySafeArea()
    {
        lastSafe = Screen.safeArea; lastWidth = Screen.width; lastHeight = Screen.height;
        if (safe == null || Screen.width == 0 || Screen.height == 0) return;
        panel.referenceResolution = Screen.width > Screen.height ? new Vector2Int(900, 480) : new Vector2Int(420, 900);
        float scale = panel.referenceResolution.x / (float)Screen.width;
        safe.style.paddingLeft = lastSafe.xMin * scale + 22;
        safe.style.paddingRight = (Screen.width - lastSafe.xMax) * scale + 22;
        safe.style.paddingTop = (Screen.height - lastSafe.yMax) * scale + 18;
        // Standard AdMob banner is 50 density-independent pixels; reserve a conservative
        // screen fraction even before the asynchronous banner loads.
        float bannerReserve = gameScene && !result.HasValue ? 10 : Mathf.Max(62, 60 * Screen.dpi / 160f * scale);
        safe.style.paddingBottom = lastSafe.yMin * scale + bannerReserve;
        root.EnableInClassList("compact", Screen.height / (float)Screen.width < 1.7f);
        root.EnableInClassList("landscape", Screen.width > Screen.height);
        if (stage != null) stage.schedule.Execute(FitBoard);
    }
    private VisualElement Element(VisualElement parent, string classes)
    {
        var element = new VisualElement();
        foreach (string cls in classes.Split(' ')) element.AddToClassList(cls);
        parent.Add(element); return element;
    }
    private Label Text(VisualElement parent, string key, string cls = "body")
    {
        var label = new Label(Localization.Get(key)); label.AddToClassList(cls); label.pickingMode = PickingMode.Ignore;
        parent.Add(label); return label;
    }
    private Label Literal(VisualElement parent, string text, string cls)
    {
        var label = new Label(text); label.AddToClassList(cls); label.pickingMode = PickingMode.Ignore;
        // Leave space around native-script ascenders and descenders at small sizes.
        if (cls == "footer") { label.style.paddingTop = 4; label.style.paddingBottom = 4; }
        parent.Add(label); return label;
    }
    private Button Action(VisualElement parent, string key, Action action, string cls = "secondary")
    {
        var button = new Button(() => { if (SceneTransition.IsLoading || (AdManager.Instance != null && AdManager.Instance.IsShowingAd)) return; AudioManager.Instance?.PlayButtonClick(); action(); }) { text = Localization.Get(key), name = key };
        button.AddToClassList("action"); button.AddToClassList(cls); parent.Add(button); return button;
    }
    private void Brand(VisualElement parent, bool large)
    {
        var brand = Element(parent, large ? "brand hero" : "brand");
        var marks = Element(brand, "brand-marks row");
        for (int i = 0; i < 4; i++) Element(marks, i == 3 ? "dot gold" : "dot coral");
        var title = Literal(brand, "FOUR FUSION", large ? "wordmark" : "wordmark-small");
        title.languageDirection = LanguageDirection.LTR;
        title.style.unityFontDefinition = FontDefinition.FromSDFFont(GetFont("en"));
    }
    private void Navigate(string next) { page = next; Rebuild(); }
    private void BuildMenu()
    {
        var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("screen-scroll"); scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden; scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden; content.Add(scroll);
        var body = scroll.contentContainer;
        if (page == "home")
        {
            Brand(body, true);
            Text(body, "TAGLINE", "tagline");
            var illustration = Element(body, "mini-board");
            for (int r = 0; r < 4; r++)
            {
                var row = Element(illustration, "mini-row");
                for (int c = 0; c < 5; c++) Element(row, "mini-cell " + (r == 3 || (r == 2 && c == 2) ? (c % 2 == 0 ? "coral" : "gold") : "empty"));
            }
            Action(body, "MENU_PLAY", () => Navigate("mode"), "primary");
            Action(body, "MENU_SETTINGS", () => Navigate("settings"));
            Action(body, "MENU_HELP", () => Navigate("help"), "quiet");
            Literal(body, "MN7 STUDIO", "footer");
        }
        else
        {
            Brand(body, false);
            Action(body, "BUTTON_BACK", Back, "back");
            if (page == "mode")
            {
                Text(body, "MODE_TITLE", "heading");
                Card(body, "MODE_HUMAN_VS_HUMAN", "MODE_LOCAL_DETAIL", () => { GameSettings.GameMode = GameMode.HumanVsHuman; SceneTransition.Load("GameScene"); }, "01");
                Card(body, "MODE_HUMAN_VS_AI", "MODE_AI_DETAIL", () => { GameSettings.GameMode = GameMode.HumanVsAI; Navigate("difficulty"); }, "02");
            }
            else if (page == "difficulty")
            {
                Text(body, "DIFFICULTY_TITLE", "heading");
                for (int i = 0; i < 3; i++)
                {
                    var difficulty = (Difficulty)i;
                    var card = Card(body, DifficultyKey(difficulty), new[] { "EASY_DETAIL", "MEDIUM_DETAIL", "HARD_DETAIL" }[i], () => { selectedDifficulty = difficulty; Rebuild(); }, new string('•', i + 1));
                    card.AddToClassList("difficulty-card");
                    card.EnableInClassList("selected", selectedDifficulty == difficulty);
                    if (selectedDifficulty == difficulty) Text(card, "SELECTED", "selection-label");
                }
                Action(body, "BUTTON_START", () => { GameSettings.Difficulty = selectedDifficulty; GameSettings.GameMode = GameMode.HumanVsAI; SceneTransition.Load("GameScene"); }, "primary");
            }
            else if (page == "settings") BuildSettings(body);
            else
            {
                Text(body, page == "help" ? "MENU_HELP" : "ABOUT_TITLE", "heading");
                Text(body, page == "help" ? "HELP_BODY" : "ABOUT_BODY");
                if (page == "help") Text(body, "HELP_UNDO");
            }
        }
        content.AddToClassList("enter");
        content.schedule.Execute(() => content.RemoveFromClassList("enter"));
    }
    private Button Card(VisualElement parent, string title, string detail, Action action, string mark)
    {
        var card = Action(parent, title, action, "card"); card.text = "";
        Literal(card, mark, "card-mark");
        Text(card, title, "card-title"); Text(card, detail, "muted");
        return card;
    }
    private void BuildSettings(VisualElement body)
    {
        Text(body, "MENU_SETTINGS", "heading");
        Text(body, "AUDIO_TITLE", "section-title");
        var audio = Element(body, "settings-card");
        Toggle(audio, "AUDIO_MUSIC", PlayerSettingsStore.MusicEnabled, PlayerSettingsStore.SetMusic);
        Toggle(audio, "AUDIO_SFX", PlayerSettingsStore.SoundEnabled, PlayerSettingsStore.SetSound);
        Text(audio, "AUDIO_VOLUME", "setting-label");
        var volume = new Slider(0, 1) { value = AudioManager.Instance != null ? AudioManager.Instance.GetMasterVolume() : PlayerPrefs.GetFloat("MasterVolume", 1) };
        volume.AddToClassList("volume");
        volume.RegisterValueChangedCallback(evt => AudioManager.Instance?.SetMasterVolume(evt.newValue)); audio.Add(volume);
        Text(body, "MENU_LANGUAGE", "section-title");
        var languages = Element(body, "language-grid");
        for (int i = 0; i < Localization.Codes.Length; i++)
        {
            string code = Localization.Codes[i];
            var button = new Button(() => { AudioManager.Instance?.PlayButtonClick(); Localization.SetLanguage(code); }) { text = Localization.Names[i], name = "language-" + code };
            button.style.unityFontDefinition = FontDefinition.FromSDFFont(GetFont(code));
            button.languageDirection = code == "ar" || code == "ur" ? LanguageDirection.RTL : LanguageDirection.LTR;
            button.AddToClassList("action"); button.AddToClassList("language");
            button.EnableInClassList("selected", code == Localization.Language); languages.Add(button);
        }
        Text(body, "VISUAL_TITLE", "section-title");
        var themes = Element(body, "row");
        for (int i = 0; i < 2; i++)
        {
            var theme = (PieceTheme)i;
            var button = Action(themes, i == 0 ? "THEME_ONE" : "THEME_TWO", () => { PlayerSettingsStore.SetTheme(theme); Rebuild(); }, "theme");
            button.text = "";
            Text(button, i == 0 ? "THEME_ONE" : "THEME_TWO", "player-name");
            var preview = Element(button, "row swatches");
            ThemeDisc(preview, theme, false, "dot"); ThemeDisc(preview, theme, true, "dot");
            button.EnableInClassList("selected", GameSettings.Theme == theme);
        }
        Text(body, "OTHER_TITLE", "section-title");
        Action(body, "PRIVACY_POLICY", () => Application.OpenURL(PlayerSettingsStore.PrivacyUrl), "quiet");
        if (AdManager.Instance != null && AdManager.Instance.PrivacyOptionsRequired) Action(body, "PRIVACY_OPTIONS", () => AdManager.Instance.ShowPrivacyOptions(), "quiet");
        Action(body, "ABOUT_TITLE", () => Navigate("about"), "quiet");
        Literal(body, Localization.Get("VERSION") + " " + Application.version, "footer");
    }
    private void Toggle(VisualElement parent, string key, bool value, Action<bool> change)
    {
        var toggle = new Toggle(Localization.Get(key)) { value = value }; toggle.AddToClassList("setting-toggle");
        toggle.RegisterValueChangedCallback(evt => { AudioManager.Instance?.PlayButtonClick(); change(evt.newValue); }); parent.Add(toggle);
    }
    private static string DifficultyKey(Difficulty value) => value switch { Difficulty.Medium => "DIFFICULTY_MEDIUM", Difficulty.Hard => "DIFFICULTY_HARD", _ => "DIFFICULTY_EASY" };
    private void BuildGameplay()
    {
        var header = Element(content, "game-header");
        Brand(header, false);
        var players = Element(header, "row players");
        playerOne = PlayerCard(players, "1", GameSettings.GameMode == GameMode.HumanVsAI ? "PLAYER_YOU" : "PLAYER_ONE", false);
        playerTwo = PlayerCard(players, "2", GameSettings.GameMode == GameMode.HumanVsAI ? "PLAYER_AI" : "PLAYER_TWO", true);
        turnLabel = Text(header, "TURN_ONE", "turn");
        if (GameSettings.GameMode == GameMode.HumanVsAI) Text(header, DifficultyKey(GameSettings.Difficulty), "difficulty-badge");
        stage = Element(content, "board-stage");
        columnTargets = Element(stage, "column-targets");
        for (int i = 0; i < 7; i++)
        {
            int column = i;
            var button = new Button(() => BoardManager.Instance?.DropPiece(column)) { name = "column-" + i, tooltip = (i + 1).ToString() };
            button.AddToClassList("column-target");
            columnTargets.Add(button); columns.Add(button);
        }
        stage.RegisterCallback<GeometryChangedEvent>(_ => FitBoard());
        var footer = Element(content, "game-footer");
        Text(footer, "DROP_HINT", "hint");
        undoCount = Literal(footer, "", "undo-count");
        undo = Action(footer, "BUTTON_UNDO", () => GameManager.Instance?.UndoMove(), "undo");
        var actions = Element(footer, "row");
        Action(actions, "BUTTON_RESTART", () => Confirm("RESTART_CONFIRM", () => GameManager.Instance?.RestartGame()), "compact-action");
        Action(actions, "BUTTON_PAUSE", () => { GameManager.Instance?.SetPaused(true); RefreshGameplay(); }, "compact-action");
        Action(actions, "BUTTON_HOME", () => Confirm("HOME_CONFIRM", () => GameManager.Instance?.Home()), "compact-action");
        RefreshGameplay();
    }
    private VisualElement PlayerCard(VisualElement parent, string number, string name, bool second)
    {
        var card = Element(parent, "player-card");
        var dot = ThemeDisc(card, GameSettings.Theme, second, "player-disc");
        Literal(dot, number, "disc-number"); Text(card, name, "player-name"); return card;
    }
    private VisualElement ThemeDisc(VisualElement parent, PieceTheme theme, bool second, string cls)
    {
        var disc = Element(parent, cls);
        var sprite = PieceThemeManager.Instance != null ? PieceThemeManager.Instance.GetThemeSprite(theme, second) : null;
        if (sprite != null) disc.style.backgroundImage = new StyleBackground(sprite);
        else disc.AddToClassList(second ? "gold" : "coral");
        return disc;
    }
    public void RefreshGameplay()
    {
        if (!gameScene || turnLabel == null) return;
        var board = BoardManager.Instance; var game = GameManager.Instance;
        PieceType current = board != null ? board.CurrentPlayer : PieceType.Player1;
        turnLabel.text = Localization.Get(GameSettings.GameMode == GameMode.HumanVsAI ? (current == PieceType.Player1 ? "TURN_YOU" : "TURN_AI") : (current == PieceType.Player1 ? "TURN_ONE" : "TURN_TWO"));
        playerOne.EnableInClassList("active-player", current == PieceType.Player1);
        playerTwo.EnableInClassList("active-player", current == PieceType.Player2);
        bool blocked = board == null || game == null || game.InputBlocked || board.IsBusy || (GameSettings.GameMode == GameMode.HumanVsAI && current == PieceType.Player2);
        for (int i = 0; i < columns.Count; i++) columns[i].SetEnabled(!blocked && board.IsValidMove(i));
        if (undo != null)
        {
            undo.SetEnabled(!blocked && board.CanUndo());
            undo.text = Localization.Get(game != null && game.FreeUndosRemaining == 0 ? "BUTTON_REWARDED_UNDO" : "BUTTON_UNDO");
            undoCount.text = Localization.Get("UNDO_FREE") + " · " + (game != null ? game.FreeUndosRemaining : 0);
        }
        if (game != null && game.IsPaused && overlay == null)
        {
            var modal = Modal("PAUSE_TITLE");
            Action(modal, "BUTTON_RESUME", () => { CloseModal(); game.SetPaused(false); }, "primary");
            Action(modal, "BUTTON_HOME", () => Confirm("HOME_CONFIRM", game.Home));
        }
    }
    private void FitBoard()
    {
        if (stage == null || boardCamera == null || stage.worldBound.width < 1 || stage.worldBound.height < 1) return;
        Rect bounds = stage.worldBound;
        float panelHeight = root.resolvedStyle.height, panelWidth = root.resolvedStyle.width;
        // Geometry can be undefined for one layout pass during resolution/language changes.
        if (!(bounds.width > 0 && bounds.height > 0 && panelHeight > 0 && panelWidth > 0)) return;
        float unitsPerPixel = Mathf.Max(7.4f / bounds.width, 6.5f / bounds.height);
        boardCamera.orthographicSize = panelHeight * unitsPerPixel * .5f;
        boardCamera.transform.position = new Vector3((panelWidth * .5f - bounds.center.x) * unitsPerPixel, (bounds.center.y - panelHeight * .5f) * unitsPerPixel, -10);
        float width = 7 / unitsPerPixel, height = 6 / unitsPerPixel;
        columnTargets.style.width = width; columnTargets.style.height = height;
        columnTargets.style.left = (bounds.width - width) * .5f; columnTargets.style.top = (bounds.height - height) * .5f;
    }
    public void ShowResult(PieceType winner)
    {
        result = winner;
        string key = winner == PieceType.Empty ? "RESULT_DRAW" : GameSettings.GameMode == GameMode.HumanVsHuman ? (winner == PieceType.Player1 ? "RESULT_ONE" : "RESULT_TWO") : winner == PieceType.Player1 ? "RESULT_WIN" : "RESULT_LOSE";
        var modal = Modal(key);
        var mark = Element(modal, "result-mark row");
        for (int i = 0; i < 4; i++)
            if (winner == PieceType.Empty) Element(mark, "dot blue");
            else ThemeDisc(mark, GameSettings.Theme, winner == PieceType.Player2, "dot");
        Text(modal, "RESULT_DETAIL", "muted");
        Action(modal, "BUTTON_REMATCH", () => GameManager.Instance?.RestartGame(), "primary");
        var reward = Action(modal, "BUTTON_REWARDED_UNDO", () => GameManager.Instance?.ResultPanelUndo());
        reward.SetEnabled(BoardManager.Instance != null && BoardManager.Instance.CanUndoFromResultPanel());
        Action(modal, "BUTTON_HOME", () => GameManager.Instance?.Home(), "quiet");
        ApplySafeArea();
    }
    public void ShowGameplay() { result = null; CloseModal(); ApplySafeArea(); RefreshGameplay(); }
    private VisualElement Modal(string title)
    {
        CloseModal();
        overlay = Element(root, "overlay");
        var scroll = new ScrollView(); scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden; scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden; scroll.AddToClassList("modal-scroll"); overlay.Add(scroll);
        var modal = Element(scroll.contentContainer, "modal");
        Text(modal, title, "heading");
        modal.AddToClassList("enter"); modal.schedule.Execute(() => modal.RemoveFromClassList("enter"));
        return modal;
    }
    private void CloseModal() { overlay?.RemoveFromHierarchy(); overlay = null; }
    private void Confirm(string key, Action confirm)
    {
        bool wasPaused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        GameManager.Instance?.SetPaused(true);
        var modal = Modal(key);
        Action(modal, key == "RESTART_CONFIRM" ? "BUTTON_RESTART" : "BUTTON_HOME", confirm, "primary");
        Action(modal, "BUTTON_CANCEL", () => { CloseModal(); GameManager.Instance?.SetPaused(wasPaused); });
    }
    public void Notify(string key)
    {
        if (toast == null) return;
        toast.text = Localization.Get(key); toast.style.display = DisplayStyle.Flex;
        toast.BringToFront();
        toastDismiss?.Pause();
        toastDismiss = toast.schedule.Execute(() => toast.style.display = DisplayStyle.None).StartingIn(3500);
    }
    private void ShowLoading() { var modal = Modal("LOADING"); modal.SetEnabled(false); }
    private void AdsChanged() { if (!gameScene && page == "settings") Rebuild(); else RefreshGameplay(); }
    private void Back()
    {
        if (SceneTransition.IsLoading || (AdManager.Instance != null && AdManager.Instance.IsShowingAd)) return;
        AudioManager.Instance?.SaveSettings();
        if (gameScene)
        {
            if (result.HasValue) return;
            if (overlay != null) { CloseModal(); GameManager.Instance?.SetPaused(false); }
            else { GameManager.Instance?.SetPaused(true); RefreshGameplay(); }
        }
        else Navigate(page == "difficulty" ? "mode" : page == "about" ? "settings" : "home");
    }
}
