using Code.Core.Bus;

namespace Code.GameEvents.Enemy
{
    /// <summary>
    /// 진행 중이던 공세 부대가 하나도 남지 않았음을 알리는 이벤트입니다.
    /// EnemyDispatchEvent와 짝을 이룹니다 — 출발이 있으면 끝도 있어야 합니다.
    /// </summary>
    public struct AssaultsClearedEvent : IEvent
    {
    }
}
