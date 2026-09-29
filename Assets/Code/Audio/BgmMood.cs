namespace Code.Audio
{
    /// <summary>
    /// 배경음이 따라가는 전황 단계입니다. 뒤로 갈수록 급합니다.
    /// </summary>
    public enum BgmMood
    {
        /// <summary>판 초반입니다. 아직 압박이 없습니다.</summary>
        Calm,

        /// <summary>압박이 시작된 뒤의 기준 분위기입니다.</summary>
        Tension,

        /// <summary>공세가 오는 동안의 분위기입니다.</summary>
        Crisis
    }
}
