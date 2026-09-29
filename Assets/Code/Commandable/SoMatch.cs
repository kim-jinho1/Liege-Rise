using UnityEngine;

namespace Code.Commandable
{
    /// <summary>
    /// 데이터 에셋이 같은 것인지 봅니다. DataManager가 유닛·건물의 SO를 판마다 복제본으로 바꿔 끼우므로
    /// 참조 비교는 원본 에셋과 영영 안 맞습니다 — 복제본은 이름에 "(Clone)"이 붙은 같은 타입입니다.
    /// </summary>
    public static class SoMatch
    {
        public static bool Same(ScriptableObject a, ScriptableObject b)
        {
            if (a == b)
                return true;

            if (a == null || b == null || a.GetType() != b.GetType())
                return false;

            return Strip(a.name) == Strip(b.name);
        }

        private static string Strip(string assetName)
            => assetName.EndsWith("(Clone)") ? assetName[..^7] : assetName;
    }
}
