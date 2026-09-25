using System;
using System.Threading;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;
#if UNITY_ANDROID && !UNITY_EDITOR && !DEVELOPMENT_BUILD
    private const string BannerId = "ca-app-pub-1729898546273793/6166062447";
    private const string InterstitialId = "ca-app-pub-1729898546273793/4852980773";
    private const string RewardedId = "ca-app-pub-1729898546273793/3668605627";
#else
    private const string BannerId = "ca-app-pub-3940256099942544/6300978111";
    private const string InterstitialId = "ca-app-pub-3940256099942544/1033173712";
    private const string RewardedId = "ca-app-pub-3940256099942544/5224354917";
#endif
    private BannerView banner;
    private InterstitialAd interstitial;
    private RewardedAd rewarded;
    private SynchronizationContext unityContext;
    private bool initialized, initializing, bannerWanted, loadingRewarded, loadingInterstitial;
    private bool consentPending;
    private float savedTimeScale = 1;
    public bool IsShowingAd { get; private set; }
    private static bool Supported => Application.isEditor || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
    public bool PrivacyOptionsRequired => Supported && ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
    public event Action StateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        unityContext = SynchronizationContext.Current;
    }
    private void Dispatch(Action action)
    {
        unityContext.Post(_ => { if (this != null && Instance == this) action(); }, null);
    }
    private void Start()
    {
        if (!Supported || consentPending) return;
        consentPending = true;
        ConsentInformation.Update(new ConsentRequestParameters(), error => Dispatch(() =>
        {
            if (error != null) { consentPending = false; Debug.LogWarning("Consent update: " + error.Message); InitializeAds(); return; }
            bool showingConsent = ConsentInformation.ConsentStatus == ConsentStatus.Required && !IsShowingAd;
            if (showingConsent) BeginFullScreen();
            ConsentForm.LoadAndShowConsentFormIfRequired(formError => Dispatch(() =>
            {
                consentPending = false;
                if (showingConsent) EndFullScreen();
                if (formError != null) Debug.LogWarning("Consent form: " + formError.Message);
                InitializeAds(); StateChanged?.Invoke();
            }));
        }));
        InitializeAds();
    }
    private void InitializeAds()
    {
        if (initialized || initializing || !ConsentInformation.CanRequestAds()) return;
        initializing = true;
        MobileAds.Initialize(_ => Dispatch(() =>
        {
            initialized = true; initializing = false;
            LoadBanner(); LoadRewarded(); LoadInterstitial();
        }));
    }
    public void ShowPrivacyOptions()
    {
        if (!PrivacyOptionsRequired || IsShowingAd) return;
        BeginFullScreen();
        ConsentForm.ShowPrivacyOptionsForm(error => Dispatch(() =>
        {
            EndFullScreen();
            if (error != null) FusionUI.Instance?.Notify("ERROR_PRIVACY");
            // Discard inventory collected under the previous consent state.
            banner?.Destroy(); banner = null;
            interstitial?.Destroy(); interstitial = null;
            rewarded?.Destroy(); rewarded = null;
            if (ConsentInformation.CanRequestAds()) { InitializeAds(); if (initialized) { LoadBanner(); LoadRewarded(); LoadInterstitial(); } }
            StateChanged?.Invoke();
        }));
    }
    private void LoadBanner()
    {
        if (!initialized || !ConsentInformation.CanRequestAds() || banner != null) return;
        banner = new BannerView(BannerId, AdSize.Banner, AdPosition.Bottom);
        banner.OnBannerAdLoaded += () => Dispatch(() => { if (bannerWanted) banner?.Show(); else banner?.Hide(); });
        banner.OnBannerAdLoadFailed += error => Dispatch(() => { banner?.Destroy(); banner = null; Retry(nameof(LoadBanner)); });
        banner.Hide();
        banner.LoadAd(new AdRequest());
    }
    public void ShowBanner() { bannerWanted = true; if (initialized && banner == null) LoadBanner(); banner?.Show(); }
    public void HideBanner() { bannerWanted = false; banner?.Hide(); }
    private void Retry(string method) { if (!IsInvoking(method)) Invoke(method, 30); }
    private void LoadInterstitial()
    {
        if (!initialized || loadingInterstitial || interstitial != null || !ConsentInformation.CanRequestAds()) return;
        loadingInterstitial = true;
        InterstitialAd.Load(InterstitialId, new AdRequest(), (ad, error) => Dispatch(() =>
        {
            loadingInterstitial = false;
            if (error != null || ad == null) { ad?.Destroy(); Retry(nameof(LoadInterstitial)); return; }
            interstitial = ad;
        }));
    }
    public void ShowInterstitial()
    {
        if (IsShowingAd || SceneTransition.IsLoading || GameManager.Instance == null || !GameManager.Instance.IsGameOver) return;
        if (interstitial == null || !interstitial.CanShowAd()) { interstitial?.Destroy(); interstitial = null; LoadInterstitial(); return; }
        var ad = interstitial; interstitial = null;
        bool finished = false;
        Action finish = () => { if (finished) return; finished = true; EndFullScreen(); ad.Destroy(); LoadInterstitial(); };
        ad.OnAdFullScreenContentClosed += () => Dispatch(finish);
        ad.OnAdFullScreenContentFailed += _ => Dispatch(finish);
        BeginFullScreen();
        ad.Show();
    }
    private void LoadRewarded()
    {
        if (!initialized || loadingRewarded || rewarded != null || !ConsentInformation.CanRequestAds()) return;
        loadingRewarded = true;
        RewardedAd.Load(RewardedId, new AdRequest(), (ad, error) => Dispatch(() =>
        {
            loadingRewarded = false;
            if (error != null || ad == null) { ad?.Destroy(); Retry(nameof(LoadRewarded)); return; }
            rewarded = ad;
        }));
    }
    public void ShowRewardedUndo() => ShowRewarded(false);
    public void ShowRewardedResultPanelUndo() => ShowRewarded(true);
    private void ShowRewarded(bool fromResult)
    {
        if (IsShowingAd || SceneTransition.IsLoading) return;
        var board = BoardManager.Instance;
        if (board == null || (fromResult ? !board.CanUndoFromResultPanel() : !board.CanUndo())) return;
        if (rewarded == null || !rewarded.CanShowAd())
        {
            rewarded?.Destroy(); rewarded = null;
            UIManager.Instance?.ShowAdUnavailableMessage();
            LoadRewarded();
            return;
        }
        var ad = rewarded; rewarded = null;
        int revision = board.Revision;
        var transaction = new RewardTransaction();
        bool finished = false;
        Action apply = () =>
        {
            if (board != null && BoardManager.Instance == board && board.Revision == revision && transaction.TryConsume())
                board.UndoLastTwoMoves();
        };
        Action<bool> finish = failed =>
        {
            if (finished) return; finished = true;
            transaction.Close(failed);
            EndFullScreen();
            apply();
            ad.Destroy();
            LoadRewarded();
            if (failed) UIManager.Instance?.ShowAdUnavailableMessage();
        };
        ad.OnAdFullScreenContentClosed += () => Dispatch(() => finish(false));
        ad.OnAdFullScreenContentFailed += _ => Dispatch(() => finish(true));
        BeginFullScreen();
        ad.Show(_ => Dispatch(() => { transaction.Earn(); apply(); }));
    }
    private void BeginFullScreen()
    {
        savedTimeScale = Time.timeScale;
        IsShowingAd = true;
        Time.timeScale = 0;
        AudioListener.pause = true;
        StateChanged?.Invoke();
    }
    private void EndFullScreen()
    {
        IsShowingAd = false;
        Time.timeScale = GameManager.Instance != null && GameManager.Instance.IsPaused ? 0 : savedTimeScale;
        AudioListener.pause = false;
        StateChanged?.Invoke();
        FusionUI.Instance?.RefreshGameplay();
    }
    private void OnApplicationPause(bool paused)
    {
        if (!paused && initialized && !IsShowingAd) { LoadRewarded(); LoadInterstitial(); if (bannerWanted) LoadBanner(); }
        else if (!paused && !initialized && !IsShowingAd) Start();
    }
    private void OnDestroy()
    {
        if (Instance != this) return;
        CancelInvoke(); banner?.Destroy(); interstitial?.Destroy(); rewarded?.Destroy();
        Instance = null; AudioListener.pause = false;
    }
}
