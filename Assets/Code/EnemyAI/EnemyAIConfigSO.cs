using System;
using Code.Military.Data;
using UnityEngine;

namespace Code.EnemyAI
{
    /// <summary>
    /// 공세 한 번이 언제·얼마나·몇 갈래로 오는지 적은 한 줄입니다.
    /// </summary>
    [Serializable]
    public struct AssaultWave
    {
        // 게임 시작 후 이 웨이브가 출발하는 시각 (초)
        [Min(0f)] public float atSeconds;

        // 이번 웨이브에 내보낼 분대 수
        [Min(0)] public int squadCount;

        // 한 갈래가 데려갈 분대 수의 상한. 갈래 수는 여기서 계산된다 (3이면 7부대는 3갈래)
        [Min(1)] public int maxSquadsPerColumn;

        // 이번 웨이브에 나올 적 분대 종류. 비워 두면 EnemyAIConfigSO.DefaultRoster를 쓴다
        public SquadSO[] roster;

        /// <summary>
        /// 이번 웨이브가 몇 갈래로 나뉘는지입니다. 살아 있는 출발지 수를 넘지 않습니다.
        /// </summary>
        public int ResolveColumns(int originCount)
        {
            if (originCount <= 0 || squadCount <= 0)
                return 0;

            int perColumn = Mathf.Max(1, maxSquadsPerColumn);
            int wanted = Mathf.CeilToInt(squadCount / (float)perColumn);
            return Mathf.Clamp(wanted, 1, originCount);
        }
    }

    /// <summary>
    /// 적 압박을 통째로 정하는 표입니다. 공세는 병력이 쌓여서 나가는 것이 아니라
    /// 여기 적힌 시각에 그 자리에서 생성돼 출발합니다 — 표의 숫자가 곧 난이도입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyAIConfig", menuName = "SO/Enemy/EnemyAIConfig", order = 0)]
    public class EnemyAIConfigSO : ScriptableObject
    {
        [Header("공세 시간표")]
        // 순서가 뒤바뀌어 있어도 시각순으로 정렬해 쓴다
        [field: SerializeField] public AssaultWave[] Waves { get; private set; } = DefaultWaves();

        // 표를 다 쓴 뒤 마지막 줄을 되풀이할 간격 (초)
        [field: SerializeField, Min(10f)] public float RepeatInterval { get; private set; } = 180f;

        // 끄면 표를 다 쓴 뒤 더는 오지 않는다. 튜토리얼처럼 딱 한 번만 올 때
        [field: SerializeField] public bool RepeatLastWave { get; private set; } = true;

        [Header("적 구성")]
        // 웨이브가 자기 roster를 비워 뒀을 때 쓸 기본 분대 목록
        [field: SerializeField] public SquadSO[] DefaultRoster { get; private set; } = Array.Empty<SquadSO>();

        [Header("지휘관")]
        // 갈래마다 하나씩 붙는 적 지휘관. 비워 두면 예전처럼 분대가 본부로 직진한다
        [field: SerializeField] public CommanderSO Commander { get; private set; }

        // 우회 성향 갈래가 이 거리 안의 플레이어 분대를 보면 옆구리로 선을 긋는다
        [field: SerializeField, Min(0f)] public float FlankRange { get; private set; } = 18f;

        // 옆구리를 맞은 갈래가 이 거리 안의 놀고 있는 다른 갈래를 요격에 부른다
        [field: SerializeField, Min(0f)] public float ReserveRange { get; private set; } = 30f;

        [Header("진지 수비")]
        // 진지 하나가 곁에 두는 수비 병력의 상한(병력 수). 차면 그 진지의 스포너는 생산을 쉰다
        [field: SerializeField, Min(0)] public int DefenseCapacity { get; private set; } = 7;

        // 한 갈래의 분대들을 출발지 주변에 흩어 놓을 반경
        [field: SerializeField, Min(0f)] public float SpawnScatterRadius { get; private set; } = 2.5f;

        private AssaultWave[] _ordered;

        /// <summary>
        /// 순번에 해당하는 웨이브를 돌려줍니다. 표를 다 쓰면 마지막 웨이브를 RepeatInterval 간격으로 되풀이합니다.
        /// </summary>
        public bool TryGetWave(int index, out AssaultWave wave, out float atSeconds)
        {
            wave = default;
            atSeconds = 0f;

            AssaultWave[] ordered = Ordered;
            if (ordered.Length == 0 || index < 0)
                return false;

            if (index < ordered.Length)
            {
                wave = ordered[index];
                atSeconds = wave.atSeconds;
                return true;
            }

            // 표를 다 썼다 — 마지막 줄을 간격만큼 밀어 되풀이한다
            if (!RepeatLastWave)
                return false;

            wave = ordered[^1];
            atSeconds = wave.atSeconds + (index - ordered.Length + 1) * Mathf.Max(10f, RepeatInterval);
            return true;
        }

        /// <summary>
        /// 이 웨이브가 쓸 적 분대 목록입니다. 웨이브가 비워 뒀으면 기본 목록으로 되돌아갑니다.
        /// </summary>
        public SquadSO[] RosterFor(AssaultWave wave)
            => wave.roster is { Length: > 0 } ? wave.roster : DefaultRoster;

        /// <summary>
        /// 시각 오름차순으로 정렬한 사본입니다. 인스펙터에서 줄 순서가 뒤바뀌어도 표대로 돌아갑니다.
        /// </summary>
        private AssaultWave[] Ordered
        {
            get
            {
                int length = Waves?.Length ?? 0;
                if (_ordered != null && _ordered.Length == length)
                    return _ordered;

                _ordered = length == 0 ? Array.Empty<AssaultWave>() : (AssaultWave[])Waves.Clone();
                Array.Sort(_ordered, (left, right) => left.atSeconds.CompareTo(right.atSeconds));
                return _ordered;
            }
        }

        /// <summary>
        /// 에셋을 만들 때 곧바로 쓸 수 있게 5분 시작·3분 간격·한 부대씩 증가로 채워 둡니다.
        /// 적 분대 목록만 채우면 그대로 돌아갑니다.
        /// </summary>
        private static AssaultWave[] DefaultWaves()
        {
            AssaultWave[] waves = new AssaultWave[9];

            for (int i = 0; i < waves.Length; i++)
                waves[i] = new AssaultWave
                {
                    atSeconds = 300f + i * 180f,
                    squadCount = i + 1,
                    maxSquadsPerColumn = 3,
                    roster = Array.Empty<SquadSO>()
                };

            return waves;
        }
    }
}
