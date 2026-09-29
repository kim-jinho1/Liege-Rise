using UnityEngine;

namespace Code.Buildings.Construction
{
    /// <summary>
    /// 건물 배치 미리보기 고스트입니다. 배치 가능 여부를 색상으로 표시합니다.
    /// </summary>
    public class GhostPlacement : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color errorColor = new(1f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color availableColor = new(0.3f, 1f, 0.5f, 0.5f);

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private bool? _isPassRestriction;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 배치 가능 여부에 따라 고스트 색상을 갱신합니다.
        /// </summary>
        public void SetPassRestriction(bool isPass)
        {
            if (_isPassRestriction == isPass)
                return;

            _isPassRestriction = isPass;
            spriteRenderer.color = isPass ? availableColor : errorColor;
        }
    }
}
