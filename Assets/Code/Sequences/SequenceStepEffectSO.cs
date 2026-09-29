using UnityEngine;

namespace Code.Sequences
{
    /// <summary>
    /// 시퀀스 스텝 진입 시 1회 실행되는 효과(카메라 이동, 유닛·자원 지급 등)의 베이스입니다.
    /// SO에 런타임 상태를 저장하지 않으며, Execute에서 씬 싱글톤/오브젝트를 찾아 동작합니다.
    /// </summary>
    public abstract class SequenceStepEffectSO : ScriptableObject
    {
        /// <summary>
        /// 즉시 끝나는 효과는 이 메서드만 구현합니다(자원·유닛 지급, 카메라 점프 등).
        /// </summary>
        public virtual void Execute() { }

        /// <summary>
        /// 스텝 진입 시 호출됩니다. 기본 구현은 Execute()를 즉시 실행합니다.
        /// 타임라인처럼 재생이 끝날 때까지 다음 진행을 막아야 하는 효과는 이 메서드를 override 합니다.
        /// </summary>
        public virtual Awaitable ExecuteAsync()
        {
            Execute();
            return null;
        }
    }
}
