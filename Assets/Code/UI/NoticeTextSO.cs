using UnityEngine;

namespace Code.UI
{
    /// <summary>
    /// 여러 커맨드가 거절 사유로 함께 쓰는 알림입니다.
    /// 한 커맨드만 쓰는 문구는 그 커맨드의 인스펙터 칸에 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NoticeText", menuName = "UI/Notice Text", order = 1)]
    public class NoticeTextSO : ScriptableObject
    {
        // 쓰는 쪽이 커맨드 SO라 씬에 배선할 칸이 없다
        private const string ResourcePath = "NoticeText";

        private static NoticeTextSO _cached;

        // 건설도 훈련도 같은 말을 쓴다. 각자 들고 있으면 두 벌이 된다
        [field: SerializeField] public string NotEnoughSupplies { get; private set; } = "자원이 부족합니다";

        // {0}에 "목재 +30 · 석재 +5"가 들어간다. 조사를 쓰지 않는다 —
        // 목재는 "를", 식량은 "을"이라 한 틀로 안 맞는다
        [field: SerializeField] public string SuppliesGrantedFormat { get; private set; } = "지원 물자 도착 — {0}";

        /// <summary>
        /// 공용 알림 문구를 읽습니다. 에셋이 없으면 코드 기본값으로 돌고 한 번만 경고합니다.
        /// </summary>
        public static NoticeTextSO Load()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<NoticeTextSO>(ResourcePath);
            if (_cached != null)
                return _cached;

            _cached = CreateInstance<NoticeTextSO>();
            Debug.LogWarning($"Resources/{ResourcePath} 을 찾지 못해 기본 문구로 알립니다.");
            return _cached;
        }
    }
}
