using System.Collections;
using UnityEngine;

public class Piece : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private float bounceHeight = 0.12f;
    [SerializeField] private float bounceDuration = 0.08f;
    [SerializeField] private AnimationCurve bounceCurve;



    private bool isMoving;
    private Vector3 targetPosition;

    public PieceType PieceType;

    public bool IsMoving => isMoving;

    public void MoveTo(Vector3 target)
    {
        targetPosition = target;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;

            isMoving = false;

            StartCoroutine(BounceLanding());
        }
    }

    public void Highlight()
    {
        spriteRenderer.color = Color.gold;
    }

    public void ResetHighlight()
    {
        spriteRenderer.color = Color.white;
    }

    public void ResetColor()
    {
        spriteRenderer.color = Color.white;
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
        spriteRenderer.sprite = sprite;
    }
}