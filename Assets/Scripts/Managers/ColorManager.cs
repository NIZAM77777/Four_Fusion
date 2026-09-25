using UnityEngine;

public class PieceThemeManager : MonoBehaviour
{
    public static PieceThemeManager Instance;

    [Header("Combo 1")]
    [SerializeField] private Sprite combo1Player1;
    [SerializeField] private Sprite combo1Player2;

    [Header("Combo 2")]
    [SerializeField] private Sprite combo2Player1;
    [SerializeField] private Sprite combo2Player2;

    [Header("Winning Sprite")]
    [SerializeField] private Sprite winningSprite;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public Sprite GetPlayer1Sprite()
    {
        return GetThemeSprite(GameSettings.Theme, false);
    }

    public Sprite GetThemeSprite(PieceTheme theme, bool second)
    {
        switch (theme)
        {
            case PieceTheme.Combo2:
                return second ? combo2Player2 : combo2Player1;

            default:
                return second ? combo1Player2 : combo1Player1;
        }
    }

    public Sprite GetPlayer2Sprite()
    {
        switch (GameSettings.Theme)
        {
            case PieceTheme.Combo2:
                return combo2Player2;

            default:
                return combo1Player2;
        }
    }

    public Sprite GetPlayer1WinningSprite()
    {
        return winningSprite;
    }

    public Sprite GetPlayer2WinningSprite()
    {
        return winningSprite;
    }
}
