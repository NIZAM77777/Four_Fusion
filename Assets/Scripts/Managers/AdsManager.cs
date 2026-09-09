using UnityEngine;
using GoogleMobileAds.Api;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

#if UNITY_ANDROID

    //==================================================
    // LIVE AD UNIT IDs
    //==================================================

    private string bannerAdUnitId =
        "ca-app-pub-1729898546273793/6166062447";

    private string interstitialAdUnitId =
        "ca-app-pub-1729898546273793/4852980773";

    private string rewardedAdUnitId =
        "ca-app-pub-1729898546273793/3668605627";

#else

    private string bannerAdUnitId = "unused";
    private string interstitialAdUnitId = "unused";
    private string rewardedAdUnitId = "unused";

#endif

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;


    //==================================================
    // REWARDED ACTION
    //==================================================

    private bool rewardedUndoFromResultPanel = false;


    //==================================================
    // UNITY
    //==================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("AdMob Initialized");

            LoadBanner();
            LoadRewardedAd();
            LoadInterstitialAd();
        });
    }


    //==================================================
    // BANNER ADS
    //==================================================

    private void LoadBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        bannerView = new BannerView(
            bannerAdUnitId,
            AdSize.Banner,
            AdPosition.Bottom);

        AdRequest request = new AdRequest();

        bannerView.LoadAd(request);

        Debug.Log("Banner Loaded");
    }


    public void ShowBanner()
    {
        if (bannerView != null)
        {
            bannerView.Show();
        }
    }


    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
        }
    }


    //==================================================
    // INTERSTITIAL ADS
    //==================================================

    private void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        AdRequest request = new AdRequest();

        InterstitialAd.Load(
            interstitialAdUnitId,
            request,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.Log(
                        "Interstitial failed to load: " +
                        error);

                    return;
                }

                interstitialAd = ad;

                RegisterInterstitialEvents();

                Debug.Log("Interstitial Loaded");
            });
    }


    private void RegisterInterstitialEvents()
    {
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial Ad Closed");

            LoadInterstitialAd();
        };

        interstitialAd.OnAdFullScreenContentFailed +=
        (AdError error) =>
        {
            Debug.Log(
                "Interstitial failed to show: " +
                error);

            LoadInterstitialAd();
        };
    }


    public void ShowInterstitial()
    {
        if (interstitialAd != null &&
            interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            Debug.Log(
                "Interstitial ad is not available.");
        }
    }


    //==================================================
    // REWARDED ADS
    //==================================================

    private void LoadRewardedAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        AdRequest request = new AdRequest();

        RewardedAd.Load(
            rewardedAdUnitId,
            request,
            (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.Log(
                        "Rewarded ad failed to load: " +
                        error);

                    return;
                }

                rewardedAd = ad;

                Debug.Log("Rewarded ad loaded.");

                RegisterRewardedEvents();
            });
    }


    private void RegisterRewardedEvents()
    {
        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded Ad Closed");

            LoadRewardedAd();
        };

        rewardedAd.OnAdFullScreenContentFailed +=
        (AdError error) =>
        {
            Debug.Log(
                "Rewarded ad failed: " +
                error);

            LoadRewardedAd();
        };
    }


    //==================================================
    // NORMAL GAMEPLAY REWARDED UNDO
    //==================================================

    public void ShowRewardedUndo()
    {
        rewardedUndoFromResultPanel = false;

        ShowRewardedAd();
    }


    //==================================================
    // RESULT PANEL REWARDED UNDO
    //==================================================

    public void ShowRewardedResultPanelUndo()
    {
        rewardedUndoFromResultPanel = true;

        ShowRewardedAd();
    }


    //==================================================
    // SHOW REWARDED
    //==================================================

    private void ShowRewardedAd()
    {
        if (rewardedAd != null &&
            rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log(
                    "Reward earned.");

                if (BoardManager.Instance == null)
                    return;


                // Result-panel undo
                if (rewardedUndoFromResultPanel)
                {
                    if (BoardManager.Instance
                        .CanUndoFromResultPanel())
                    {
                        BoardManager.Instance
                            .UndoLastTwoMoves();
                    }
                }
                // Normal gameplay undo
                else
                {
                    if (BoardManager.Instance.CanUndo())
                    {
                        BoardManager.Instance
                            .UndoLastTwoMoves();
                    }
                }


                rewardedUndoFromResultPanel = false;
            });
        }
        else
        {
            Debug.Log(
                "Rewarded ad is not available.");

            UIManager.Instance
                .ShowAdUnavailableMessage();

            rewardedUndoFromResultPanel = false;
        }
    }
}