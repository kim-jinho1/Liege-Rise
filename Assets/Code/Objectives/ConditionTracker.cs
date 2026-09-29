using System;

namespace Code.Objectives
{
    public abstract class ConditionTracker
    {
        public event Action OnComplete;

        /// <summary>
        /// 진행도가 바뀌었을 때 발생합니다. 완료 직전의 마지막 변화도 여기로 먼저 나갑니다.
        /// </summary>
        public event Action OnProgress;

        /// <summary>
        /// 지금까지 달성한 수량입니다. 셀 것이 없는 조건은 0으로 둡니다.
        /// </summary>
        public virtual int Current => 0;

        /// <summary>
        /// 완료에 필요한 수량입니다. 셀 것이 없는 조건은 0으로 둡니다.
        /// </summary>
        public virtual int Target => 0;

        /// <summary>
        /// 숫자로 보여 줄 가치가 있는 목표인지입니다.
        /// "병영을 지으세요"에 0/1을 띄우면 도움이 아니라 방해입니다.
        /// </summary>
        public bool HasProgress => Target > 1;

        public abstract void Subscribe();
        public abstract void Unsubscribe();

        /// <summary>
        /// 진행도가 바뀐 트래커가 부릅니다. 완료 여부와 무관하게 표시부터 갱신합니다.
        /// </summary>
        protected void Report()
            => OnProgress?.Invoke();

        protected void Complete()
        {
            Unsubscribe();
            OnComplete?.Invoke();
        }
    }
}
