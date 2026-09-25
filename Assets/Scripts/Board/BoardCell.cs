using UnityEngine;

public class BoardCell : MonoBehaviour
{
    public int Column;
    public int Row;
    private void Awake()
    {
        // Keep the authored board silhouette, soften its bright blue plate for the HUD palette.
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.color = new Color(.35f, .42f, .58f, 1);
    }
}
