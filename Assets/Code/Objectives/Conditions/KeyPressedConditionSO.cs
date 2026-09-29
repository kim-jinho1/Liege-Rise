using Code.Core.Input;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 튜토리얼에서 안내하는 단축키 종류입니다.
    /// </summary>
    public enum TutorialKey
    {
        BuildMenu,
        Advance
    }

    /// <summary>
    /// 지정한 단축키를 눌렀을 때 완료되는 조건입니다("이 키를 누르세요" 스텝용).
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_KeyPressed", menuName = "Objectives/Conditions/KeyPressed")]
    public class KeyPressedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField] public InputReader InputReader { get; private set; }
        [field: SerializeField] public TutorialKey Key { get; private set; }

        public override ConditionTracker CreateTracker()
            => new KeyPressedTracker(InputReader, Key);
    }

    public class KeyPressedTracker : ConditionTracker
    {
        private readonly InputReader _input;
        private readonly TutorialKey _key;

        public KeyPressedTracker(InputReader input, TutorialKey key)
        {
            _input = input;
            _key = key;
        }

        public override void Subscribe()
        {
            if (_input == null)
                return;

            switch (_key)
            {
                case TutorialKey.BuildMenu: _input.OnBuildMenuButton += HandlePressed; break;
                case TutorialKey.Advance: _input.OnAdvancePressed += HandlePressed; break;
            }
        }

        public override void Unsubscribe()
        {
            if (_input == null)
                return;

            switch (_key)
            {
                case TutorialKey.BuildMenu: _input.OnBuildMenuButton -= HandlePressed; break;
                case TutorialKey.Advance: _input.OnAdvancePressed -= HandlePressed; break;
            }
        }

        private void HandlePressed()
            => Complete();
    }
}
