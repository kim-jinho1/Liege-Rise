using System;
using UnityEngine;

namespace Code.Environments
{
    /// <summary>
    /// 자원 오브젝트의 겉모습을 맡습니다 — 선택용 콜라이더, 애니메이션, 고갈된 모습입니다.
    /// 시야 밖에서는 겉모습을 바꾸지 않고 미뤄 둡니다. 탐색 지역은 반투명하게 비치므로,
    /// 보지 않는 사이에 나무가 밑둥으로 바뀌면 마지막으로 본 모습과 어긋납니다.
    /// </summary>
    public sealed class ResourceVisuals
    {
        private readonly ResourceObjectSO _so;
        private readonly SpriteRenderer _spriteRenderer;
        private readonly Animator _animator;
        private readonly Action _retire; // 고갈 스프라이트가 없는 자원을 치우는 방법. 소유자가 정한다

        private BoxCollider2D _selectionCollider;
        private bool _depletePending;

        public ResourceVisuals(GameObject owner, SpriteRenderer spriteRenderer, Animator animator,
            ResourceObjectSO so, Action retire)
        {
            _spriteRenderer = spriteRenderer;
            _animator = animator;
            _so = so;
            _retire = retire;

            SetupSelectionCollider(owner);
        }

        /// <summary>
        /// 시야 상태를 반영합니다. 다시 보이는 순간 미뤄 둔 고갈 모습을 한꺼번에 적용합니다.
        /// </summary>
        public void SetVisible(bool isVisible, bool isDepleted)
        {
            if (!isVisible)
            {
                // 탐색 지역은 반투명하게 비치므로 애니메이션이 계속 돌면 살아 있는 것처럼 보인다.
                if (_animator != null)
                    _animator.enabled = false;

                return;
            }

            if (_depletePending)
            {
                ApplyDepleted();
                return;
            }

            if (_animator != null && !isDepleted)
                _animator.enabled = true;
        }

        /// <summary>
        /// 고갈된 모습으로 바꿉니다. 시야 밖이면 다시 보일 때까지 미룹니다.
        /// </summary>
        public void Deplete(bool isVisible)
        {
            if (!isVisible)
            {
                _depletePending = true;
                return;
            }

            ApplyDepleted();
        }

        private void ApplyDepleted()
        {
            _depletePending = false;

            if (_animator != null)
                _animator.enabled = false;

            if (_so.DepletedSprite == null)
            {
                _retire?.Invoke();
                return;
            }

            _spriteRenderer.sprite = _so.DepletedSprite;

            // 안 맞추면 밑둥 바깥 빈 공간에서도 커서에 잡힌다
            ResizeSelectionCollider();
        }

        /// <summary>
        /// 자원 오브젝트의 스프라이트에 맞춰 선택용 트리거 콜라이더를 답니다.
        /// </summary>
        private void SetupSelectionCollider(GameObject owner)
        {
            if (_spriteRenderer.sprite == null)
                return;

            Bounds spriteBounds = _spriteRenderer.sprite.bounds;
            // 고갈 시 밑둥 크기로 다시 맞춰야 하므로 참조를 들고 있는다.
            _selectionCollider = owner.AddComponent<BoxCollider2D>();
            _selectionCollider.isTrigger = true;
            _selectionCollider.size = spriteBounds.size;
            _selectionCollider.offset = spriteBounds.center;
        }

        private void ResizeSelectionCollider()
        {
            if (_selectionCollider == null || _spriteRenderer.sprite == null)
                return;

            Bounds bounds = _spriteRenderer.sprite.bounds;
            _selectionCollider.size = bounds.size;
            _selectionCollider.offset = bounds.center;
        }
    }
}
