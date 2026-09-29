using Code.Military.Stats;
using UnityEngine;

namespace Code.Military.Data
{
    /// <summary>
    /// 해당 지휘관이 보유한 패시브 능력의 정의 에셋
    /// </summary>
    [CreateAssetMenu(fileName = "Commander Passive", menuName = "Military/Commander Passive", order = 13)]
    public class CommanderPassiveSO : ScriptableObject
    {
        [field: Header("Passive")]
        [field: SerializeField] public StatSO Stat { get; private set; } // 보정할 스탯의 정의 에셋
        [field: SerializeField] public ModifierOperator Operator { get; private set; } = ModifierOperator.Multiplicative;
        [field: SerializeField] public float Value { get; private set; } = 1.15f; // 승산이면 배율(1.15 = +15%), 가산이면 더할 값
    }
}
