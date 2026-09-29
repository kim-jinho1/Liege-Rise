using System;
using Code.Core.Input;
using Code.Sequences;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.UI.Sequence
{
    /// <summary>
    /// 시퀀스 다이얼로그 패널입니다. 스피커 이름, 아이콘, 본문을 표시하며
    /// 패널 클릭 또는 Space로 다음 줄로 넘기고, 모든 줄을 읽으면 onComplete를 호출합니다.
    /// 화면 아무 데나 누르는 클릭으로는 넘기지 않습니다 — 유닛을 고르고 명령하는 클릭에 한 줄짜리 대사가
    /// 읽기도 전에 닫혀서, "옵니다" 경고를 아무도 못 보고 적이 불쑥 나타났습니다.
    /// </summary>
    public class SequenceDialogUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Dialog")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private Image speakerIconImage;
        [SerializeField] private TextMeshProUGUI bodyText;

        [Header("Settings")]
        [SerializeField] protected InputReader inputReader;
        [SerializeField] private float typingSpeed = 0.03f;

        private DialogLine[] _lines;
        private int _lineIndex;
        private Action _onComplete;
        private bool _isTyping;
        private string _fullText;

        // 타이핑 루프의 세대 번호. 새 줄이 시작되면 올라가고, 지난 루프는 이것으로 자기 차례가
        // 끝난 것을 안다 — 플래그 하나로는 새 루프가 다시 켠 참을 지난 루프가 자기 것으로 읽는다
        private int _typeGeneration;

        private void Awake()
        {
            Debug.Assert(inputReader != null, "InputReader reference is missing in SequenceDialogUI.");
            inputReader.OnAdvancePressed += Advance;

            // 컨테이너의 레이아웃 그룹이 텍스트 폭을 "선호 폭"으로 주는데, 텍스트를 넣기 전에는 그게 0이라
            // 첫 글자부터 한 자씩 줄바꿈되어 세로로 쌓였다. 남는 폭을 다 받도록 해 두면 글자 수와 무관하게 가로로 흐른다
            FillWidth(speakerNameText);
            FillWidth(bodyText);
        }

        private static void FillWidth(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            LayoutElement element = text.GetComponent<LayoutElement>();
            if (element == null)
                element = text.gameObject.AddComponent<LayoutElement>();

            element.flexibleWidth = 1f;
        }

        private void OnDestroy()
        {
            if (inputReader != null)
                inputReader.OnAdvancePressed -= Advance;
        }

        /// <summary>
        /// 다이얼로그를 표시합니다. 모든 줄을 읽으면 onComplete가 호출됩니다.
        /// </summary>
        public void Show(DialogLine[] lines, Action onComplete)
        {
            if (lines == null || lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _isTyping = false;
            _lines = lines;
            _lineIndex = 0;
            _onComplete = onComplete;
            panel.SetActive(true);
            ShowLine(0);
        }

        // 패널(자식 Image)에 떨어진 클릭이 여기까지 올라온다. 캔버스 루트엔 그래픽이 없어 빈 화면 클릭은 안 온다
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                Advance();
        }

        /// <summary>
        /// 다이얼로그를 다음 줄로 넘깁니다. 타이핑 중이면 즉시 전체 텍스트를 표시합니다.
        /// </summary>
        private void Advance()
        {
            if(_lines == null || _lines.Length == 0)
                return;

            if (_isTyping)
            {
                _isTyping = false;
                bodyText.maxVisibleCharacters = int.MaxValue;
                return;
            }

            _lineIndex++;
            if (_lineIndex >= _lines.Length)
            {
                panel.SetActive(false);
                Action callback = _onComplete;
                _onComplete = null;
                _lines = null;
                callback?.Invoke();
                return;
            }

            ShowLine(_lineIndex);
        }

        private void ShowLine(int index)
        {
            DialogLine line = _lines[index];

            // UnityEngine.Object에 ?. 를 쓰면 파괴된 대상을 살아 있다고 보고 그대로 만진다
            if (speakerNameText != null)
                speakerNameText.text = line.speakerName;

            if (speakerIconImage != null)
            {
                bool hasIcon = line.speakerIcon != null;
                if (hasIcon)
                    speakerIconImage.sprite = line.speakerIcon;

                speakerIconImage.gameObject.SetActive(hasIcon);
            }

            _fullText = line.text;
            TypeTextAsync();
        }

        /// <summary>
        /// 글자를 한 자씩 드러냅니다. 텍스트는 처음부터 통째로 넣습니다 —
        /// 레이아웃 그룹이 텍스트의 선호 폭으로 칸을 잡는데, 텍스트를 한 자씩 바꾸면 폭이 늘 한 자 뒤처져
        /// 새 글자가 줄바꿈되어 세로로 쌓였습니다. 보이는 글자 수만 늘리면 폭이 처음부터 확정됩니다.
        /// </summary>
        private async void TypeTextAsync()
        {
            int generation = ++_typeGeneration;

            _isTyping = true;

            // SetText는 string.Format처럼 중괄호를 자리표시자로 읽는다. 대사는 사람이 쓴 글이라
            // 중괄호가 섞이면 그 자리가 통째로 깨진다 — 그대로 넣는 text를 쓴다
            bodyText.text = _fullText;
            bodyText.maxVisibleCharacters = 0;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel.transform as RectTransform);

            try
            {
                for (int i = 0; i < _fullText.Length; i++)
                {
                    // 내 세대가 지났으면 조용히 물러난다. 안 그러면 이전 줄의 루프가 새 줄의 글자 수를
                    // 자기 인덱스로 덮어써 글자가 늘었다 줄었다 한다
                    if (generation != _typeGeneration || !_isTyping || this == null)
                        return;

                    bodyText.maxVisibleCharacters = i + 1;
                    await WaitUnscaledAsync(typingSpeed);
                }
            }
            catch (OperationCanceledException)
            {
                // 판을 나가며 이 오브젝트가 파괴된 것이다. async void라 잡지 않으면 콘솔로 샌다
                return;
            }

            if (generation == _typeGeneration)
                _isTyping = false;
        }

        /// <summary>
        /// timeScale과 무관하게 기다립니다. 승리하면 시간이 멈추는데 마무리 대사는 그 위에서 흘러야 합니다.
        /// </summary>
        private async Awaitable WaitUnscaledAsync(float seconds)
        {
            for (float left = seconds; left > 0f; left -= Time.unscaledDeltaTime)
                await Awaitable.NextFrameAsync(destroyCancellationToken);
        }
    }
}
