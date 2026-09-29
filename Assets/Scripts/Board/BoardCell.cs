using UnityEngine;

public class BoardCell : MonoBehaviour
{
    public int Column;
    public int Row;
    private void Awake()
    {
        // Presentation only: a continuous blue plate and transparent coin socket.
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) { renderer.sprite = FusionArt.CellSprite; renderer.color = Color.white; }
    }
}
