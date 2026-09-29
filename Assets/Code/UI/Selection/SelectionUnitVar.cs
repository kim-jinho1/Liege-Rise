using Code.Units.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Selection
{
    public class SelectionUnitVar : MonoBehaviour, IUIElement<AbstractUnit, int>
    {
        [Header("UI Elements")]
        [SerializeField] private Image unitIconImage;
        [SerializeField] private TextMeshProUGUI unitCountText;

        private void Awake()
        {
            Debug.Assert(unitIconImage != null, "Unit Icon Image reference is missing in SelectionUnitVar.");
            Debug.Assert(unitCountText != null, "Unit Count Text reference is missing in SelectionUnitVar.");
        }

        public void EnableFor(AbstractUnit unit, int unitCount)
        {
            unitIconImage.sprite = unit.UnitSO.Icon;
            unitCountText.SetText(unitCount.ToString());
        }

        public void Disable()
        {
            unitIconImage.sprite = null;
            unitCountText.SetText(string.Empty);
        }
    }
}
