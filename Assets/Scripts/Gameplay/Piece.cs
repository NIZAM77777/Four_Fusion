using System.Collections;
using UnityEngine;

public class Piece : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private SpriteRenderer spriteRenderer;

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
        }
    }

    public void Highlight()
    {
        Debug.Log("Highlight Called");

        spriteRenderer.color = Color.green;
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
}