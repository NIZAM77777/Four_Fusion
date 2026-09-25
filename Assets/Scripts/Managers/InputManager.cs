using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (FusionUI.Instance != null || GameManager.Instance == null || GameManager.Instance.InputBlocked || boardManager == null)
            return;

        // Block only the HUMAN during the AI turn.
        if (GameManager.Instance.CurrentGameMode == GameMode.HumanVsAI &&
            boardManager.CurrentPlayer == PieceType.Player2)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            DetectColumn();
        }
    }

    private void DetectColumn()
    {
        Vector2 worldPosition =
            mainCamera.ScreenToWorldPoint(Input.mousePosition);

        RaycastHit2D hit =
            Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider == null)
            return;

        ColumnInput column =
            hit.collider.GetComponent<ColumnInput>();

        if (column == null)
            return;

        boardManager.DropPiece(column.ColumnIndex);

    }
}
