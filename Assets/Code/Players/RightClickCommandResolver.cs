using System.Collections.Generic;
using Code.Commandable;
using Code.Commands.Base;
using Code.Commands.Types.System;

namespace Code.Players
{
    /// <summary>
    /// 우클릭이 지금 실행할 커맨드 하나를 찾습니다.
    /// </summary>
    public sealed class RightClickCommandResolver
    {
        private readonly List<BaseCommandSO> _buffer = new(16);

        /// <summary>
        /// 이 상황에서 가장 먼저 걸리는 커맨드를 돌려줍니다. 없으면 null입니다.
        /// </summary>
        public BaseCommandSO Resolve(AbstractCommandable target, CommandContext context)
        {
            Fill(target, _buffer);
            foreach (BaseCommandSO command in _buffer)
                if (command.CanHandle(context))
                    return command;

            return null;
        }

        private static void Fill(AbstractCommandable target, List<BaseCommandSO> buffer)
        {
            buffer.Clear();
            if (target == null || target.AvailableCommands == null)
                return;

            foreach (BaseCommandSO command in target.AvailableCommands)
            {
                // 사라진 에셋을 가리키는 칸은 null로 역직렬화된다 — 그대로 담으면 CanHandle에서 터진다
                if (command == null)
                    continue;

                if (command is OverrideCommandsCommand overrideCommand)
                {
                    foreach (BaseCommandSO innerCmd in overrideCommand.Commands)
                        if (innerCmd != null && innerCmd is not OverrideCommandsCommand)
                            buffer.Add(innerCmd);
                }
                else
                {
                    buffer.Add(command);
                }
            }

            SortByPriority(buffer);
        }

        /// <summary>
        /// 주어진 커맨드 요소 목록을 우선 순위 값을 기준으로 정렬합니다.
        /// </summary>
        private static void SortByPriority(List<BaseCommandSO> buffer)
        {
            for (int i = 1; i < buffer.Count; i++)
            {
                BaseCommandSO current = buffer[i];
                int j = i - 1;

                while (j >= 0 && buffer[j].Priority < current.Priority)
                {
                    buffer[j + 1] = buffer[j];
                    j--;
                }

                buffer[j + 1] = current;
            }
        }
    }
}
