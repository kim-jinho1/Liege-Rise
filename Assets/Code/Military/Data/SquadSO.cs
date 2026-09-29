using Code.Commandable;
using Code.Military.Squads.Formations;
using Code.Military.Stats;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Military.Data
{
    /// <summary>
    /// 게임 내 분대의 설정을 정의하는 설정 에셋입니다.
    /// 분대원 구성, 탐지 속성, 대형 설정, 분대원의 스탯 보정값 등
    /// 분대와 관련된 설정 데이터를 포함합니다.
    /// 상속 받은 Health는 사용하지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Squad data", menuName = "Military/Squad data", order = 10)]
    public class SquadSO : AbstractCommandableSO
    {
        [field: Header("Identity")] // 선택했을 때 분대 위에 뜨는 깃발. 비워 두면 깃발이 뜨지 않는다
        [field: SerializeField] public Sprite Flag { get; private set; }

        [field: Header("Members")]
        [field: SerializeField] public CombatUnitSO MemberSO { get; private set; }
        [field: SerializeField] public int MemberCount { get; private set; }

        [field: Header("Stats")]
        [field: SerializeField] public StatOverride[] Stats { get; private set; }

        [field: Header("Stat Keys")]
        [field: SerializeField] public StatSO DamageMultiplierStat { get; private set; }
        [field: SerializeField] public StatSO AttackDelayMultiplierStat { get; private set; }
        [field: SerializeField] public StatSO MaxHealthMultiplierStat { get; private set; }
        [field: SerializeField] public StatSO MoveSpeedMultiplierStat { get; private set; } // 대형이 행군 속도를 정하는 통로

        [field: Header("Directional Armor")] // 피격 방향별 피해 배율. SquadArmor가 콘 각도와 함께 읽는다
        [field: SerializeField] public StatSO FrontDamageMultiplierStat { get; private set; }
        [field: SerializeField] public StatSO SideDamageMultiplierStat { get; private set; }
        [field: SerializeField] public StatSO RearDamageMultiplierStat { get; private set; }

        [field: Header("Tactics")] // 플레이어 분대는 직접 조종하므로 비워 둔다
        [field: SerializeField] public SquadDoctrine Doctrine { get; private set; } // 적 지휘관이 이 분대를 어떻게 쓰는지
        [field: SerializeField] public float PatrolRadius { get; private set; } // 0이면 초소에 붙박이로 선다
        [field: SerializeField] public float PatrolInterval { get; private set; } = 6f;

        [field: Header("Detection")]
        [field: SerializeField] public float DetectRange { get; private set; } = 7f; // 분대 단위 감지 범위

        [field: Header("Tower")] // 탑승형 타워에 올릴 수 있는지. 멤버 BT에 MountTower 분기가 있어야 실제로 오른다
        [field: SerializeField] public bool CanMountTower { get; private set; }

        [field: Header("Formation")]
        [field: SerializeField] public FormationSO[] Formations { get; private set; } // 분대가 사용할 수 있는 대형 설정 배열

        [field: Header("Cones")]
        [field: SerializeField] public float AttackConeAngle { get; private set; } = 180f; // 정면 ±(값/2) 안에 목표가 있어야 공격 가능
        [field: SerializeField] public float FrontConeAngle { get; private set; } = 120f;
        [field: SerializeField] public float RearConeAngle { get; private set; } = 120f;
    }
}
