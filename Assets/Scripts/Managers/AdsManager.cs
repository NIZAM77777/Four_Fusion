using UnityEngine;
using GoogleMobileAds.Api;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

#if UNITY_ANDROID
    private string bannerAdUnitId =
        "ca-app-pub-3940256099942544/6300978111";

    private string interstitialAdUnitId =
        "ca-app-pub-3940256099942544/1033173712";

    private string rewardedAdUnitId =
        "ca-app-pub-3940256099942544/5224354917";
#else
    private string bannerAdUnitId = "unused";
    private string interstitialAdUnitId = "unused";
    private string rewardedAdUnitId = "unused";
#endif

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

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
    // Banner Ads
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
    // Interstitial Ads
    //==================================================

    private void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        var request = new AdRequest();

        InterstitialAd.Load(
            interstitialAdUnitId,
            request,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.Log("Interstitial failed to load.");
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
            LoadInterstitialAd();
        };

        interstitialAd.OnAdFullScreenContentFailed +=
        (AdError error) =>
        {
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
    }

    //==================================================
    // Rewarded Ads
    //==================================================

    private void LoadRewardedAd()
    {
        // Destroy the old ad if it exists
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        var request = new AdRequest();

        RewardedAd.Load(
            rewardedAdUnitId,
            request,
            (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.Log("Rewarded ad failed to load.");
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

        rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.Log(error);

            LoadRewardedAd();
        };
    }

    public void ShowRewardedUndo()
    {
        if (rewardedAd != null &&
            rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward Earned");

                BoardManager.Instance.UndoLastTwoMoves();
            });
        }
        else
        {
            Debug.Log("Rewarded ad not ready.");

            // Optional fallback
            // BoardManager.Instance.UndoLastTwoMoves();
        }
    }
}