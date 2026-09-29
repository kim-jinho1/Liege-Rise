using Code.Core.Input;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 플레이어가 카메라 이동 입력(WASD)을 했을 때 완료되는 조건입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_CameraMoved", menuName = "Objectives/Conditions/CameraMoved")]
    public class CameraMovedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField] public InputReader InputReader { get; private set; }

        public override ConditionTracker CreateTracker()
            => new CameraMovedTracker(InputReader);
    }

    public class CameraMovedTracker : ConditionTracker
    {
        private readonly InputReader _input;

        public CameraMovedTracker(InputReader input)
        {
            _input = input;
        }

        public override void Subscribe()
        {
            if (_input != null)
                _input.OnMovePerformed += OnMoved;
        }

        public override void Unsubscribe()
        {
            if (_input != null)
                _input.OnMovePerformed -= OnMoved;
        }

        private void OnMoved()
            => Complete();
    }
}
