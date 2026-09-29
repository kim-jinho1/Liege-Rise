using Code.Core.Bus;
using Code.Economics;
using Code.GameEvents.Economy;
using Code.GameEvents.System;
using Code.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Sequences.Effects
{
    /// <summary>
    /// 플레이어에게 자원을 지급하는 효과입니다. SupplyEvent를 발행해 UserSupplies에 반영합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Eff_GiveResources", menuName = "Sequence/Effects/GiveResources")]
    public class GiveResourcesEffectSO : SequenceStepEffectSO
    {
        [BoxGroup("자원 지급"), LabelText("자원"), Required]
        [field: SerializeField] public ResourceSO Resource { get; private set; }

        [BoxGroup("자원 지급"), LabelText("수량"), MinValue(1)]
        [field: SerializeField] public int Amount { get; private set; } = 50;

        // 같은 프레임에 함께 지급된 것들. 에셋마다 따로 두면 서로를 못 보므로 static이다
        private static int _pendingFrame = -1;
        private static string _pendingItems;

        public override void Execute()
        {
            if (Resource == null || Amount <= 0)
                return;

            Bus<SupplyEvent>.Raise(Owner.Player, new SupplyEvent(Owner.Player, Amount, Resource));
            Announce();
        }

        /// <summary>
        /// 배너로 한 줄 알립니다. HUD에 +150이 잠깐 뜨는 것만으로는 어디서 온 것인지 알 수 없습니다.
        /// 한 스텝이 자원을 둘 이상 주면 효과가 같은 프레임에 잇달아 돕니다. 배너는 한 줄뿐이라
        /// 뒤엣것이 앞엣것을 즉시 덮으므로, 같은 프레임 것은 모아 매번 더 긴 한 줄로 다시 냅니다.
        /// </summary>
        private void Announce()
        {
            string item = TextFormat.SupplyItem(Resource.DisplayName, Amount);
            _pendingItems = Time.frameCount == _pendingFrame && !string.IsNullOrEmpty(_pendingItems)
                ? $"{_pendingItems} · {item}"
                : item;
            _pendingFrame = Time.frameCount;

            Bus<NoticeEvent>.Raise(Owner.Player, new NoticeEvent(TextFormat.SuppliesGranted(_pendingItems)));
        }
    }
}
