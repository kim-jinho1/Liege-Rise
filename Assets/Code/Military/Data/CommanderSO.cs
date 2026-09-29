using Code.Units.Combat;
using UnityEngine;

namespace Code.Military.Data
{
    /// <summary>
    /// 지휘관 데이터입니다. 전투 유닛과 같은 데이터 계층이라 AttackConfig를 그대로 갖습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Commander data", menuName = "Military/Commander data", order = 12)]
    public class CommanderSO : CombatUnitSO
    {
        [field: Header("Passive")]
        [field: SerializeField] public CommanderPassiveSO Passive { get; private set; }
        [field: SerializeField] public float AuraRange { get; private set; } = 7f; // 이 안의 배속 분대만 패시브를 받는다. 물러선 거리에 닿아야 한다

        [field: Header("Command")]
        [field: SerializeField] public int SquadCapacity { get; private set; } = 6;
        // 분대 무리 뒤로 물러서는 거리. 7은 부대와 따로 노는 것으로 보였고 4도 여전히 멀었다.
        // 2면 대형 바로 뒤라 한 덩어리로 읽히면서 근접 사거리(1.2) 밖에 선다 (9/9 빌드 테스트)
        [field: SerializeField] public float LineRearOffset { get; private set; } = 2f;

        public override object Clone()
        {
            CommanderSO copy = (CommanderSO)base.Clone();
            copy.Passive = Passive == null ? null : Instantiate(Passive);
            return copy;
        }
    }
}
