using Code.Commands.Base;
using UnityEditor;
using UnityEngine;

namespace Code.Progression.Editor
{
    /// <summary>
    /// 디버그용 저장 데이터 초기화 메뉴를 제공하는 에디터 전용 유틸리티입니다.
    /// </summary>
    public static class SaveDataDebugMenu
    {
        /// <summary>
        /// 확인 후 모든 PlayerPrefs(진행 + 오디오)를 삭제하고 진행 데이터 캐시를 초기화합니다.
        /// </summary>
        [MenuItem("Tools/Debug/Clear Save Data")]
        private static void ClearSaveData()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Save Data",
                "Delete ALL saved data (progress + audio settings)?\nThis cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // 정적 캐시가 남아 다음 Play 세션에 옛 진행이 보이는 것을 막기 위해 빈 상태로 다시 로드합니다.
            ProgressService.Load();
            Debug.Log("[SaveDataDebug] All PlayerPrefs cleared (progress + audio).");
        }

        /// <summary>
        /// 프로젝트의 모든 커맨드 UnlockKey를 즉시 해금합니다.
        /// 해금 조건 없이 전 건물·유닛을 테스트할 때 사용합니다.
        /// </summary>
        [MenuItem("Tools/Debug/Unlock All Progress Keys")]
        private static void UnlockAllProgressKeys()
        {
            int unlocked = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:BaseCommandSO"))
            {
                var command = AssetDatabase.LoadAssetAtPath<BaseCommandSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (command != null && TryUnlock(command.UnlockKey))
                    unlocked++;
            }

            Debug.Log($"[SaveDataDebug] Unlocked {unlocked} progress keys.");
        }

        private static bool TryUnlock(string key)
        {
            if (string.IsNullOrEmpty(key) || ProgressService.IsUnlocked(key))
                return false;

            ProgressService.Unlock(key);
            return true;
        }
    }
}
