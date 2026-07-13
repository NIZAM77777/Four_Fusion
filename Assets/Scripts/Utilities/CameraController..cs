using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 6;
    [SerializeField] private float cellSize = 1f;

    [SerializeField] private float padding = 1f;

    private void Start()
    {
        FitCamera();
    }

    private void FitCamera()
    {
        Camera cam = Camera.main;

        float boardWidth = columns * cellSize;
        float boardHeight = rows * cellSize;

        float screenRatio = (float)Screen.width / Screen.height;
        float targetRatio = boardWidth / boardHeight;

        if (screenRatio >= targetRatio)
        {
            cam.orthographicSize = boardHeight / 2f + padding;
        }
        else
        {
            float difference = targetRatio / screenRatio;
            cam.orthographicSize = boardHeight / 2f * difference + padding;
        }
    }
}