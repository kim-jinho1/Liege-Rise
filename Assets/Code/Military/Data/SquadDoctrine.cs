namespace Code.Military.Data
{
    /// <summary>
    /// 적 지휘관이 이 분대로 무엇을 하는지입니다. 플레이어 분대는 직접 조종하므로 읽지 않습니다.
    /// </summary>
    public enum SquadDoctrine
    {
        Frontal, // 본부를 향해 가로선으로 민다
        Flank    // 근처 플레이어 분대의 옆구리로 돌아 들어간다
    }
}
