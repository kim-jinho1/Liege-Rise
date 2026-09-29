using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Sequences.Guide
{
    /// <summary>
    /// 안내 손의 키캡 부분입니다. 방향키 넷과 WASD는 십자로 펼쳐 보여 줍니다.
    /// </summary>
    public sealed partial class GuideCursorView
    {
        // 십자 배치: 가운데(아래 키)와 위·왼·오른쪽. "Arrows"·"WASD" 라벨이 이 묶음을 부른다
        private static readonly Vector2[] ExtraOffsets = { new(0f, 56f), new(-56f, 0f), new(56f, 0f) };
        private static readonly string[] ArrowCluster = { "Down", "Up", "Left", "Right" };
        private static readonly string[] WasdCluster = { "S", "W", "A", "D" };

        private RectTransform[] _extraCaps;
        private Image[] _extraArrows;
        private TextMeshProUGUI[] _extraTexts;

        /// <summary>
        /// 키캡을 띄웁니다. Up·Down·Left·Right는 글자 대신 삼각형을 돌려 보여 줍니다.
        /// </summary>
        public void ShowKey(string label, bool pressed)
        {
            // 키캡은 손 옆이 아니라 화면 한가운데. 손은 화면 끝에 가 있어 그 옆은 눈에 안 띈다
            Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
            float scale = pressed ? KeyScale * 0.85f : KeyScale;

            string[] cluster = label == "Arrows" ? ArrowCluster : label == "WASD" ? WasdCluster : null;
            SetKeyFace(_keyArrow, _keyText, cluster != null ? cluster[0] : label);
            _keycap.anchoredPosition = center;
            _keycap.localScale = Vector3.one * scale;
            _keycap.gameObject.SetActive(true);

            if (cluster != null && _extraCaps == null)
                BuildExtraCaps();

            if (_extraCaps == null)
                return;

            for (int i = 0; i < _extraCaps.Length; i++)
            {
                if (cluster != null)
                    SetKeyFace(_extraArrows[i], _extraTexts[i], cluster[i + 1]);

                _extraCaps[i].anchoredPosition = center + ExtraOffsets[i] * KeyScale;
                _extraCaps[i].localScale = Vector3.one * scale;
                _extraCaps[i].gameObject.SetActive(cluster != null);
            }
        }

        private void BuildExtraCaps()
        {
            _extraCaps = new RectTransform[ExtraOffsets.Length];
            _extraArrows = new Image[ExtraOffsets.Length];
            _extraTexts = new TextMeshProUGUI[ExtraOffsets.Length];
            for (int i = 0; i < _extraCaps.Length; i++)
            {
                RectTransform cap = MakeImage("Keycap", _root, GuideCursorArt.RoundedBox(48, 8), new Vector2(48f, 48f), new Vector2(0.5f, 0.5f), BodyColor).rectTransform;
                _extraTexts[i] = MakeKeyText(cap);
                _extraArrows[i] = MakeImage("Arrow", cap, GuideCursorArt.ArrowRight(32), new Vector2(28f, 28f), new Vector2(0.5f, 0.5f), KeyColor);
                CenterInParent(_extraArrows[i].rectTransform);
                _extraCaps[i] = cap;
            }
        }

        private static void SetKeyFace(Image arrow, TextMeshProUGUI text, string label)
        {
            bool isArrow = TryArrowAngle(label, out float angle);
            arrow.gameObject.SetActive(isArrow);
            if (isArrow)
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (text == null)
                return;

            text.gameObject.SetActive(!isArrow);
            if (!isArrow)
                text.SetText(label);
        }

        private static bool TryArrowAngle(string label, out float angle)
        {
            angle = 0f;
            switch (label)
            {
                case "Right": return true;
                case "Up": angle = 90f; return true;
                case "Left": angle = 180f; return true;
                case "Down": angle = 270f; return true;
                default: return false;
            }
        }

        public void HideKey()
        {
            _keycap.gameObject.SetActive(false);
            if (_extraCaps == null)
                return;

            foreach (RectTransform cap in _extraCaps)
                cap.gameObject.SetActive(false);
        }
    }
}
