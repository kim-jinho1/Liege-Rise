using UnityEngine;

namespace Code.Commandable
{
    /// <summary>
    /// 같은 자리에서 잇달아 나오는 유닛을 겹치지 않게 흩어 놓습니다.
    /// 한 점에 그대로 뱉으면 스프라이트가 포개져 몇 기가 나왔는지 보이지 않습니다.
    /// </summary>
    public static class SpawnScatter
    {
        // 황금각. 무작위와 달리 순번이 이어져도 각이 겹치지 않아 나온 수가 눈으로 세어진다
        private const float GoldenAngleDegrees = 137.5f;

        // 이웃한 두 자리 사이의 최소 간격(월드 단위 = 타일 수).
        // 5기가 반경 1.1타일 안에 들어와 절벽·건물을 파고들 여지가 거의 없다
        private const float DefaultSpacing = 0.5f;

        /// <summary>
        /// 중심에서 순번만큼 떨어진 자리를 돌려줍니다. 0번은 중심 그대로입니다.
        /// </summary>
        public static Vector3 Around(Vector3 center, int index, float spacing = DefaultSpacing)
        {
            if (index <= 0 || spacing <= 0f)
                return center;

            // 나선(phyllotaxis) — 반지름이 √n으로 자라 밀도가 고르게 유지된다
            float angle = index * GoldenAngleDegrees * Mathf.Deg2Rad;
            float radius = spacing * Mathf.Sqrt(index);

            return center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }
    }
}
