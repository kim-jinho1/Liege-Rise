using UnityEngine;

namespace Code.FogSystem
{
    /// <summary>
    /// 안개에 가려진 대상을 마우스 조작에서 걸러내는 공용 판정입니다.
    /// 시야를 벗어나도 콜라이더는 살아 있어 막지 않으면 클릭·호버에 그대로 반응합니다.
    /// </summary>
    public static class FogVisibility
    {
        /// <summary>
        /// 지금 플레이어에게 보이는지 판정합니다. IHideable이 아닌 대상은 늘 보이는 것으로 봅니다.
        /// </summary>
        public static bool IsVisibleNow(Component hit)
        {
            if (hit == null)
                return false;

            IHideable hideable = hit.GetComponentInParent<IHideable>();
            return hideable == null || hideable.IsVisible;
        }
    }
}
