using System.Collections.Generic;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Navigation.Baking;
using Code.Navigation.Graph;
using Code.Units.Base;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Managers
{
    public class UnitSeparationManager : MonoSingleton<UnitSeparationManager>
    {
        [SerializeField] private LayerMask unitLayer;
        [SerializeField] private float pushStrength = 2f;
        [SerializeField] private float settleDuration = 0.4f;

        // 멈춰 선 뒤에도 이 배율로 계속 밀어낸다. 0이면 예전처럼 완전히 멈춰 서로 겹친 채로 굳는다
        [SerializeField, Range(0f, 1f)] private float idlePushScale = 0.35f;
        [SerializeField] private int maxNeighbors = 16;

        private readonly List<AbstractUnit> _units = new();
        private readonly List<float> _settle = new();

        private Collider2D[] _hits;
        private int _phase;     // 이번 프레임에 밀어낼 절반(걷는 유닛)
        private int _idlePhase; // 멈춘 유닛은 네 조각으로 나눠 훨씬 드물게 본다
        private ContactFilter2D _filter;
        private BusSubscriptionScope _scope;

        protected override void Awake()
        {
            base.Awake();

            _hits = new Collider2D[maxNeighbors];
            _filter.SetLayerMask(unitLayer);
            _filter.useTriggers = false; // Do not include triggers in the filter

            _scope = new BusSubscriptionScope();
            _scope.SubscribeAll<UnitSpawnEvent>(HandleSpawn);
            _scope.SubscribeAll<UnitDeathEvent>(HandleDeath);
        }

        protected override void OnDestroy()
        {
            _scope.Dispose();
            base.OnDestroy();
        }

        private void HandleSpawn(UnitSpawnEvent evt)
        {
            if (evt.Unit == null || _units.Contains(evt.Unit))
                return;

            _units.Add(evt.Unit);
            _settle.Add(settleDuration); // 겹쳐 태어난 경우를 여기서 푼다
        }

        private void Update()
        {
            float delta = Time.deltaTime;

            // 물리 질의는 한 프레임 걸러 돈다. 행군 중인 유닛 전원이 매 프레임 OverlapCircle을 부르는 것이
            // 유닛이 많을 때 프레임이 끊기는 큰 몫이었다. 밀어내는 양을 두 배로 주면 결과가 같다
            _phase ^= 1;

            // 멈춰 선 유닛은 네 프레임에 한 번만 본다. 겹침을 푸는 데는 그걸로 충분하고,
            // 판 중반에는 서 있는 유닛이 걷는 유닛보다 훨씬 많아 여기가 곧 프레임 비용이다
            _idlePhase = (_idlePhase + 1) & 3;

            for (int i = _units.Count - 1; i >= 0; i--)
            {
                AbstractUnit unit = _units[i];
                if(unit == null || unit.IsDead)
                {
                    RemoveAt(i);
                    continue;
                }

                // 걷는 중과 막 멈춘 동안은 온 힘으로, 그 뒤로는 약하게 계속 민다.
                // 완전히 멈추면 제자리에 선 분대가 서로 겹친 채로 굳어 한 덩어리로 보인다
                bool active = true;
                if (unit.Agent.IsOnPath)
                    _settle[i] = settleDuration;
                else if (_settle[i] > 0f)
                    _settle[i] -= delta;
                else
                    active = false;

                // 명부 정리와 타이머는 매 프레임 돌리고, 밀어내기만 나눠 맡는다.
                // 건너뛴 프레임만큼 밀어내는 양을 곱해 주면 결과는 같다
                if (active)
                {
                    if ((i & 1) != _phase)
                        continue;

                    Separate(unit, delta * 2f);
                    continue;
                }

                if (idlePushScale <= 0f || (i & 3) != _idlePhase)
                    continue;

                Separate(unit, delta * 4f * idlePushScale);
            }
        }

       /// <summary>
       /// 유닛을 밀어내기 반경 내의 다른 유닛들과 겹치지 않도록 밀어냅니다.
       /// </summary>
        private void Separate(AbstractUnit unit, float delta)
        {
            float radius = unit.SeparationRadius;
            if (radius <= 0f)
                return;

            Vector2 position = unit.transform.position;
            int count = Physics2D.OverlapCircle(position, radius, _filter, _hits);
            Vector2 push = Vector2.zero;

            for (int i = 0; i < count; i++)
            {
                Transform otherRoot = _hits[i].attachedRigidbody != null
                    ? _hits[i].attachedRigidbody.transform
                    : _hits[i].transform;

                if (otherRoot == unit.transform)
                    continue;

                Vector2 diff = position - (Vector2)otherRoot.position;
                float dist = diff.magnitude;

                if (dist < 0.001f)
                {
                    diff = TieBreak(unit.transform, otherRoot);
                    dist = 1f;
                }
                else if (dist > radius)
                {
                    continue;
                }

                push += diff / dist * (radius - Mathf.Min(dist, radius));
            }

            if (push == Vector2.zero)
                return;

            Vector3 candidate = (Vector2)unit.transform.position + push * (pushStrength * delta);
            if (IsWalkable(unit.transform.position, candidate))
                unit.transform.position = candidate;
        }

        /// <summary>
        /// 두 유닛이 정확히 같은 위치에 있을 때, 랜덤하게 밀어낼 방향을 결정합니다.
        /// </summary>
        private static Vector2 TieBreak(Transform a, Transform b)
        {
            int idA = a.GetHashCode();
            int idB = b.GetHashCode();
            float angle = (idA ^ idB) * 0.618f;
            float sign = idA < idB ? 1f : -1f;

            return sign * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>
        /// 밀려날 위치가 길찾기 가능한 노드이며, 현재 서 있는 층과 같은 층인지 확인합니다.
        /// 층 검사가 없으면 유닛이 절벽 아래로 밀려납니다.
        /// </summary>
        private static bool IsWalkable(Vector3 current, Vector3 candidate)
        {
            PathBaker baker = PathBaker.Instance;
            if (baker == null || baker.BakedData == null || baker.BaseTilemap == null)
                return true;

            Tilemap map = baker.BaseTilemap;
            if (!baker.BakedData.TryGetNode(map.WorldToCell(candidate), out NodeData target))
                return false;

            // 현재 셀에 노드가 없으면(건물 위 등) 비교할 기준이 없어 통과시킵니다.
            if (!baker.BakedData.TryGetNode(map.WorldToCell(current), out NodeData from))
                return true;

            return target.level == from.level;
        }
        private void HandleDeath(UnitDeathEvent evt)
        {
            int index = _units.IndexOf(evt.Unit);
            if (index >= 0)
                RemoveAt(index);
        }

        private void RemoveAt(int index)
        {
            int last = _units.Count - 1;
            _units[index] = _units[last];
            _settle[index] = _settle[last];
            _units.RemoveAt(last);
            _settle.RemoveAt(last);
        }
    }
}
