using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Components
{
    /// <summary>
    /// 호버 툴팁의 공통 동작(표시 지연·pivot·화면 클램핑 배치)을 제공하는 베이스입니다.
    /// 클램핑에 쓸 크기는 파생 클래스가 ContentSize로 제공합니다.
    /// </summary>
    public abstract class TooltipBase : MonoBehaviour
    {
        [field: SerializeField] public float HoverDelay { get; private set; } = 0.5f;

        private RectTransform _rectTransform;
        private object _owner;

        /// <summary>
        /// 툴팁이 커서를 가로채지 않도록 자식 그래픽의 레이캐스트를 모두 끕니다.
        /// 툴팁이 포인터를 먹으면 그 밑의 슬롯이 OnPointerExit를 받아 툴팁이 깜빡입니다.
        /// </summary>
        protected virtual void Awake()
        {
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        protected RectTransform Rect
            => _rectTransform != null ? _rectTransform : _rectTransform = GetComponent<RectTransform>();

        /// <summary>
        /// 화면 클램핑에 사용할 툴팁의 현재 크기입니다.
        /// </summary>
        protected abstract Vector2 ContentSize { get; }

        /// <summary>
        /// 툴팁의 pivot을 설정합니다. 앵커 지점 기준으로 펼쳐질 방향을 결정합니다.
        /// (1,0) = 앵커 좌측 상단 방향으로 펼쳐집니다.
        /// </summary>
        public void SetPivot(Vector2 pivot)
            => Rect.pivot = pivot;

        /// <summary>
        /// 스크린 좌표 기준으로 툴팁을 배치합니다. 화면 밖으로 벗어나지 않도록 클램핑합니다.
        /// 반드시 Show() 이후 활성 상태에서 호출해야 합니다.
        /// </summary>
        public void SetPosition(Vector2 screenPosition)
        {
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);

            Vector2 size = ContentSize;
            float width = size.x * Rect.lossyScale.x;
            float height = size.y * Rect.lossyScale.y;

            float leftEdge = width * Rect.pivot.x;
            float rightEdge = width * (1f - Rect.pivot.x);
            float bottomEdge = height * Rect.pivot.y;
            float topEdge = height * (1f - Rect.pivot.y);

            float clampedX = Mathf.Clamp(screenPosition.x, leftEdge, Screen.width - rightEdge);
            float clampedY = Mathf.Clamp(screenPosition.y, bottomEdge, Screen.height - topEdge);

            Rect.position = new Vector2(clampedX, clampedY);
        }

        /// <summary>
        /// 툴팁을 표시하고 소유자를 기록합니다. 이후 다른 소유자의 Hide는 무시됩니다.
        /// </summary>
        public virtual void Show(object owner)
        {
            _owner = owner;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 자기가 띄운 툴팁만 숨깁니다.
        /// 툴팁은 여러 슬롯이 공유하므로, 갱신으로 꺼지는 빈 슬롯이 남의 툴팁을 끄면 안 됩니다.
        /// </summary>
        public void Hide(object owner)
        {
            if (_owner == owner)
                ForceHide();
        }

        /// <summary>
        /// 소유자와 무관하게 툴팁을 숨깁니다. 패널을 통째로 닫을 때만 씁니다.
        /// </summary>
        public virtual void ForceHide()
        {
            _owner = null;
            gameObject.SetActive(false);
        }
    }
}
