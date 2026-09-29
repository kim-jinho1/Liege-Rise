using Code.Military.Data;
using Code.Military.Squads;
using Code.Military.Stats;
using UnityEngine;

namespace Code.Military.Combat
{
    /// <summary>
    /// 분대의 정면·측면·후면 배율을 판정하는 클래스입니다. 분대 스탯이 만들어진 뒤에 생성해야 합니다.
    /// </summary>
    public class SquadArmor
    {
        private readonly SquadSO _squadSO;
        private readonly SquadFacing _facing;
        private readonly StatSheet _stats;

        /// <summary>
        /// 판정에 필요한 데이터와 협력 객체를 주입합니다. 분대 스탯이 만들어진 뒤에 생성해야 합니다.
        /// </summary>
        public SquadArmor(SquadSO squadSO, SquadFacing facing, StatSheet stats)
        {
            _squadSO = squadSO;
            _facing = facing;
            _stats = stats;
        }

        /// <summary>
        /// 분대의 정면·측면·후면 배율을 판정합니다. 공격자 방향이 없으면 정면으로 간주합니다.
        /// </summary>
        /// <summary>마지막으로 맞은 구간·방향·시각입니다. 적 지휘관이 "옆구리를 맞았다"를 여기서 읽습니다.</summary>
        public ArmorZone LastHitZone { get; private set; } = ArmorZone.Front;
        public Vector2 LastHitDirection { get; private set; }
        public float LastHitTime { get; private set; } = -1f;

        public float MultiplierFrom(Vector2 attackerDirection)
            => MultiplierOf(ZoneFrom(attackerDirection));

        /// <summary>실제 피격을 기록합니다. 판정만 보는 PeekZone은 여기를 지나지 않습니다.</summary>
        public void Record(ArmorZone zone, Vector2 attackerDirection)
        {
            LastHitZone = zone;
            LastHitDirection = attackerDirection;
            LastHitTime = Time.time;
        }

        /// <summary>
        /// 공격자 방향이 어느 구간에 걸리는지 판정합니다.
        /// 발밑 부채꼴도 이 판정을 그대로 그리므로 화면과 피해가 갈라지지 않습니다.
        /// </summary>
        public ArmorZone ZoneFrom(Vector2 attackerDirection)
        {
            // 방향이 없는 피해(지속 피해는 normal에 Vector3.zero를 넘긴다)는 가장 유리한 정면으로 본다.
            if (_squadSO == null || _facing == null || attackerDirection.sqrMagnitude < 0.0001f)
                return ArmorZone.Front;

            // CurrentAngle 기준이다. 슬롯 배치만 DesiredAngle을 쓴다 — 섞으면 회전 중에 배율이 튄다.
            float angle = _facing.AngleTo(attackerDirection);

            if (angle <= _squadSO.FrontConeAngle * 0.5f)
                return ArmorZone.Front;

            return angle >= 180f - _squadSO.RearConeAngle * 0.5f ? ArmorZone.Rear : ArmorZone.Side;
        }

        /// <summary>
        /// 구간에 해당하는 피해 배율입니다.
        /// </summary>
        public float MultiplierOf(ArmorZone zone)
        {
            if (_squadSO == null || _stats == null)
                return 1f;

            return zone switch
            {
                ArmorZone.Rear => Multiplier(_squadSO.RearDamageMultiplierStat),
                ArmorZone.Side => Multiplier(_squadSO.SideDamageMultiplierStat),
                _ => Multiplier(_squadSO.FrontDamageMultiplierStat)
            };
        }

        /// <summary>
        /// 스탯 시트에서 배율을 읽습니다. 등록되지 않은 스탯이면 보정 없음(1배)입니다.
        /// </summary>
        private float Multiplier(StatSO definition)
            => _stats.Get(definition)?.Value ?? 1f;
    }
}
