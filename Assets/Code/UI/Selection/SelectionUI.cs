using System.Collections.Generic;
using Code.Commandable;
using Code.Units.Base;
using UnityEngine;

namespace Code.UI.Selection
{
    /// <summary>
    /// 다중 선택 시 유닛 종류별 아이콘과 수량을 표시합니다.
    /// 단일 선택의 아이콘·이름·체력은 공용 정보 패널(GeneralInfoUI)이 전담하므로 여기서 다루지 않으며,
    /// 패널 컨테이너의 활성화는 RuntimeUI가 책임집니다.
    /// </summary>
    public class SelectionUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject multipleUnitPanel;
        [SerializeField] private List<SelectionUnitVar> selectionUnits;

        /// <summary>
        /// 선택된 유닛들을 종류별로 묶어 아이콘과 수량으로 표시합니다.
        /// 슬롯보다 종류가 많으면 앞에서부터 채우고 나머지는 표시하지 않습니다.
        /// </summary>
        public void EnableForMultiple(IReadOnlyCollection<AbstractUnit> units)
        {
            multipleUnitPanel.SetActive(true);

            Dictionary<AbstractCommandableSO, (AbstractUnit sample, int count)> grouped = new();
            foreach (AbstractUnit unit in units)
            {
                AbstractCommandableSO so = unit.AbstractCommandableSO;
                if (grouped.TryGetValue(so, out var entry))
                    grouped[so] = (entry.sample, entry.count + 1);
                else
                    grouped[so] = (unit, 1);
            }

            int index = 0;
            foreach (var kvp in grouped)
            {
                if (index >= selectionUnits.Count)
                    break;

                selectionUnits[index].gameObject.SetActive(true);
                selectionUnits[index].EnableFor(kvp.Value.sample, kvp.Value.count);
                index++;
            }

            for (int i = index; i < selectionUnits.Count; i++)
            {
                selectionUnits[i].Disable();
                selectionUnits[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 다중 선택 패널만 숨깁니다. 형제 패널은 건드리지 않습니다.
        /// </summary>
        public void Disable()
        {
            if (multipleUnitPanel != null)
                multipleUnitPanel.SetActive(false);
        }
    }
}
