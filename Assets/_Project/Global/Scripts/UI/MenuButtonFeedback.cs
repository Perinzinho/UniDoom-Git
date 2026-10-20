using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UniDoom.UI
{
    public sealed class MenuButtonFeedback : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        public GameObject Marker;
        void OnEnable() { if (Marker != null) Marker.SetActive(false); }
        public void OnSelect(BaseEventData data) { if (Marker != null) Marker.SetActive(true); }
        public void OnDeselect(BaseEventData data) { if (Marker != null) Marker.SetActive(false); }
        public void OnPointerEnter(PointerEventData data)
        {
            var button = GetComponent<Button>();
            if (button.IsInteractable()) button.Select();
        }
    }
}
