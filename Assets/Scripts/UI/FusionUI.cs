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
    private VisualElement root, safe, content, stage, columnTargets, overlay, boardFrame;
    private SpriteRenderer scenery;
    private Sprite scenerySprite;
    private Sprite boardBackingSprite;
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
            // A dark backing behind the transparent sockets keeps pieces readable.
            // It sits below the existing cell and animated piece renderers.
            var backing = new GameObject("Board backdrop").AddComponent<SpriteRenderer>();
            backing.transform.SetParent(transform, false);
            backing.transform.localScale = new Vector3(7, 6, 1);
            boardBackingSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2, 0, SpriteMeshType.FullRect);
            backing.sprite = boardBackingSprite;
            backing.color = new Color(.008f, .025f, .11f);
            backing.sortingOrder = -10;
            foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (renderer.name == "Bg") renderer.enabled = false;
            var texture = FusionArt.Load("Backdrop");
            if (texture != null && boardCamera != null)
            {
                scenery = new GameObject("Four Fusion scenery").AddComponent<SpriteRenderer>();
                scenery.transform.SetParent(boardCamera.transform, false);
                scenery.transform.localPosition = new Vector3(0, 0, 20);
                scenerySprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                scenery.sprite = scenerySprite; scenery.sortingOrder = -1000;
            }
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
        if (scenery != null) Destroy(scenery.gameObject);
        if (scenerySprite != null) Destroy(scenerySprite);
        if (boardBackingSprite != null) Destroy(boardBackingSprite);
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
        foreach (string view in new[] { "home", "mode", "difficulty", "settings", "help", "about" }) root.EnableInClassList("view-" + view, !gameScene && page == view);
        root.EnableInClassList("game-view", gameScene);
        root.EnableInClassList("rtl", Localization.IsRTL);
        root.languageDirection = Localization.IsRTL ? LanguageDirection.RTL : LanguageDirection.LTR;
        root.style.unityFontDefinition = FontDefinition.FromSDFFont(GetFont(Localization.Language));
        if (!gameScene) Art(root, "Backdrop", "screen-backdrop");
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
        safe.style.paddingLeft = lastSafe.xMin * scale + 16;
        safe.style.paddingRight = (Screen.width - lastSafe.xMax) * scale + 16;
        safe.style.paddingTop = (Screen.height - lastSafe.yMax) * scale + 10;
        // Standard AdMob banner is 50 density-independent pixels; reserve a conservative
        // screen fraction even before the asynchronous banner loads.
        float bannerReserve = gameScene && !result.HasValue ? 10 : Mathf.Max(62, 60 * Screen.dpi / 160f * scale);
        safe.style.paddingBottom = lastSafe.yMin * scale + bannerReserve;
        root.EnableInClassList("compact", Screen.height / (float)Screen.width < 1.85f);
        root.EnableInClassList("landscape", Screen.width > Screen.height);
        if (overlay != null)
        {
            overlay.style.paddingLeft = lastSafe.xMin * scale + 16;
            overlay.style.paddingRight = (Screen.width - lastSafe.xMax) * scale + 16;
            overlay.style.paddingTop = (Screen.height - lastSafe.yMax) * scale + 12;
            overlay.style.paddingBottom = lastSafe.yMin * scale + bannerReserve;
        }
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
        var button = new Button(() => { if (SceneTransition.IsLoading || (AdManager.Instance != null && AdManager.Instance.IsShowingAd)) return; AudioManager.Instance?.PlayButtonClick(); action(); }) { name = key };
        button.AddToClassList("action"); button.AddToClassList(cls); parent.Add(button);
        if (cls != "quiet" && cls != "card" && cls != "theme") button.Add(new FusionSurface(cls == "primary" ? FusionTone.Gold : cls == "danger" || cls == "rematch" ? FusionTone.Red : FusionTone.Blue, cls == "back" || cls == "information"));
        string icon = key switch { "MENU_PLAY" or "BUTTON_START" or "BUTTON_REMATCH" or "BUTTON_RESUME" => "play", "MENU_SETTINGS" => "settings", "MENU_EXIT" => "exit", "BUTTON_BACK" => "back", "BUTTON_RESTART" => "restart", "BUTTON_UNDO" => "undo", "BUTTON_REWARDED_UNDO" => "ad", "BUTTON_PAUSE" => "pause", "BUTTON_HOME" => "home", "PRIVACY_POLICY" or "PRIVACY_OPTIONS" or "ABOUT_TITLE" => "info", _ => null };
        if (icon != null && cls != "quiet") { button.Add(new FusionIcon(icon, cls == "primary" || cls == "rematch" ? new Color(.2f,.08f,.005f) : Color.white)); button.AddToClassList("has-icon"); }
        var caption = Text(button, key, "action-label"); caption.name = "caption";
        return button;
    }
    private VisualElement Art(VisualElement parent, string asset, string classes)
    {
        var art = Element(parent, classes); art.pickingMode = PickingMode.Ignore;
        var texture = FusionArt.Load(asset);
        if (texture != null) art.style.backgroundImage = new StyleBackground(texture);
        return art;
    }
    private void Heading(VisualElement parent, string key)
    {
        Text(parent, key, "heading"); var line = Element(parent, "heading-rule"); line.pickingMode = PickingMode.Ignore;
        Element(line, "rule-gold"); Element(line, "rule-blue");
    }
    private void Section(VisualElement parent, string key, string icon)
    {
        var row = Element(parent, "section-heading"); row.Add(new FusionIcon(icon, FusionSurface.Edge(FusionTone.Gold))); Text(row, key, "section-title");
    }
    private void Selection(VisualElement parent)
    {
        var check = Element(parent, "selection-check"); check.Add(new FusionIcon("check", new Color(.12f,.08f,.015f)));
    }
    private void Brand(VisualElement parent, bool large)
    {
        var brand = Element(parent, large ? "brand hero" : "brand");
        Art(brand, "Logo", "brand-image");
    }
    private void Navigate(string next) { page = next; Rebuild(); }
    private void BuildMenu()
    {
        var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("screen-scroll"); scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden; scroll.verticalScrollerVisibility = page == "settings" ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden; content.Add(scroll);
        scroll.verticalScroller.style.width = 4;
        scroll.verticalScroller.style.minWidth = 4;
        scroll.verticalScroller.style.maxWidth = 4;
        var body = scroll.contentContainer;
        if (page == "home")
        {
            var showcase = Element(body, "home-showcase");
            Brand(showcase, true);
            Text(showcase, "TAGLINE", "tagline");
            Art(showcase, "BoardHero", "home-illustration");
            var actions = Element(body, "home-actions");
            Action(actions, "MENU_PLAY", () => Navigate("mode"), "primary");
            Action(actions, "MENU_SETTINGS", () => Navigate("settings"));
            Action(actions, "MENU_EXIT", () => { AudioManager.Instance?.SaveSettings(); Application.Quit(); }, "danger");
            var links = Element(body, "home-links");
            Action(links, "MENU_HELP", () => Navigate("help"), "quiet");
            Literal(links, "MN7 STUDIO", "footer");
        }
        else
        {
            var top = Element(body, "menu-top");
            Brand(top, false);
            Action(top, "BUTTON_BACK", Back, "back");
            if (page == "mode")
            {
                var marks = Element(body, "brand-marks row"); for (int i=0;i<4;i++) Element(marks, i==3 ? "dot gold" : "dot coral");
                Heading(body, "MODE_TITLE");
                Card(body, "MODE_HUMAN_VS_HUMAN", "MODE_LOCAL_DETAIL", () => { GameSettings.GameMode = GameMode.HumanVsHuman; SceneTransition.Load("GameScene"); }, "01", "LocalPlayers", FusionTone.Gold);
                Card(body, "MODE_HUMAN_VS_AI", "MODE_AI_DETAIL", () => { GameSettings.GameMode = GameMode.HumanVsAI; Navigate("difficulty"); }, "02", "Robot", FusionTone.Blue);
            }
            else if (page == "difficulty")
            {
                Heading(body, "DIFFICULTY_TITLE");
                for (int i = 0; i < 3; i++)
                {
                    var difficulty = (Difficulty)i;
                    var card = Card(body, DifficultyKey(difficulty), new[] { "EASY_DETAIL", "MEDIUM_DETAIL", "HARD_DETAIL" }[i], () => { selectedDifficulty = difficulty; Rebuild(); }, "", "BoardHero", i == 0 ? FusionTone.Green : i == 1 ? FusionTone.Blue : FusionTone.Red);
                    card.AddToClassList("difficulty-card");
                    card.EnableInClassList("selected", selectedDifficulty == difficulty);
                    card.Q<FusionSurface>().Accent(selectedDifficulty == difficulty);
                    var copy = card.Q<VisualElement>("card-copy");
                    var stars = Element(copy, "rating row"); for(int star=0;star<3;star++) stars.Add(new FusionIcon("star", star <= i ? FusionSurface.Edge(FusionTone.Gold) : new Color(.23f,.34f,.49f)));
                    if (selectedDifficulty == difficulty) { Selection(card); Text(copy, "SELECTED", "selection-label"); }
                }
                Action(body, "BUTTON_START", () => { GameSettings.Difficulty = selectedDifficulty; GameSettings.GameMode = GameMode.HumanVsAI; SceneTransition.Load("GameScene"); }, "primary");
            }
            else if (page == "settings") BuildSettings(body);
            else
            {
                Heading(body, page == "help" ? "MENU_HELP" : "ABOUT_TITLE");
                var info = Element(body, "info-panel"); info.Add(new FusionSurface(FusionTone.Blue, true));
                Text(info, page == "help" ? "HELP_BODY" : "ABOUT_BODY");
                if (page == "help") Text(info, "HELP_UNDO");
            }
        }
        content.AddToClassList("enter");
        content.schedule.Execute(() => content.RemoveFromClassList("enter"));
    }
    private Button Card(VisualElement parent, string title, string detail, Action action, string mark, string artwork, FusionTone tone)
    {
        var card = Action(parent, title, action, "card"); card.Q<Label>("caption").RemoveFromHierarchy();
        card.Add(new FusionSurface(tone, true));
        Art(card, artwork, "card-art");
        var copy = Element(card, "card-copy"); copy.name = "card-copy";
        if (!string.IsNullOrEmpty(mark)) Literal(copy, mark, "card-mark");
        Text(copy, title, "card-title"); Text(copy, detail, "muted");
        var arrow = Element(card, "card-arrow"); arrow.Add(new FusionSurface(tone)); arrow.Add(new FusionIcon("next", new Color(.015f,.025f,.07f)));
        return card;
    }
    private void BuildSettings(VisualElement body)
    {
        Heading(body, "MENU_SETTINGS");
        Section(body, "AUDIO_TITLE", "sound");
        var audio = Element(body, "settings-card");
        audio.Add(new FusionSurface(FusionTone.Blue, true));
        Toggle(audio, "AUDIO_MUSIC", PlayerSettingsStore.MusicEnabled, PlayerSettingsStore.SetMusic);
        Toggle(audio, "AUDIO_SFX", PlayerSettingsStore.SoundEnabled, PlayerSettingsStore.SetSound);
        Text(audio, "AUDIO_VOLUME", "setting-label");
        var volume = new Slider(0, 1) { value = AudioManager.Instance != null ? AudioManager.Instance.GetMasterVolume() : PlayerPrefs.GetFloat("MasterVolume", 1) };
        volume.AddToClassList("volume");
        volume.RegisterValueChangedCallback(evt => AudioManager.Instance?.SetMasterVolume(evt.newValue)); audio.Add(volume);
        Section(body, "MENU_LANGUAGE", "language");
        var languages = Element(body, "language-carousel");
        languages.languageDirection = LanguageDirection.LTR;
        LanguageArrow(languages, -1, "back");
        int languageIndex = Array.IndexOf(Localization.Codes, Localization.Language);
        if (languageIndex < 0) languageIndex = 0;
        var currentLanguage = Element(languages, "language-current");
        currentLanguage.Add(new FusionSurface(FusionTone.Blue, true));
        var languageName = Literal(currentLanguage, Localization.Names[languageIndex], "language-name");
        languageName.style.unityFontDefinition = FontDefinition.FromSDFFont(GetFont(Localization.Codes[languageIndex]));
        languageName.languageDirection = Localization.IsRTL ? LanguageDirection.RTL : LanguageDirection.LTR;
        LanguageArrow(languages, 1, "next");
        Section(body, "VISUAL_TITLE", "pieces");
        var themes = Element(body, "row");
        for (int i = 0; i < 2; i++)
        {
            var theme = (PieceTheme)i;
            var button = Action(themes, i == 0 ? "THEME_ONE" : "THEME_TWO", () => { PlayerSettingsStore.SetTheme(theme); Rebuild(); }, "theme");
            button.Q<Label>("caption").RemoveFromHierarchy();
            var surface = new FusionSurface(FusionTone.Blue, true); surface.Accent(GameSettings.Theme == theme); button.Add(surface);
            var preview = Element(button, "row swatches");
            ThemeDisc(preview, theme, false, "dot"); ThemeDisc(preview, theme, true, "dot");
            Text(button, i == 0 ? "THEME_ONE" : "THEME_TWO", "player-name");
            button.EnableInClassList("selected", GameSettings.Theme == theme);
            if(GameSettings.Theme == theme) Selection(button);
        }
        Section(body, "OTHER_TITLE", "info");
        Action(body, "PRIVACY_POLICY", () => Application.OpenURL(PlayerSettingsStore.PrivacyUrl), "information");
        if (AdManager.Instance != null && AdManager.Instance.PrivacyOptionsRequired) Action(body, "PRIVACY_OPTIONS", () => AdManager.Instance.ShowPrivacyOptions(), "information");
        Action(body, "ABOUT_TITLE", () => Navigate("about"), "information");
        Literal(body, Localization.Get("VERSION") + " " + Application.version, "footer");
    }
    private void LanguageArrow(VisualElement parent, int direction, string icon)
    {
        var button = new Button(() => CycleLanguage(direction)) { name = direction < 0 ? "language-previous" : "language-next" };
        button.AddToClassList("action"); button.AddToClassList("language-arrow");
        button.Add(new FusionSurface(FusionTone.Blue, true));
        button.Add(new FusionIcon(icon));
        parent.Add(button);
    }
    private void CycleLanguage(int direction)
    {
        if (Localization.Codes.Length == 0) return;
        AudioManager.Instance?.PlayButtonClick();
        int index = Array.IndexOf(Localization.Codes, Localization.Language);
        if (index < 0) index = 0;
        index = (index + direction + Localization.Codes.Length) % Localization.Codes.Length;
        Localization.SetLanguage(Localization.Codes[index]);
    }
    private void Toggle(VisualElement parent, string key, bool value, Action<bool> change)
    {
        var toggle = new Toggle(Localization.Get(key)) { value = value }; toggle.AddToClassList("setting-toggle");
        toggle.name = key;
        var checkmark = toggle.Q(className: "unity-toggle__checkmark"); if (checkmark != null) Element(checkmark, "switch-thumb").pickingMode = PickingMode.Ignore;
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
        boardFrame = new FusionBoardFrame(); stage.Add(boardFrame);
        columnTargets = Element(stage, "column-targets");
        for (int i = 0; i < 7; i++)
        {
            int column = i;
            var button = new Button(() => BoardManager.Instance?.DropPiece(column)) { name = "column-" + i, tooltip = (i + 1).ToString() };
            button.AddToClassList("column-target");
            Element(button, "column-cue").pickingMode = PickingMode.Ignore;
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
        card.Add(new FusionSurface(FusionTone.Blue, true));
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
        playerOne.Q<FusionSurface>().Accent(current == PieceType.Player1);
        playerTwo.Q<FusionSurface>().Accent(current == PieceType.Player2);
        bool blocked = board == null || game == null || game.InputBlocked || board.IsBusy || (GameSettings.GameMode == GameMode.HumanVsAI && current == PieceType.Player2);
        for (int i = 0; i < columns.Count; i++) columns[i].SetEnabled(!blocked && board.IsValidMove(i));
        if (undo != null)
        {
            undo.SetEnabled(!blocked && board.CanUndo());
            undo.Q<Label>("caption").text = Localization.Get(game != null && game.FreeUndosRemaining == 0 ? "BUTTON_REWARDED_UNDO" : "BUTTON_UNDO");
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
        Rect stageBounds = stage.worldBound;
        Rect bounds = new Rect(stageBounds.x, stageBounds.y + 24, stageBounds.width, Mathf.Max(1, stageBounds.height - 40));
        float panelHeight = root.resolvedStyle.height, panelWidth = root.resolvedStyle.width;
        // Geometry can be undefined for one layout pass during resolution/language changes.
        if (!(bounds.width > 0 && bounds.height > 0 && panelHeight > 0 && panelWidth > 0)) return;
        float unitsPerPixel = Mathf.Max(7.5f / bounds.width, 6.4f / bounds.height);
        boardCamera.orthographicSize = panelHeight * unitsPerPixel * .5f;
        boardCamera.transform.position = new Vector3((panelWidth * .5f - bounds.center.x) * unitsPerPixel, (bounds.center.y - panelHeight * .5f) * unitsPerPixel, -10);
        float width = 7 / unitsPerPixel, height = 6 / unitsPerPixel;
        columnTargets.style.width = width; columnTargets.style.height = height;
        float left = (bounds.width - width) * .5f, top = 24 + (bounds.height - height) * .5f;
        columnTargets.style.left = left; columnTargets.style.top = top;
        boardFrame.style.left = left - 8; boardFrame.style.top = top - 8; boardFrame.style.width = width + 16; boardFrame.style.height = height + 16;
        if (scenery != null)
        {
            float scale = Mathf.Max(boardCamera.orthographicSize * 2 * boardCamera.aspect / scenerySprite.bounds.size.x, boardCamera.orthographicSize * 2 / scenerySprite.bounds.size.y);
            scenery.transform.localScale = new Vector3(scale, scale, 1);
        }
    }
    public void ShowResult(PieceType winner)
    {
        result = winner;
        string key = winner == PieceType.Empty ? "RESULT_DRAW" : GameSettings.GameMode == GameMode.HumanVsHuman ? (winner == PieceType.Player1 ? "RESULT_ONE" : "RESULT_TWO") : winner == PieceType.Player1 ? "RESULT_WIN" : "RESULT_LOSE";
        var modal = Modal(key);
        modal.AddToClassList("result-modal");
        bool lost = GameSettings.GameMode == GameMode.HumanVsAI && winner == PieceType.Player2;
        modal.EnableInClassList("result-lose", lost);
        modal.EnableInClassList("result-draw", winner == PieceType.Empty);
        var mark = Element(modal, "result-mark row");
        mark.Add(new FusionResultBurst());
        for (int i = 0; i < 4; i++)
        {
            var coin = ThemeDisc(mark, GameSettings.Theme, winner == PieceType.Empty ? i % 2 == 1 : !lost && winner == PieceType.Player2, "dot");
            if (lost && i == 3) { var crack = new FusionIcon("crack"); crack.AddToClassList("coin-crack"); coin.Add(crack); }
        }
        Text(modal, lost ? "RESULT_LOSE_DETAIL" : "RESULT_DETAIL", "muted");
        Action(modal, "BUTTON_REMATCH", () => GameManager.Instance?.RestartGame(), "rematch");
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
        modal.Add(new FusionSurface(FusionTone.Blue, true));
        Brand(modal, false);
        Text(modal, title, "heading");
        modal.AddToClassList("enter"); modal.schedule.Execute(() => modal.RemoveFromClassList("enter"));
        ApplySafeArea();
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
