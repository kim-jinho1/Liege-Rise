using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Buildings.Logic.CoreBuilding;
using Code.Commandable;
using Code.Core.Bus;
using Code.GameEvents.Enemy;
using Code.Military.Squads;
using UnityEngine;

namespace Code.EnemyAI
{
    /// <summary>
    /// 적 진지입니다. 두 가지 일만 합니다 — 자기 건물을 지킬 수비 병력을 상한까지 곁에 두는 것과,
    /// 시간표가 부를 때 공세의 출발지가 되어 주는 것입니다.
    /// 출격 판단은 하지 않습니다. 언제 얼마나 나갈지는 EnemyAIManager의 시간표가 정합니다.
    /// </summary>
    public class EnemyGarrison : MonoBehaviour
    {
        /// <summary>
        /// 병력을 거둬들이는 반경입니다. 겹치면 가장 가까운 진지가 가져갑니다.
        /// </summary>
        [SerializeField, Min(1f)] private float musterRadius = 25f;

        private readonly List<AbstractCommandable> _roster = new(16);

        private AbstractCommandable _self;
        private AbstractBuilding _building;
        private bool _isHeadquarters;

        /// <summary>
        /// 적 소속 건물에 붙어 실제로 동작하는 진지인지 여부입니다.
        /// </summary>
        public bool IsActiveGarrison
            => _self != null && _self.Owner == Owner.Enemy;

        /// <summary>
        /// 본거지에 붙은 진지입니다. 전초 진지가 전부 무너졌을 때만 공세 출발지로 쓰입니다 —
        /// 진지를 밀어 놓으면 공세가 맵 반대편에서 출발해 대응 시간이 늘어납니다.
        /// </summary>
        public bool IsLastResortOrigin => _isHeadquarters;

        /// <summary>
        /// 이 진지에서 병력이 나오는 자리입니다.
        /// </summary>
        public Vector3 SpawnPosition
            => _building != null && _building.SpawnPoint != null
                ? _building.SpawnPoint.position
                : transform.position;

        /// <summary>
        /// 수비 병력을 더 받을 수 있는지입니다. 스포너가 생산 직전에 물어봅니다.
        /// </summary>
        public bool HasRoom
        {
            get
            {
                PruneRoster();
                return RosterStrength() < DefenseCapacity;
            }
        }

        private static int DefenseCapacity
            => EnemyAIManager.Instance != null ? EnemyAIManager.Instance.DefenseCapacity : 7;

        private void Awake()
        {
            _self = GetComponent<AbstractCommandable>();
            _building = GetComponent<AbstractBuilding>();
            _isHeadquarters = GetComponent<HeadquartersMarker>() != null;
        }

        private void OnDestroy()
        {
            if (gameObject.scene.isLoaded)
                DispatchAll(); // 진지가 무너지면 수비 병력이 즉시 반격에 나섭니다.
        }

        /// <summary>
        /// 수비 병력 하나를 이 진지 명부에 등록합니다. EnemyAIManager가 배정합니다.
        /// </summary>
        public void Register(AbstractCommandable unit)
        {
            if (unit == null || _roster.Contains(unit))
                return;

            _roster.Add(unit);
        }

        /// <summary>
        /// 지정 좌표까지의 제곱 거리입니다. 거둬들이는 반경 밖이면 float.MaxValue를 반환합니다.
        /// </summary>
        public float SqrDistanceTo(Vector3 point)
        {
            float sqr = ((Vector2)point - (Vector2)transform.position).sqrMagnitude;
            return sqr <= musterRadius * musterRadius ? sqr : float.MaxValue;
        }

        /// <summary>
        /// 반경을 무시한 제곱 거리입니다. 어느 진지의 반경에도 들지 않는 병력을 거둘 때 씁니다.
        /// </summary>
        public float SqrDistanceToUnbounded(Vector3 point)
            => ((Vector2)point - (Vector2)transform.position).sqrMagnitude;

        /// <summary>
        /// 명부의 실제 병력 수입니다. 분대는 생존 인원만큼, 단독 유닛은 1로 셉니다.
        /// </summary>
        private int RosterStrength()
        {
            int strength = 0;

            foreach (AbstractCommandable unit in _roster)
                strength += unit is ISquad squad ? squad.AliveCount : 1;

            return strength;
        }

        /// <summary>
        /// 수비 병력 전원을 플레이어 본부로 내보내고 명부를 비웁니다.
        /// </summary>
        private void DispatchAll()
        {
            PruneRoster();
            if (_roster.Count == 0)
                return;

            int strength = RosterStrength();
            AssaultForce.Create(_roster);
            _roster.Clear();

            Bus<EnemyDispatchEvent>.Raise(Owner.Enemy, new EnemyDispatchEvent(transform.position, strength));
        }

        /// <summary>
        /// 파괴되었거나 사망한 항목을 명부에서 제거합니다.
        /// </summary>
        private void PruneRoster()
        {
            for (int i = _roster.Count - 1; i >= 0; i--)
                if (_roster[i] == null || _roster[i].IsDead)
                    _roster.RemoveAt(i);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 선택 시 거둬들이는 반경을 그려 다른 진지와의 겹침을 눈으로 확인합니다.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.3f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, musterRadius);
        }
#endif
    }
}
