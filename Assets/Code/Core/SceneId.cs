namespace Code.Core
{
    /// <summary>
    /// Build Settings에 등록된 씬을 가리키는 식별자입니다.
    /// 각 값은 Build Settings의 씬 인덱스와 반드시 일치시켜야 합니다(인덱스 기반 로드로 성능·오타 방지).
    /// </summary>
    public enum SceneId
    {
        MainMenu = 0,
        Core = 1,
        Stage1 = 2,
        Stage2 = 3,
    }

    /// <summary>
    /// SceneId 판정 헬퍼입니다.
    /// </summary>
    public static class SceneIds
    {
        private const int FirstStage = (int)SceneId.Stage1;
        private const int LastStage = (int)SceneId.Stage2;

        /// <summary>
        /// 해당 빌드 인덱스가 스테이지(플레이 맵) 씬인지 반환합니다.
        /// </summary>
        public static bool IsStage(int buildIndex)
            => buildIndex is >= FirstStage and <= LastStage;
    }
}
