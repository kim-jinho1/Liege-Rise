using Code.Commands.Base;
using Code.Core.Bus;
using Code.GameEvents.Economy;
using Code.Military.Commanders;
using Code.Military.Data;
using Code.Military.Squads;
using Code.Players;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Code.Military.Commands
{
    /// <summary>
    /// 선택한 분대에 지휘관을 임명하는 커맨드입니다. 비용을 치르고 즉시 완료됩니다
    /// — 임명 타이머는 2026-07-30에 버린 기능이라 진행 바가 없습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Appoint Commander", menuName = "Military/Commands/Appoint Commander", order = 43)]
    public class AppointCommanderCommandSO : BaseCommandSO
    {
        /// <summary>
        /// 임명할 지휘관 데이터입니다. 비용과 프리팹이 여기서 나옵니다.
        /// </summary>
        [field: SerializeField] public CommanderSO CommanderData { get; private set; }

        private void OnEnable()
            => RequireClickToActivate = false; // 대상 클릭 없이 버튼만으로 발동한다

        /// <summary>
        /// 지휘관이 없는 아군 분대를 살 여유가 있을 때만 받습니다.
        /// 우클릭 컨텍스트는 명시적으로 거부합니다 — CommandExecutor는 RequireClickToActivate를
        /// 보지 않고 첫 CanHandle true에서 멈추므로, 여기서 받으면 분대의 이동 우클릭을 납치합니다.
        /// </summary>
        public override bool CanHandle(CommandContext context)
            => context.MouseButton != MouseButton.Right &&
               CommanderData != null &&
               CommanderData.Prefab != null &&
               context.Target is Squad { HasCommander: false } squad &&
               squad.Owner == context.Owner &&
               UserSupplies.Instance != null &&
               UserSupplies.Instance.HasEnoughSupplies(context.Owner, CommanderData.Cost);

        /// <summary>
        /// 지휘관을 분대 후방에 생성해 즉시 임명하고 비용을 차감합니다.
        /// </summary>
        public override void Handle(CommandContext context)
        {
            if (context.Target is not Squad squad || !CanHandle(context))
                return;

            Commander commander = Spawn(squad);
            if (commander == null)
                return;

            // 임명한 분대가 첫 배속 분대다. 나머지는 분대를 골라 지휘관을 우클릭해 붙인다
            if (!commander.Roster.Assign(squad))
            {
                Destroy(commander.gameObject);
                return;
            }

            Bus<ResourceChangedEvent>.Raise(context.Owner, new ResourceChangedEvent(context.Owner, CommanderData.Cost));
        }

        /// <summary>
        /// 임명할 수 없는 상태이면 버튼을 잠급니다.
        /// </summary>
        public override bool IsLocked(CommandContext context)
            => !CanHandle(context);

        /// <summary>
        /// 지휘관을 분대 후방에 생성합니다. 소유자와 데이터는 Start 전에 채웁니다.
        /// </summary>
        private Commander Spawn(Squad squad)
        {
            GameObject go = Instantiate(CommanderData.Prefab, squad.RearPoint(1.2f), Quaternion.identity);
            if (!go.TryGetComponent(out Commander commander))
            {
                Destroy(go);
                Debug.LogError($"{CommanderData.Name}: 프리팹에 Commander 컴포넌트가 없습니다.", CommanderData);
                return null;
            }

            commander.Owner = squad.Owner;
            commander.AbstractCommandableSO = CommanderData; // Start 전이라 Health·AttackConfig가 이 값으로 잡힌다
            return commander;
        }
    }
}
