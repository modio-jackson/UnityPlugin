using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Automatically disables a sibling toggle on mouse exit.</summary>
    [RequireComponent(typeof(Toggle))]
    public class UntoggleOnMouseExit : MonoBehaviour, UnityEngine.EventSystems.IPointerExitHandler
    {
        private void OnDisable()
        {
            this.GetComponent<Toggle>().isOn = false;
        }

        /// <summary>IPointerExitHandler interface.</summary>
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData pointerEventData)
        {
            this.GetComponent<Toggle>().isOn = false;
        }
    }
}
