using Code.Units.Combat;
using UnityEngine;

namespace Code.Commandable
{
    public class AbstractCommandableSO : UnlockableSO
    {
        [field: SerializeField] public int Health { get; private set; } = 100;
        [field: SerializeField] public GameObject Prefab { get; private set; }

        /// <summary>
        /// 유닛의 시야 반경 설정입니다. null이면 시야 비주얼을 표시하지 않습니다.
        /// </summary>
        [field: SerializeField] public SightConfigSO SightConfig { get; private set; }
    }
}
