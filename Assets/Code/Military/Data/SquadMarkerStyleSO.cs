using TMPro;
using UnityEngine;

namespace Code.Military.Data
{
    /// <summary>
    /// 분대 위에 뜨는 표시(진격 화살표·병종 깃발)의 생김새입니다.
    /// 잔디 위에서 눈으로 맞춰야 하는 값이라 코드가 아니라 데이터가 쥡니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SquadMarkerStyle", menuName = "Military/Squad Marker Style", order = 20)]
    public class SquadMarkerStyleSO : ScriptableObject
    {
        // Resources에 두는 이유는 CursorSetSO와 같다 — 뷰가 런타임에 붙어 인스펙터 칸이 없다
        private const string ResourcePath = "SquadMarkerStyle";

        private static SquadMarkerStyleSO _cached;

        [field: Header("색 — 진영이 아니라 명령이 정한다")]
        [field: SerializeField] public Color MoveFill { get; private set; } = new(0.95f, 0.94f, 0.88f, 1f);
        [field: SerializeField] public Color MoveEdge { get; private set; } = new(0.16f, 0.15f, 0.12f, 1f);
        [field: SerializeField] public Color AttackFill { get; private set; } = new(0.85f, 0.22f, 0.16f, 1f);
        [field: SerializeField] public Color AttackEdge { get; private set; } = new(0.29f, 0.06f, 0.04f, 1f);

        [field: Header("자루")]
        [field: SerializeField] public float TailWidthPerSpread { get; private set; } = 0.45f;
        [field: SerializeField] public float MinTailWidth { get; private set; } = 0.9f;  // 집중 대형
        [field: SerializeField] public float MaxTailWidth { get; private set; } = 3.4f;  // 산개 대형
        [field: SerializeField] public float NeckWidthScale { get; private set; } = 0.55f;

        [field: Header("화살촉")] // 폭 배율이 1보다 커야 자루와 구분돼 촉으로 읽힌다
        [field: SerializeField] public float HeadWidthScale { get; private set; } = 1.35f;
        [field: SerializeField] public float HeadLengthScale { get; private set; } = 1f;
        [field: SerializeField, Range(0.2f, 0.9f)] public float HeadLengthCap { get; private set; } = 0.55f;

        [field: Header("거리 비례")] // 코앞을 가리키는 화살표가 뭉툭한 덩어리로 보이지 않게 한다
        [field: SerializeField] public float FullWidthLength { get; private set; } = 9f;
        [field: SerializeField, Range(0.05f, 1f)] public float ShortWidthScale { get; private set; } = 0.35f;

        [field: Header("마감")]
        [field: SerializeField] public float FadeLength { get; private set; } = 2f;
        [field: SerializeField, Range(0.1f, 1f)] public float FadeCap { get; private set; } = 0.8f;
        [field: SerializeField] public float OutlineWidth { get; private set; } = 0.08f;
        [field: SerializeField] public float MinLength { get; private set; } = 0.6f;

        [field: Header("분대 깃발")]
        [field: SerializeField] public float FlagHeightOffset { get; private set; } = 1.4f;
        // 분대원은 Y 정렬로 순서가 계속 바뀐다. 깃발은 언제나 그 위에 있어야 해 정렬에서 빼 둔다
        [field: SerializeField] public int FlagSortingOrder { get; private set; } = 30000;

        [field: Header("방향 피해 부채꼴")]
        // 각도는 여기 없다 — SquadSO의 FrontConeAngle·RearConeAngle을 그대로 그린다
        [field: SerializeField] public float ArcRadius { get; private set; } = 2f;
        [field: SerializeField, Range(0f, 1f)] public float ArcFillAlpha { get; private set; } = 0.3f;
        [field: SerializeField, Range(0f, 1f)] public float ArcEdgeAlpha { get; private set; } = 1f;
        [field: SerializeField] public float ArcEdgeWidth { get; private set; } = 0.09f;

        // 정면이 아닌 구간을 옅게 만드는 배율. 적 분대에 늘 띄워도 화면이 조용하도록 하는 값이다
        [field: SerializeField, Range(0f, 1f)] public float ArcMutedScale { get; private set; } = 0.45f;

        [field: SerializeField, Range(0f, 1f)] public float ArcFlashAlpha { get; private set; } = 0.9f;
        [field: SerializeField] public float ArcFlashSeconds { get; private set; } = 0.45f;

        // 끄면 아군 부채꼴은 선택했을 때만 뜬다. 방향은 이제 공세선 화살표가 말하므로 기본은 끈다
        [field: SerializeField] public bool ArcAlwaysOnAllies { get; private set; }

