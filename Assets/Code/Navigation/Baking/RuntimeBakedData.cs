using UnityEngine;

namespace Code.Navigation.Baking
{
    /// <summary>
    /// 런타임에 쓸 베이크 데이터 사본을 만듭니다.
    /// 건물이 서고 무너질 때마다 그래프를 다시 굽는데, 그 대상이 에셋 자체면 고친 자국이 에셋에 남습니다.
    /// ScriptableObject는 플레이를 멈춰도 메모리에 살아 있어, 다음 판에는 지난 판 건물이 서 있던 자리에
    /// 구멍이 뚫린 채로 시작합니다 — 그 자리에는 건설도 안 되고 유닛도 돌아서 갑니다.
    /// </summary>
    public static class RuntimeBakedData
    {
        /// <summary>
        /// 플레이 중이면 사본을, 에디터 편집 중이면 원본을 돌려줍니다.
        /// 원본을 돌려주는 쪽이 맞습니다 — 전체 베이크는 에셋에 저장돼야 합니다.
        /// </summary>
        public static BakedDataSO ForPlay(BakedDataSO source)
        {
            if (source == null || !Application.isPlaying)
                return source;

            BakedDataSO copy = Object.Instantiate(source);
            copy.name = $"{source.name} (Runtime)";

            // 사본은 직렬화된 목록만 따라온다. 조회용 사전은 새로 채워야 한다
            copy.RebuildDict();
            return copy;
        }
    }
}
