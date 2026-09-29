using Code.Core.Bus;
using Code.GameEvents.System;
using Code.Rendering;
using ObjectPool.RunTime;
using UnityEngine;

namespace Code.FogSystem
{
    /// <summary>
    /// 시야에서 벗어난 적 유닛/건물의 마지막 목격 위치에 표시되는 잔상 오브젝트입니다.
    /// 오브젝트 풀링으로 관리됩니다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlaceHolder : MonoBehaviour, IHideable, IPoolable
    {
        public Transform Transform
            => this == null ? null : transform;

        public bool IsVisible { get; private set; }
        public event IHideable.VisibilityChangedEvent OnVisibilityChanged;

        private Pool _myPool;
        private SpriteRenderer _spriteRenderer;

        [field: SerializeField] public PoolingItemSO PoolType { get; private set; }
        public GameObject GameObject => this == null ? null : gameObject;

        public Owner Owner { get; set; }

        /// <summary>
        /// 잔상의 원본 오브젝트입니다. 원본이 파괴되면 잔상도 제거됩니다.
        /// </summary>
        public GameObject ParentObject { get; set; }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 풀에서 갓 꺼낸 잔상을 세우고 시야 판정 대상으로 등록되도록 알립니다.
        /// </summary>
        public void SetTransformAndSprite(Transform trm, SpriteRenderer sourceRenderer)
        {
            Capture(trm, sourceRenderer);
            Bus<PlaceHolderSpawnEvent>.Raise(Owner, new PlaceHolderSpawnEvent(this));
        }

        /// <summary>
        /// 원본 오브젝트의 위치, 회전, 스케일, 스프라이트, 색을 베낍니다.
        /// 이미 떠 있는 잔상을 다시 숨을 때 갱신하는 경로라 등록 이벤트는 내지 않습니다.
        /// </summary>
        public void Capture(Transform trm, SpriteRenderer sourceRenderer)
        {
            // 루트가 아니라 렌더러 자신의 좌표를 쓴다 — Visual 자식이 오프셋을 가지면 잔상이 밀린다.
            Transform sourceTrm = sourceRenderer != null ? sourceRenderer.transform : trm;

            transform.SetPositionAndRotation(sourceTrm.position, sourceTrm.rotation);
            transform.localScale = sourceTrm.lossyScale;

            _spriteRenderer.sprite = sourceRenderer.sprite;
            _spriteRenderer.flipX = sourceRenderer.flipX;
            _spriteRenderer.flipY = sourceRenderer.flipY;

            // 마지막으로 본 색을 그대로 베낀다. 유닛·건물은 흰색이라 달라지는 것이 없고,
            // 옅어지는 중이던 잔해만 그 진하기 그대로 멈춘다 — 임의로 흰색을 넣으면 다시 진해진다
            _spriteRenderer.color = sourceRenderer.color;
            SortingCopier.Copy(_spriteRenderer, sourceRenderer);
        }

        /// <summary>
        /// 시야에 다시 들어왔는데 원본이 파괴됐으면 잔상을 풀에 반환합니다.
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            bool changed = isVisible != IsVisible;
            IsVisible = isVisible;

            if (changed)
                OnVisibilityChanged?.Invoke(this, isVisible);

            // 파괴된 GameObject는 is null이 잡지 못한다 — Unity의 == 오버로드를 써야 한다.
            if (IsVisible && ParentObject == null)
            {
                Bus<PlaceHolderDestroyEvent>.Raise(Owner, new PlaceHolderDestroyEvent(this));
                _myPool.Push(this);
            }
        }

        public void SetUpPool(Pool pool)
        {
            _myPool = pool;
        }

        /// <summary>
        /// 풀에서 다시 꺼내기 전 상태를 비웁니다.
        /// 비워 두면 직전 상태를 들고 나와 팝하자마자 반환되어 잔상이 순간 사라집니다.
        /// </summary>
        public void ResetItem()
        {
            IsVisible = false;
            ParentObject = null;
            Owner = Owner.Invalid;
            OnVisibilityChanged = null;
        }
    }
}
