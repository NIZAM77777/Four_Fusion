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

    private void Awake()
    {
        Instance = this;
    }

    public Sprite GetPlayer1Sprite()
    {
        switch (GameSettings.Theme)
        {
            case PieceTheme.Combo2:
                return combo2Player1;

            default:
                return combo1Player1;
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
}