        [field: SerializeField] public Color ArcFrontColor { get; private set; } = new(0.24f, 0.43f, 0.56f, 1f);
        [field: SerializeField] public Color ArcSideColor { get; private set; } = new(0.71f, 0.51f, 0.17f, 1f);
        [field: SerializeField] public Color ArcRearColor { get; private set; } = new(0.68f, 0.25f, 0.17f, 1f);

        [field: Header("지휘관")] // 임명 순서대로 돌려 쓴다. 깃발·공세선·배속 분대 깃발이 같은 색을 받는다
        // 아군 전용이다. 적에게도 돌려 쓰면 색이 진영이 아니라 번호를 말하게 된다
        [field: SerializeField] public Color[] CommanderColors { get; private set; } =
        {
            new(0.25f, 0.52f, 0.85f, 1f), new(0.85f, 0.30f, 0.25f, 1f), new(0.30f, 0.70f, 0.35f, 1f),
            new(0.85f, 0.65f, 0.20f, 1f), new(0.60f, 0.35f, 0.80f, 1f), new(0.20f, 0.70f, 0.75f, 1f)
        };

        // 적 지휘관은 몇이 서든 이 한 색이다. 아군 팔레트와 계열이 겹치지 않게 어둡게 잡는다
        [field: SerializeField] public Color EnemyCommanderColor { get; private set; } = new(0.62f, 0.09f, 0.11f, 1f);
        [field: SerializeField] public Sprite CommanderFlag { get; private set; } // 비면 지휘관 머리 위에 깃발이 없다
        [field: SerializeField] public TMP_FontAsset FlagFont { get; private set; } // 비면 TMP 기본 폰트
        [field: SerializeField] public float FlagNumberSize { get; private set; } = 3f;
        [field: SerializeField] public float SelectedFlagScale { get; private set; } = 1.3f;

        [field: Header("공세선")]
        [field: SerializeField] public Color LinePreviewColor { get; private set; } = new(0.95f, 0.94f, 0.88f, 0.9f);
        [field: SerializeField] public Color LineEdgeColor { get; private set; } = new(0.16f, 0.15f, 0.12f, 1f);
        [field: SerializeField] public float LineWidth { get; private set; } = 0.3f;
        [field: SerializeField] public float LineOutlineWidth { get; private set; } = 0.08f;
        [field: SerializeField] public float LineArrowLength { get; private set; } = 4f; // 근처에 적이 없을 때 곧게 뻗는 길이
        [field: SerializeField] public float LineArrowWidth { get; private set; } = 1.4f; // 꼬리 폭. 자루·촉은 진격 화살표와 같은 배율
        [field: SerializeField] public float LineProbeRadius { get; private set; } = 12f; // 이 안의 가장 가까운 적 분대로 구간을 판정한다
        [field: SerializeField] public float LineLabelSize { get; private set; } = 3.5f;
        [field: SerializeField] public string FrontLabel { get; private set; } = "정면";
        [field: SerializeField] public string SideLabel { get; private set; } = "측면";
        [field: SerializeField] public string RearLabel { get; private set; } = "후면";

        [field: Header("정렬")]
        // 유닛과 같은 정렬 레이어의 맨 아래. 지형·장식보다는 위, 유닛보다는 아래에 깔린다
        [field: SerializeField] public int ArrowSortingOrder { get; private set; } = -30000;

        // 진격 화살표보다 한 겹 위. 분대 화살표가 전선 막대에 묻히지 않는다
        [field: SerializeField] public int LineSortingOrder { get; private set; } = -29900;

        // 화살표보다 한 겹 더 아래. 진격 화살표가 부채꼴에 묻히면 안 된다
        [field: SerializeField] public int ArcSortingOrder { get; private set; } = -30100;

        /// <summary>
        /// 공용 스타일을 읽습니다. 에셋이 없으면 코드 기본값으로 만들어 돌려주고 한 번만 경고합니다 —
        /// 에셋 하나가 빠졌다고 화면에서 표시가 통째로 사라지면 원인을 찾기 어렵습니다.
        /// </summary>
        public static SquadMarkerStyleSO Load()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<SquadMarkerStyleSO>(ResourcePath);
            if (_cached != null)
                return _cached;

            _cached = CreateInstance<SquadMarkerStyleSO>();
            Debug.LogWarning($"Resources/{ResourcePath} 을 찾지 못해 기본값으로 그립니다.");
            return _cached;
        }
    }
}
