using Code.Commandable;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Code.UI.Components
{
    public class BuildingQueueButtonUI : MonoBehaviour, IUIElement<UnlockableSO, UnityAction>
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Button button;

        private void Awake()
        {
            Disable();
        }

        public void EnableFor(UnlockableSO item, UnityAction callback)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = true;
            button.onClick.AddListener(callback);
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = item.Icon;
        }

        public void Disable()
        {
            button.interactable = false;
            button.onClick.RemoveAllListeners();
            iconImage.gameObject.SetActive(false);
        }
    }
}
