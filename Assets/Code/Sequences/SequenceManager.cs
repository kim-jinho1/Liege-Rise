using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Sequence;
using Code.GameEvents.System;
using Code.Objectives;
using Code.Sequences.Guide;
using Code.Stages;
using Code.UI.Sequence;
using UnityEngine;

namespace Code.Sequences
{
    /// <summary>
    /// SequenceSO에 정의된 순차적 연출을 관리하는 싱글톤입니다.
    /// 각 스텝의 진입 효과를 순서대로 실행하고, 대사와 완료 조건을 활성화합니다.
    /// </summary>
    public class SequenceManager : MonoSingleton<SequenceManager>
    {
        [Header("Sequence")]
        [SerializeField] private SequenceSO sequence;
        [SerializeField] private SequenceDialogUI dialogUI;
        [SerializeField] private SequenceHighlighter gameObjHighlighter;
        [SerializeField] private SequenceUIHighlighter uiHighlighter;

        private int _currentIndex;
        private SequenceStepSO _step;     // 지금 스텝. 손이 월드를 짚는 차례에 테두리가 돌아갈 칸을 안다
        private GuideCursorPlayer _guide; // 씬 배선 없이 여기서 붙인다. 오버레이 캔버스도 스스로 만든다

        /// <summary>
        /// 아직 실행하지 않은 스텝이 남아 있는지 여부입니다.
        /// </summary>
        public bool HasPendingSteps
            => sequence != null && sequence.Steps != null && _currentIndex < sequence.Steps.Length;

        protected override void Awake()
        {
            base.Awake();

            StageSO stage = StageDatabaseSO.Current;
            if (stage != null && stage.SequenceOverride != null)
                sequence = stage.SequenceOverride;

            _guide = gameObject.AddComponent<GuideCursorPlayer>();
            _guide.UiFocusChanged += HandleGuideUiFocus;
            Bus<ObjectiveCompletedEvent>.OnEvent[Owner.Player] += HandleObjectiveCompleted;
            Bus<GameOverEvent>.RegisterForAll(HandleGameOver);
        }

        protected override void OnDestroy()
        {
            if (_guide != null)
                _guide.UiFocusChanged -= HandleGuideUiFocus;

            Bus<ObjectiveCompletedEvent>.OnEvent[Owner.Player] -= HandleObjectiveCompleted;
            Bus<GameOverEvent>.UnRegisterForAll(HandleGameOver);
            base.OnDestroy();
        }

        private void Start()
            => _ = LoadStepAsync(0);

        /// <summary>
        /// 지정 인덱스의 스텝을 로드합니다. 진입 효과(연출)를 순서대로 실행하며,
        /// 타임라인 등 대기형 효과는 끝날 때까지 기다린 뒤 대사·완료 조건을 활성화합니다.
        /// </summary>
        private async Awaitable LoadStepAsync(int index)
        {
            // 시퀀스가 없는 스테이지에서도 이 컴포넌트가 남아 있을 수 있습니다.
            if (sequence == null || sequence.Steps == null)
                return;

            if (index >= sequence.Steps.Length)
            {
                // 마지막 목표가 화면에 남지 않도록 표시부부터 비운다
                if (ObjectiveManager.Instance != null)
                    ObjectiveManager.Instance.Clear();

                Bus<AllObjectivesCompletedEvent>.Raise(Owner.Player, new AllObjectivesCompletedEvent());
                return;
            }

            SequenceStepSO step = sequence.Steps[index];
            _step = step;
            Debug.Log($"[{nameof(SequenceManager)}] 스텝 진입 #{index + 1} {step.Title}", this);
            if (step.Effects != null)
            {
                foreach (SequenceStepEffectSO effect in step.Effects)
                {
                    if (effect == null)
                        continue;

                    Awaitable running = effect.ExecuteAsync();
                    if (running != null)
                        await running;
                }
            }

            bool guided = step.Gestures != null && step.Gestures.Length > 0;

            // HUD를 가르치는 스텝은 테두리가 글과 함께 떠야 어느 칸을 말하는지 안다.
            // 손이 짚는 스텝만 대사가 끝나기를 기다린다 — 읽는 동안 화면이 가려져 손을 놓친다
            if (!guided)
                ShowHighlights(step);

            dialogUI.Show(step.DialogLines, () =>
            {
                if (guided)
                {
                    // 손이 짚는 스텝은 테두리를 겹치지 않는다 — 카메라만 옮긴다
                    if (gameObjHighlighter != null)
                        gameObjHighlighter.Focus(step.GameObjectHint);

                    if (uiHighlighter != null)
                        uiHighlighter.Show(step.UIElementHint, DimsScreen(step));
                }

                _guide.Play(step.Gestures, lockCameraOnFirstLoop: true);
                ObjectiveManager.Instance.Activate(step);
            });
        }

