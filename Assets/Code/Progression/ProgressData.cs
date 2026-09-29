using System;
using System.Collections.Generic;

namespace Code.Progression
{
    /// <summary>
    /// 영속화되는 게임 진척 데이터입니다. JsonUtility로 직렬화되어 PlayerPrefs에 저장됩니다.
    /// 클리어한 스테이지, 해금 키, 범용 플래그를 담습니다.
    /// </summary>
    [Serializable]
    public class ProgressData
    {
        /// <summary>
        /// 클리어한 스테이지의 SceneId(정수) 목록입니다.
        /// </summary>
        public List<int> ClearedSceneIds;

        /// <summary>
        /// 해금된 요소/대사 트리거 키 목록입니다.
        /// </summary>
        public List<string> UnlockKeys;

        /// <summary>
        /// 대사 분기·카운터 등에 사용하는 범용 플래그(키-값) 목록입니다.
        /// </summary>
        public List<FlagEntry> Flags;

        /// <summary>
        /// 스테이지별 최고 기록입니다. 기록이 없는 스테이지는 항목 자체가 없습니다.
        /// </summary>
        public List<StageRecord> Records;

        /// <summary>
        /// 가장 최근에 클리어한 스테이지의 SceneId입니다. 없으면 -1.
        /// </summary>
        public int LastClearedSceneId = -1;

        /// <summary>
        /// 빈 진척 데이터를 생성합니다.
        /// </summary>
        public static ProgressData CreateEmpty() => new ProgressData
        {
            ClearedSceneIds = new List<int>(),
            UnlockKeys = new List<string>(),
            Flags = new List<FlagEntry>(),
            Records = new List<StageRecord>(),
            LastClearedSceneId = -1
        };

        /// <summary>
        /// 역직렬화 후 null 컬렉션을 빈 리스트로 보정합니다.
        /// </summary>
        public void EnsureInitialized()
        {
            ClearedSceneIds ??= new List<int>();
            UnlockKeys ??= new List<string>();
            Flags ??= new List<FlagEntry>();

            // Records는 나중에 생긴 필드라 옛 세이브에는 아예 없다
            Records ??= new List<StageRecord>();
        }

        /// <summary>
        /// 지정 키를 가진 플래그의 인덱스를 반환합니다. 없으면 -1을 반환합니다.
        /// </summary>
        public int IndexOfFlag(string key)
        {
            for (int i = 0; i < Flags.Count; i++)
                if (Flags[i].Key == key)
                    return i;

            return -1;
        }

        /// <summary>
        /// 지정 스테이지의 기록 인덱스를 반환합니다. 없으면 -1을 반환합니다.
        /// </summary>
        public int IndexOfRecord(int sceneId)
        {
            for (int i = 0; i < Records.Count; i++)
                if (Records[i].SceneId == sceneId)
                    return i;

            return -1;
        }
    }

    /// <summary>
    /// 범용 진척 플래그(키-값) 항목입니다. 대사 분기·진행 카운터 등에 사용합니다.
    /// </summary>
    [Serializable]
    public struct FlagEntry
    {
        public string Key;
        public int Value;
    }

    /// <summary>
    /// 스테이지 한 곳의 최고 기록입니다.
    /// </summary>
    [Serializable]
    public struct StageRecord
    {
        public int SceneId;
        public float BestSeconds;
    }
}
