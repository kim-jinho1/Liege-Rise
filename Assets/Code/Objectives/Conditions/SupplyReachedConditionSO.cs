using Code.Core.Bus;
using Code.Economics;
using Code.Players;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 이 스텝에 들어온 뒤 지정 수량만큼 더 모으면 완료되는 조건입니다.
    /// 총 보유량으로 판정하면 시작 자원이나 앞 스텝의 지급만으로 문턱을 넘겨
    /// 스텝이 들어오자마자 통과합니다. 값을 조정할 때마다 그 사고가 되풀이되므로
    /// 판정도 표시와 같은 상대값으로 둡니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_SupplyReached", menuName = "Objectives/Conditions/SupplyReached")]
    public class SupplyReachedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField] public ResourceSO Resource { get; private set; }

        // 이 스텝에서 더 모아야 할 양. 총 보유량이 아니다
        [field: SerializeField] public int TargetValue { get; private set; }

        public override ConditionTracker CreateTracker()
            => new SupplyReachedTracker(Resource, TargetValue);
    }

    public class SupplyReachedTracker : ConditionTracker
    {
        // 진행도는 이번 스텝에서 실제로 해야 할 몫만 센다.
        // 50을 들고 시작해 60을 더 모으는 목표에 "50 / 110"을 띄우면 할 일이 얼마인지 안 보인다
        public override int Current => Mathf.Clamp(_best, 0, Target);

        public override int Target => Mathf.Max(0, _needed);

        private readonly ResourceSO _resource;
        private readonly int _needed;
        private int _baseline;

        // 이번 스텝에서 늘린 최대치. 도중에 건물을 지어 자원을 쓰더라도 진행도가 되감기면 안 된다
        private int _best;

        // 보유량은 UserSupplies가 들고 있으므로 세어 두지 않고 그때그때 읽는다
        private int Amount
            => UserSupplies.Instance != null && _resource != null
                ? UserSupplies.Instance.GetCurrentAmount(Owner.Player, _resource)
                : 0;

        public SupplyReachedTracker(ResourceSO resource, int needed)
        {
            _resource = resource;
            _needed = needed;
        }

        public override void Subscribe()
        {
            _baseline = Amount;
            _best = 0;

            // 더 모을 것이 없는 목표는 들어오자마자 끝난다 — 값이 0이거나 음수인 경우다
            if (_needed <= 0)
            {
                Complete();
                return;
            }

            if (UserSupplies.Instance != null)
                UserSupplies.Instance.OnSupplyChanged += OnSupplyChanged;
        }

        public override void Unsubscribe()
        {
            if (UserSupplies.Instance != null)
                UserSupplies.Instance.OnSupplyChanged -= OnSupplyChanged;
        }

        private void OnSupplyChanged(Owner owner, int amount, ResourceSO resource)
        {
            if (resource != _resource || owner != Owner.Player)
                return;

            _best = Mathf.Max(_best, amount - _baseline);

            Report();
            if (_best >= _needed)
                Complete();
        }
    }
}
