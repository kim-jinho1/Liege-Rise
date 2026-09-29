using Code.Core;
using Code.Stages;
using UnityEngine;

namespace Code.Progression
{
    /// <summary>
    /// 게임 진척(클리어한 스테이지·해금 키·범용 플래그)을 PlayerPrefs에 영속화하는 정적 서비스입니다.
    /// 씬에 종속되지 않으며, 다른 시스템은 질의/기록 메서드로만 진척에 접근합니다.
    /// </summary>
    public static class ProgressService
    {
        private const string SaveKey = "progress_save";

        private static ProgressData _data;
        private static bool _loaded;

        private static ProgressData Data
        {
            get
            {
                if (!_loaded)
                    Load();

                return _data;
            }
        }

        /// <summary>
        /// 진척이 하나라도 존재하는지(클리어한 스테이지가 있는지) 여부입니다.
        /// </summary>
        public static bool HasAnyProgress
            => Data.ClearedSceneIds.Count > 0;

        /// <summary>
        /// 가장 최근에 클리어한 스테이지입니다. 없으면 null을 반환합니다.
        /// </summary>
        public static SceneId? LastClearedStage =>
            Data.LastClearedSceneId < 0 ? null : (SceneId)Data.LastClearedSceneId;

        /// <summary>
        /// PlayerPrefs에서 진척 데이터를 불러옵니다. 저장된 데이터가 없으면 빈 데이터로 초기화합니다.
        /// </summary>
        public static void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                _data = ProgressData.CreateEmpty();
            }
            else
            {
                try
                {
                    _data = JsonUtility.FromJson<ProgressData>(json) ?? ProgressData.CreateEmpty();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ProgressService] 저장 데이터 파싱 실패, 초기화합니다: {e}");
                    _data = ProgressData.CreateEmpty();
                }
            }

            _data.EnsureInitialized();
            _loaded = true;
        }

        /// <summary>
        /// 지정 스테이지를 클리어한 적이 있는지 반환합니다.
        /// </summary>
        public static bool IsStageCleared(SceneId scene)
            => Data.ClearedSceneIds.Contains((int)scene);

        /// <summary>
        /// 지정 스테이지의 최고 기록(초)입니다. 기록이 없으면 0보다 작은 값을 반환합니다.
        /// </summary>
        public static float GetBestSeconds(SceneId scene)
        {
            int index = Data.IndexOfRecord((int)scene);
            if (index < 0)
                return -1f;

            // 0초 이하는 기록이 아니라 오염이다 — 없는 것으로 답해 다음 클리어가 덮어쓰게 한다.
            // 승리 조건이 즉시 통과하던 시절에 0.0초가 저장됐고, 더 낮은 값만 남기는 규칙 때문에
            // 그 뒤로 어떤 기록도 이것을 이기지 못했다
            float best = Data.Records[index].BestSeconds;
            return best > 0f ? best : -1f;
        }

        /// <summary>
        /// 스테이지 클리어를 기록하고, 해당 스테이지가 부여하는 해금 키를 적용한 뒤 저장합니다.
        /// </summary>
        public static void RecordStageClear(StageSO stage)
            => RecordStageClear(stage, -1f);

        /// <summary>
        /// 클리어와 함께 소요 시간을 기록합니다. 더 느린 기록은 무시하므로 최고 기록만 남습니다.
        /// seconds가 0보다 작으면 시간은 건드리지 않습니다.
        /// </summary>
        public static void RecordStageClear(StageSO stage, float seconds)
        {
            if (stage == null)
                return;

            int id = (int)stage.Scene;
            if (seconds > 0f)
                RecordBestSeconds(id, seconds);

            if (!Data.ClearedSceneIds.Contains(id))
                Data.ClearedSceneIds.Add(id);

            Data.LastClearedSceneId = id;
            foreach (string key in stage.GrantsUnlockKeysOnClear)
            {
                if (string.IsNullOrEmpty(key) || Data.UnlockKeys.Contains(key))
                    continue;

                Data.UnlockKeys.Add(key);
            }

            Save();
            Debug.Log($"[Progress] 클리어 기록 — {stage.DisplayName} {seconds:0.0}초 (최고 {GetBestSeconds(stage.Scene):0.0}초)");
        }

        private static void RecordBestSeconds(int sceneId, float seconds)
        {
            int index = Data.IndexOfRecord(sceneId);
            if (index < 0)
            {
                Data.Records.Add(new StageRecord { SceneId = sceneId, BestSeconds = seconds });
                return;
            }

            // 0초 이하로 굳은 옛 기록은 무조건 덮는다. 안 그러면 그 항목이 영영 안 풀린다
            float stored = Data.Records[index].BestSeconds;
            if (stored > 0f && seconds >= stored)
                return;

            StageRecord record = Data.Records[index];
            record.BestSeconds = seconds;
            Data.Records[index] = record;
        }

        /// <summary>
        /// 선행 스테이지가 모두 클리어되어 지정 스테이지가 해금되었는지 반환합니다.
        /// </summary>
        public static bool IsStageUnlocked(StageSO stage)
        {
            if (stage == null)
                return false;

            foreach (SceneId prerequisite in stage.PrerequisiteStages)
                if (!IsStageCleared(prerequisite))
                    return false;

            return true;
        }

        /// <summary>
        /// 지정 키가 해금되었는지 반환합니다(요소 해금·대사 트리거 질의용).
        /// </summary>
        public static bool IsUnlocked(string key)
            => !string.IsNullOrEmpty(key) && Data.UnlockKeys.Contains(key);

        /// <summary>
        /// 지정 키를 해금하고 저장합니다.
        /// </summary>
        public static void Unlock(string key)
        {
            if (string.IsNullOrEmpty(key) || Data.UnlockKeys.Contains(key))
                return;

            Data.UnlockKeys.Add(key);
            Save();
        }

        /// <summary>
        /// 지정 키의 플래그 값을 반환합니다. 없으면 defaultValue를 반환합니다(대사 분기·카운터 질의용).
        /// </summary>
        public static int GetFlag(string key, int defaultValue = 0)
        {
            int index = Data.IndexOfFlag(key);
            return index < 0 ? defaultValue : Data.Flags[index].Value;
        }

        /// <summary>
        /// 지정 키의 플래그 값을 설정하고 저장합니다.
        /// </summary>
        public static void SetFlag(string key, int value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            int index = Data.IndexOfFlag(key);
            if (index < 0)
            {
                Data.Flags.Add(new FlagEntry { Key = key, Value = value });
            }
            else
            {
                FlagEntry entry = Data.Flags[index];
                entry.Value = value;
                Data.Flags[index] = entry;
            }

            Save();
        }

        /// <summary>
        /// 모든 진척을 삭제하고 저장합니다(디버그/초기화용).
        /// </summary>
        public static void ResetAll()
        {
            _data = ProgressData.CreateEmpty();
            _data.EnsureInitialized();
            _loaded = true;
            Save();
        }

        private static void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }
    }
}
