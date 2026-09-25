using UnityEngine;

public static class PlayerSettingsStore
{
    public const string PrivacyUrl = "https://nizam77777.github.io/FourFusion/privacy-policy.html";
    public static bool MusicEnabled => PlayerPrefs.GetInt("MusicEnabled", 1) != 0;
    public static bool SoundEnabled => PlayerPrefs.GetInt("SoundEnabled", 1) != 0;
    public static void SetMusic(bool value) { PlayerPrefs.SetInt("MusicEnabled", value ? 1 : 0); AudioManager.Instance?.RefreshSettings(); PlayerPrefs.Save(); }
    public static void SetSound(bool value) { PlayerPrefs.SetInt("SoundEnabled", value ? 1 : 0); AudioManager.Instance?.RefreshSettings(); PlayerPrefs.Save(); }
    public static void SetTheme(PieceTheme theme) { GameSettings.Theme = theme; PlayerPrefs.SetInt("Theme", (int)theme); PlayerPrefs.Save(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameSettings.Theme = (PieceTheme)Mathf.Clamp(PlayerPrefs.GetInt("Theme", 0), 0, 1);
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Localization.Initialize();
    }
}
