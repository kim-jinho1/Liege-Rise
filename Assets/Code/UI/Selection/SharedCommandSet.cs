using System.Collections.Generic;
using Code.Commandable;
using Code.Commands.Base;

namespace Code.UI.Selection
{
    /// <summary>
    /// 여러 대상을 함께 선택했을 때 전원이 공통으로 가진 커맨드를 추립니다.
    /// </summary>
    public class SharedCommandSet
    {
        private readonly List<BaseCommandSO> _shared = new(8);

        /// <summary>
        /// 마지막으로 추린 공통 커맨드 목록입니다.
        /// </summary>
        public IReadOnlyList<BaseCommandSO> Commands => _shared;

        /// <summary>
        /// 선택된 대상 중 살아 있는 첫 번째입니다. 전부 비었으면 null입니다.
        /// </summary>
        public static AbstractCommandable First(IReadOnlyCollection<AbstractCommandable> targets)
        {
            if (targets == null)
                return null;

            foreach (AbstractCommandable target in targets)
                if (target != null)
                    return target;

            return null;
        }

        /// <summary>
        /// 전원이 공통으로 가진 커맨드만 다시 추립니다. Slot이 음수인 것은 우클릭 전용이라 제외합니다.
        /// </summary>
        public void Collect(IReadOnlyCollection<AbstractCommandable> targets, AbstractCommandable first)
        {
            _shared.Clear();

            if (first == null || first.AvailableCommands == null)
                return;

            // 커맨드 페이지가 열린 대표는 교집합을 잡지 않는다 — 페이지는 능력이 아니라 대표 하나의
            // 표시 상태라, 남들이 안 가진 페이지 항목이 전부 지워져 건설 목록이 통째로 사라졌다.
            // 패널의 실행 대상도 대표 하나뿐이라 페이지를 그대로 보여 주는 쪽이 실제 동작과 맞는다
            bool showsOverriddenPage = first.HasCommandOverrides;

            foreach (BaseCommandSO command in first.AvailableCommands)
            {
                if (command == null || command.Slot < 0)
                    continue;

                if (showsOverriddenPage || HasAll(targets, command))
                    _shared.Add(command);
            }
        }

        private static bool HasAll(IReadOnlyCollection<AbstractCommandable> targets, BaseCommandSO command)
        {
            foreach (AbstractCommandable target in targets)
            {
                if (target == null || target.AvailableCommands == null)
                    return false;

                if (!Contains(target.AvailableCommands, command))
                    return false;
            }

            return true;
        }

        private static bool Contains(IReadOnlyList<BaseCommandSO> commands, BaseCommandSO command)
        {
            foreach (BaseCommandSO candidate in commands)
                if (candidate == command)
                    return true;

            return false;
        }
    }
}
