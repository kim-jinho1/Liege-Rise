namespace Code.Navigation.Graph
{
    /// <summary>
    /// 타일의 지형 성격입니다. 고도 자체는 NodeData.level이 들고 있습니다.
    /// </summary>
    public enum TopographyType
    {
        None = 0,
        Flat,       // 평지·고원 공통 (통행 O)
        Ramp,       // 한 단계 위 층과의 연결을 허용하는 셀
    }
}
