using Ami.BroAudio;
using Code.Core;
using Code.Core.Bus;
using Code.Economics;
using Code.GameEvents.Economy;
using UnityEngine;

namespace Code.Audio
{
    /// <summary>
    /// 일하는 소리를 냅니다 — 도끼질·곡괭이질·망치질과 자원 소비입니다.
    /// 일꾼이 여럿이면 같은 소리가 겹치므로 간격이 이 클래스의 절반입니다.
    /// </summary>
    public class EconomyAudio : MonoSingleton<EconomyAudio>
    {
        [Header("채집")]
        // 나무와 돌은 소리가 달라야 한다. 종류별 소리는 ResourceSO가 들고 있고
        // 여기 있는 것은 지정되지 않은 자원이 쓰는 기본값이다
        [SerializeField] private SoundID gatherFallback;
        [SerializeField, Min(0f)] private float gatherInterval = 0.12f;

        [Header("건설")]
        [SerializeField] private SoundID buildHammer;
        [SerializeField, Min(0f)] private float buildInterval = 0.14f;

        [Header("목장")]
        [SerializeField] private SoundID sheepHarvest;  // 울타리에서 양 한 마리가 빠질 때
        [SerializeField, Min(0f)] private float sheepInterval = 0.2f;

        [Header("자원")]
        [SerializeField] private SoundID supplySpend;   // 건설·생산으로 소비
        [SerializeField, Min(0f)] private float supplyInterval = 0.1f;

        private readonly SoundThrottle _throttle = new();

        private BusSubscriptionScope _scope;

        private void OnEnable()
        {
            _scope = new BusSubscriptionScope();
            _scope.Subscribe<ResourceChangedEvent>(Owner.Player, OnResourceChanged);
        }

        private void OnDisable()
            => _scope?.Dispose();

        /// <summary>
        /// 채집 한 번의 타격음입니다. 자원이 자기 소리를 들고 있으면 그것이 이깁니다.
        /// 간격은 sourceKey별로 따로 잽니다 — 일꾼 여럿이 함께 캘 때
        /// 소리가 한 명분으로 묶이면 도끼질이 드문드문 들려 오히려 어색합니다.
        /// </summary>
        public static void PlayGather(ResourceSO resource, Vector3 position, int sourceKey)
        {
            if (Instance == null)
                return;

            SoundID id = resource != null && resource.GatherSound.IsValid()
                ? resource.GatherSound
                : Instance.gatherFallback;

            Instance._throttle.Play(id, position, Instance.gatherInterval, sourceKey);
        }

        /// <summary>
        /// 망치가 실제로 닿는 순간에 부릅니다. 폰 클립에는 애니메이션 이벤트가 없어
        /// 재생 위상으로 시점을 잡는 쪽(PawnConstructionRunner)이 불러 줍니다.
        /// </summary>
        public static void PlayBuildHit(Vector3 position)
        {
            if (Instance != null)
                Instance._throttle.Play(Instance.buildHammer, position, Instance.buildInterval);
        }

        /// <summary>
        /// 양 한 마리가 식량으로 바뀌는 순간에 부릅니다. 목장이 한 번에 여러 마리를 치울 수 있어 간격을 둡니다.
        /// </summary>
        public static void PlaySheepHarvest(Vector3 position)
        {
            if (Instance != null)
                Instance._throttle.Play(Instance.sheepHarvest, position, Instance.sheepInterval);
        }

        // 환불은 소비가 아니다. 돈이 나갈 때만 낸다
        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            if (!evt.IsRefund)
                _throttle.Play(supplySpend, supplyInterval);
        }
    }
}
