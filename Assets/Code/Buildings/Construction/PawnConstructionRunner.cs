using System.Collections.Generic;
using Code.Audio;
using Code.Buildings.Data;
using Code.Players;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Buildings.Construction
{
    /// <summary>
    /// 건설 현장에 배정된 폰을 관리하고, 진행률과 체력을 누적하며, 유휴 폰을 주기적으로 배정합니다.
    /// </summary>
    public class PawnConstructionRunner : MonoBehaviour
    {
        private const float RecruitInterval = 1f;

        private ConstructionDummy _dummy;
        private BuildingSO _buildingSO;

        private readonly List<Pawn> _workers = new(); // 도착해 건설 중인 폰
        private readonly List<Pawn> _assigned = new(); // 배정된 폰
        private readonly Dictionary<Pawn, int> _slots = new(); // 폰별 외곽 작업 슬롯

        private float _progress;
        private float _healAccumulator;
        private float _recruitTimer;

        /// <summary>
        /// 건설 현장과 건물 데이터를 설정합니다. Begin 이후에만 AssignPawn을 호출할 수 있습니다.
        /// </summary>
        public void Begin(ConstructionDummy dummy)
        {
            _dummy = dummy;
            _buildingSO = dummy.BuildingSO;
        }

        /// <summary>
        /// 지정한 폰에게 이 현장으로의 건설 명령을 내리고 예약 목록에 등록합니다.
        /// </summary>
        public void AssignPawn(Pawn pawn)
        {
            RegisterAssigned(pawn);
            pawn.AssignToConstruction(_dummy);
        }

        /// <summary>
        /// 배정된 폰을 예약 목록에 등록합니다. 자동 배정 중복 선발을 방지합니다.
        /// </summary>
        public void RegisterAssigned(Pawn pawn)
        {
            if (!_assigned.Contains(pawn))
                _assigned.Add(pawn);
        }

        /// <summary>
        /// 현장에 도착한 폰을 작업 인원으로 등록합니다.
        /// </summary>
        public void WorkerArrived(Pawn pawn)
        {
            RegisterAssigned(pawn);
            if (_workers.Contains(pawn))
                return;

            _workers.Add(pawn);
        }

        /// <summary>
        /// 폰이 현장을 떠날 때 작업 인원·예약·슬롯에서 제거합니다.
        /// </summary>
        public void WorkerLeft(Pawn pawn)
        {
            _assigned.Remove(pawn);
            _slots.Remove(pawn);
            _workers.Remove(pawn);
        }

        /// <summary>
        /// 폰의 외곽 작업 슬롯 인덱스를 반환합니다.
        /// 미배정이면 폰에서 가장 가까운 빈 슬롯을 예약합니다 — 도착 순서대로 주면
        /// 오른쪽에서 온 폰이 건물을 빙 돌아 반대편으로 걸어갑니다.
        /// </summary>
        public int GetWorkSlot(Pawn pawn)
        {
            if (_slots.TryGetValue(pawn, out int slot))
                return slot;

            slot = FindNearestFreeSlot(pawn);
            _slots[pawn] = slot;
            return slot;
        }

        /// <summary>
        /// 폰 위치에서 가장 가까운 빈 슬롯을 찾습니다. 모두 차 있으면 다음 번호로 넘어갑니다.
        /// </summary>
        private int FindNearestFreeSlot(Pawn pawn)
        {
            Vector3 from = pawn.transform.position;
            int best = -1;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < ConstructionDummy.WorkSlotCount; i++)
            {
                if (_slots.ContainsValue(i))
                    continue;

                float sqr = (_dummy.GetWorkPosition(i) - from).sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                best = i;
            }

            if (best >= 0)
                return best;

            // 네 자리가 다 찼다. 겹치더라도 자리를 주어 배정이 막히지 않게 한다.
            int overflow = ConstructionDummy.WorkSlotCount;
            while (_slots.ContainsValue(overflow))
                overflow++;

            return overflow;
        }

        private void Update()
        {
            PruneAssigned();
            RelayHammerBeats();
            AccumulateProgress();
            RecruitPeriodically();
        }

        /// <summary>
        /// 작업 중인 폰의 망치질을 현장에 전달합니다. 한 번이라도 내려친 프레임이면 건물이 반응합니다.
        /// 폰 클립에는 애니메이션 이벤트가 없어 재생 위상으로 시점을 잡습니다(ConsumeLoopBeat 참고).
        /// </summary>
        private void RelayHammerBeats()
        {
            bool struck = false;

            foreach (Pawn worker in _workers)
                if (worker != null && worker.UnitAnimator != null && worker.UnitAnimator.ConsumeLoopBeat())
                    struck = true;

            if (!struck)
                return;

            _dummy.PlayBuildPulse();
            EconomyAudio.PlayBuildHit(_dummy.transform.position);
        }

        /// <summary>
        /// 사망·이탈(명령 변경)한 폰을 예약 목록에서 정리합니다.
        /// </summary>
        private void PruneAssigned()
        {
            for (int i = _assigned.Count - 1; i >= 0; i--)
            {
                Pawn pawn = _assigned[i];
                if (pawn == null || !pawn.IsBuilding)
                    WorkerLeft(pawn);
            }
        }

        /// <summary>
        /// 작업 폰 수에 비례해 진행률과 체력을 누적합니다. 폰이 없으면 일시정지 상태를 유지합니다.
        /// </summary>
        private void AccumulateProgress()
        {
            if (_workers.Count == 0)
            {
                _dummy.SetProgress(BuildingState.Paused, _progress, 0f);
                return;
            }

            float buildTime = Mathf.Max(0.01f, _buildingSO.BuildTime);
            float delta = _workers.Count * Time.deltaTime / buildTime;
            _progress = Mathf.Min(1f, _progress + delta);

            _dummy.SetProgress(BuildingState.Constructing, _progress, _workers.Count);
            AccumulateHealth(delta);

            if (_progress >= 1f)
                _dummy.CompleteAndSpawn();
        }

        /// <summary>
        /// 진행률 증가분에 비례해 체력을 누적 회복합니다.
        /// </summary>
        private void AccumulateHealth(float progressDelta)
        {
            _healAccumulator += progressDelta * _buildingSO.Health;
            if (_healAccumulator < 1f)
                return;

            int healAmount = Mathf.FloorToInt(_healAccumulator);
            _dummy.Heal(healAmount);
            _healAccumulator -= healAmount;
        }

        private void RecruitPeriodically()
        {
            _recruitTimer += Time.deltaTime;
            if (_recruitTimer < RecruitInterval)
                return;

            _recruitTimer = 0f;
            TryRecruitPawns();
        }

        /// <summary>
        /// 최대 투입 수까지 가까운 순으로 유휴 폰을 배정합니다.
        /// </summary>
        private void TryRecruitPawns()
        {
            int needed = _buildingSO.MaxBuilders - _assigned.Count;
            for (int i = 0; i < needed; i++)
            {
                Pawn pawn = FindNearestIdlePawn();
                if (pawn == null)
                    return;

                AssignPawn(pawn);
            }
        }

        private Pawn FindNearestIdlePawn()
        {
            UnitRegistry registry = UnitRegistry.Instance;
            if (registry == null)
                return null;

            Pawn best = null;
            float bestSqrDist = float.MaxValue;
            foreach (Pawn pawn in registry.Pawns)
            {
                if (pawn == null || pawn.Owner != _dummy.Owner
                                 || !pawn.IsAvailable || _assigned.Contains(pawn))
                    continue;

                float sqrDist = (pawn.transform.position - transform.position).sqrMagnitude;
                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    best = pawn;
                }
            }

            return best;
        }
    }
}
