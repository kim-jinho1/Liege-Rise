using Code.Core.Bus;
using Code.Military.Commanders;
using UnityEngine;

namespace Code.GameEvents.Unit
{
    public enum CommanderOrderKind
    {
        Assigned,  // 분대가 지휘관 밑에 들어갔다
        LineDrawn  // 지휘관이 공세선을 받았다
    }

    /// <summary>
    /// 지휘관에게 배속·공세선이 들어갔을 때입니다. 튜토리얼이 "배속했는가·선을 그었는가"를 이것으로 봅니다.
    /// </summary>
    public struct CommanderOrderEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public Commander Commander { get; private set; }
        public CommanderOrderKind Kind { get; private set; }
        public Vector2 LineCenter { get; private set; } // LineDrawn일 때만 뜻이 있다

        public CommanderOrderEvent(Owner owner, Commander commander, CommanderOrderKind kind, Vector2 lineCenter = default)
        {
            Owner = owner;
            Commander = commander;
            Kind = kind;
            LineCenter = lineCenter;
        }
    }
}
