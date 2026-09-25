using System.Collections;
using UnityEngine;

public class Piece : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private float bounceHeight = 0.12f;
    [SerializeField] private float bounceDuration = 0.08f;
    [SerializeField] private AnimationCurve bounceCurve;

    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite winningSprite;

    private bool isMoving;
    private Vector3 targetPosition;
    private Vector3 restingScale;
    private void Awake() { restingScale = transform.localScale; }

    public PieceType PieceType;

    public bool IsMoving => isMoving;

    public void MoveTo(Vector3 target)
    {
        targetPosition = target;
        isMoving = true;
        StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        while ((transform.position - targetPosition).sqrMagnitude > .0001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, Mathf.Max(1, moveSpeed) * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
        yield return BounceLanding();
        isMoving = false;
    }

    public void Highlight()
    {
        if (PieceThemeManager.Instance == null) return;
        if (PieceType == PieceType.Player1)
        {
            spriteRenderer.sprite =
                PieceThemeManager.Instance.GetPlayer1WinningSprite();
        }
        else
        {
            spriteRenderer.sprite =
                PieceThemeManager.Instance.GetPlayer2WinningSprite();
        }
    }

    public void ResetHighlight()
    {
        StopAllCoroutines();
        transform.position = targetPosition;
        transform.localScale = restingScale;
        if (PieceThemeManager.Instance == null) return;
        if (PieceType == PieceType.Player1)
        {
            spriteRenderer.sprite =
                PieceThemeManager.Instance.GetPlayer1Sprite();
        }
        else
        {
            spriteRenderer.sprite =
                PieceThemeManager.Instance.GetPlayer2Sprite();
        }
    }

    public void Bounce()
    {
        StartCoroutine(BounceRoutine());
    }

    private IEnumerator BounceRoutine()
    {
        Vector3 originalScale = transform.localScale;

        Vector3 big = originalScale * 1.25f;

        float timer = 0f;

        while (timer < 0.12f)
        {
            timer += Time.deltaTime;

            transform.localScale =
                Vector3.Lerp(originalScale, big, timer / 0.12f);

            yield return null;
        }

        timer = 0f;

        while (timer < 0.12f)
        {
            timer += Time.deltaTime;

            transform.localScale =
                Vector3.Lerp(big, originalScale, timer / 0.12f);

            yield return null;
        }

        transform.localScale = originalScale;
    }

    private IEnumerator BounceLanding()
    {
        Vector3 start = transform.position;
        Vector3 up = start + Vector3.up * bounceHeight;

        float timer = 0f;

        while (timer < bounceDuration)
        {
            timer += Time.deltaTime;

            float t = timer / bounceDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(start, up, t);

            yield return null;
        }

        timer = 0f;

        while (timer < bounceDuration)
        {
            timer += Time.deltaTime;

            float t = timer / bounceDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(up, start, t);

            yield return null;
        }

        transform.position = start;

    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (sprite != null && spriteRenderer != null) spriteRenderer.sprite = sprite;
    }
}