        /// <summary>
        /// 테두리를 손이 짚는 칸으로 옮깁니다. 월드를 짚는 차례면 스텝이 지정한 칸으로 돌아갑니다.
        /// 한 스텝에서 눌러야 할 칸이 둘이면(분대 → 지휘관) 테두리가 한 칸에만 머물러선 안 됩니다.
        /// 손은 차례대로 짚는데 테두리만 마지막 칸에 붙어 있으면 순서를 거꾸로 읽게 됩니다.
        /// </summary>
        private void HandleGuideUiFocus(UIElementHighlightType hint)
        {
            if (uiHighlighter == null)
                return;

            uiHighlighter.Show(hint != UIElementHighlightType.None
                ? hint
                : _step != null ? _step.UIElementHint : UIElementHighlightType.None, DimsScreen(_step));
        }

        private void ShowHighlights(SequenceStepSO step)
        {
            if (gameObjHighlighter != null)
                gameObjHighlighter.Show(step.GameObjectHint);

            if (uiHighlighter != null)
                uiHighlighter.Show(step.UIElementHint, DimsScreen(step));
        }

        // 월드도 눌러야 하는 스텝(채집·건설·생산)에서 화면을 가리면 방해다. HUD만 설명하는 스텝에서만 가린다
        private static bool DimsScreen(SequenceStepSO step)
            => step != null && step.GameObjectHint == GameObjectHighlightType.None;

        /// <summary>
        /// 승리하면 남은 중간 목표를 건너뛰고 마지막 스텝으로 넘어갑니다.
        /// 목표를 지나치고 적 본부부터 밀어도 마무리 대사와 결과 화면이 나와야 합니다.
        /// </summary>
        private void HandleGameOver(GameOverEvent evt)
        {
            if (evt.WinnerOwner != Owner.Player || sequence == null || sequence.Steps == null)
                return;

            int closing = sequence.Steps.Length - 1;
            if (_currentIndex >= closing)
                return;

            if (ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.Clear();

            _guide.Stop();
            _currentIndex = closing;
            _ = LoadStepAsync(closing);
        }

        private void HandleObjectiveCompleted(ObjectiveCompletedEvent evt)
            => Advance();

        /// <summary>
        /// 지금 스텝을 완료로 치고 다음으로 넘깁니다. 조건이 안 걸려 안내가 막혔을 때의 탈출구입니다.
        /// 진행 중인 목표를 먼저 걷어냅니다 — 남겨 두면 나중에 완료되어 스텝이 한 번 더 넘어갑니다.
        /// </summary>
        public void SkipCurrentStep()
        {
            if (!HasPendingSteps)
                return;

            if (ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.Clear();

            Advance();
        }

        private void Advance()
        {
            if (gameObjHighlighter != null)
                gameObjHighlighter.Clear();

            if (uiHighlighter != null)
                uiHighlighter.Clear();

            _guide.Stop();

            _currentIndex++;
            _ = LoadStepAsync(_currentIndex);
        }
    }
}
