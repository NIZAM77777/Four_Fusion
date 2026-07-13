using UnityEngine;

public class Piece : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 12f;

    public PieceType PieceType;

    private bool isMoving;
    private Vector3 targetPosition;

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
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }
}