#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Commands.Base;
using UnityEngine;

namespace Code.Commandable
{
    /// <summary>
    /// 프리팹이 사라진 커맨드 에셋을 물고 있는지 알립니다. 에디터에서만 돕니다.
    /// </summary>
    public static class CommandArrayAudit
    {
        // 같은 프리팹이 수십 기 스폰돼도 콘솔을 채우지 않는다
        private static readonly HashSet<string> Reported = new();

        /// <summary>
        /// 커맨드 배열에 null 칸이 있으면 경고를 한 번 남깁니다.
        /// </summary>
        public static void Report(GameObject owner, BaseCommandSO[] commands)
        {
            if (owner == null || commands == null || !Reported.Add(owner.name))
                return;

            int missing = 0;
            foreach (BaseCommandSO command in commands)
                if (command == null)
                    missing++;

            // 사라진 에셋은 null로 역직렬화될 뿐 예외도 경고도 내지 않아, 알리지 않으면 조용히 묻힌다
            if (missing > 0)
                Debug.LogWarning($"{owner.name}: AvailableCommands에 사라진 에셋 칸이 {missing}개 있습니다.", owner);
        }
    }
}
#endif
