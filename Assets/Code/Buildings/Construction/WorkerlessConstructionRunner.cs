using System;
using Code.Buildings.Data;
using UnityEngine;

namespace Code.Buildings.Construction
{
    /// <summary>
    /// 일꾼 없이 ConstructionDummy의 건설을 자가 타이머로 진행하고 완료 시 건물을 스폰합니다.
    /// Dummy가 진행 중 파괴되면(취소/철거) destroyCancellationToken으로 안전하게 중단되어 스폰하지 않습니다.
    /// </summary>
    public class WorkerlessConstructionRunner : MonoBehaviour
    {
        private ConstructionDummy _dummy;
        private BuildingSO _buildingSO;
        private float _startTime;
        private float _healAccumulator;

        /// <summary>
        /// 자가 건설을 시작합니다.
        /// </summary>
        public void Begin(ConstructionDummy dummy)
        {
            _dummy = dummy;
            _buildingSO = dummy.BuildingSO;
            _startTime = Time.time;
            Run();
        }

        private async void Run()
        {
            try
            {
                float buildTime = Mathf.Max(0.01f, _buildingSO.BuildTime);
                while (true)
                {
                    float t = (Time.time - _startTime) / buildTime;
                    _dummy.SetProgress(BuildingState.Constructing, Mathf.Clamp01(t), 1f);
                    AccumulateHealth(buildTime);

                    if (t >= 1f)
                        break;

                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }

                _dummy.CompleteAndSpawn();
            }
            catch (OperationCanceledException)
            {
                // Dummy가 진행 중 파괴됨 — 스폰하지 않음
            }
        }

        /// <summary>
        /// 건설 시간에 비례해 체력을 누적 회복합니다.
        /// </summary>
        private void AccumulateHealth(float buildTime)
        {
            _healAccumulator += Time.deltaTime * (_buildingSO.Health / buildTime);
            if (_healAccumulator < 1f)
                return;

            int healAmount = Mathf.FloorToInt(_healAccumulator);
            _dummy.Heal(healAmount);
            _healAccumulator -= healAmount;
        }
    }
}
