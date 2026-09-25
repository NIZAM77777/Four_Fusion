using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null && button.IsInteractable() && eventData.button == PointerEventData.InputButton.Left)
            AudioManager.Instance?.PlayButtonClick();
    }
}
