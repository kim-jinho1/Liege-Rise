using UnityEngine;
using UnityEngine.Rendering;

namespace Code.Rendering
{
    /// <summary>
    /// 스프라이트의 실제 바닥선(bounds.min.y)을 기준으로 SortingGroup의 정렬 순서를 갱신합니다.
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    [DisallowMultipleComponent]
    public class YSortByPosition : MonoBehaviour
    {
        /// <summary>
        /// 정렬 정밀도입니다. 1 월드 유닛을 이 정수 단계로 환산합니다.
        /// </summary>
        private const int Precision = 100;

        private SortingGroup _group;
        private SpriteRenderer _target;
        private Vector3 _lastPosition;

        private void Awake()
        {
            _group = GetComponent<SortingGroup>();
            _target = FindLargestRenderer();
        }

        private void Start()
        {
            _lastPosition = transform.position;
            Apply();
        }

        private void LateUpdate()
        {
            // 위치가 바뀐 프레임에만 갱신합니다. 정적 오브젝트는 사실상 Start에서 1회만 계산됩니다.
            Vector3 position = transform.position;
            if (position == _lastPosition)
                return;

            _lastPosition = position;
            Apply();
        }

        /// <summary>
        /// 현재 바닥선(bounds.min.y) 위치로 정렬 순서를 갱신합니다.
        /// </summary>
        private void Apply()
        {
            if (_target == null)
                return;

            _group.sortingOrder = Mathf.RoundToInt(-_target.bounds.min.y * Precision);
        }

        /// <summary>
        /// 정렬 기준이 될 본체 스프라이트를 찾습니다.
        /// 'Visual' 자식 → 루트 렌더러 → 가장 큰 렌더러 순으로 우선합니다.
        /// </summary>
        private SpriteRenderer FindLargestRenderer()
        {
            SpriteRenderer best = null;
            float bestHeight = float.NegativeInfinity;
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.gameObject.name == "Visual")
                    return sr;

                float height = sr.bounds.size.y;
                if (height > bestHeight)
                {
                    bestHeight = height;
                    best = sr;
                }
            }

            return TryGetComponent(out SpriteRenderer rootRenderer) ? rootRenderer : best;
        }
    }
}
