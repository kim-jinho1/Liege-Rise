using System;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Sequence;
using DG.DemiLib.Attributes;

namespace Code.Objectives
{
    [DeScriptExecutionOrder(-10)] // 다른 시스템보다 먼저 초기화되어야 할 수 있습니다.
    public class ObjectiveManager : MonoSingleton<ObjectiveManager>
    {
        /// <summary>
        /// 활성 목표가 바뀌었을 때 발생합니다. 목표가 끝나 아무것도 없으면 Active가 null입니다.
        /// </summary>
        public event Action ActiveChanged;

        /// <summary>
        /// 지금 추적 중인 목표입니다. 표시부가 제목·설명을 여기서 읽습니다.
        /// </summary>
        public ObjectiveSO Active { get; private set; }

        /// <summary>
        /// 지금 추적 중인 트래커입니다. 진행도를 읽거나 OnProgress를 구독할 때 씁니다.
        /// </summary>
        public ConditionTracker ActiveTracker { get; private set; }

        /// <summary>
        /// 목표 하나를 활성화하고 완료를 추적합니다.
        /// 조건이 없으면(안내 전용 스텝 등) 즉시 완료 처리해 다음으로 넘깁니다.
        /// </summary>
        public void Activate(ObjectiveSO objective)
        {
            ClearActive();

            Active = objective;
            ObjectiveConditionSO condition = objective != null ? objective.Condition : null;

            if (condition == null)
            {
                // 표시부가 안내 문구를 한 번은 볼 수 있도록 알린 뒤에 넘긴다
                ActiveChanged?.Invoke();
                Bus<ObjectiveCompletedEvent>.Raise(Owner.Player, new ObjectiveCompletedEvent());
                return;
            }

            ActiveTracker = condition.CreateTracker();
            ActiveTracker.OnComplete += HandleComplete;
            ActiveTracker.Subscribe();

            ActiveChanged?.Invoke();
        }

        /// <summary>
        /// 추적을 멈추고 표시부를 비웁니다. 시퀀스가 끝났을 때 부릅니다.
        /// </summary>
        public void Clear()
        {
            ClearActive();
            ActiveChanged?.Invoke();
        }

        private void HandleComplete()
        {
            ClearActive();
            Bus<ObjectiveCompletedEvent>.Raise(Owner.Player, new ObjectiveCompletedEvent());
        }

        private void ClearActive()
        {
            if (ActiveTracker != null)
            {
                ActiveTracker.OnComplete -= HandleComplete;
                ActiveTracker.Unsubscribe();
            }

            ActiveTracker = null;
            Active = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearActive();
        }
    }
}
