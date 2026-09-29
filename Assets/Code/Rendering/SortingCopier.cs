using UnityEngine;
using UnityEngine.Rendering;

namespace Code.Rendering
{
    /// <summary>
    /// 원본 스프라이트가 화면에서 실제로 그려지는 조건을 다른 오브젝트에 옮깁니다.
    /// 정렬 레이어·순서와 함께 GameObject 레이어까지 옮깁니다 —
    /// 카메라 컬링 마스크에서 빠진 레이어에 놓이면 정렬을 아무리 맞춰도 화면에 나오지 않습니다.
    /// </summary>
    public static class SortingCopier
    {
        /// <summary>
        /// 원본이 실제로 그려지는 정렬 레이어·순서와 GameObject 레이어를 대상에 복사합니다.
        /// </summary>
        public static void Copy(SpriteRenderer target, SpriteRenderer source)
        {
            if (target == null || source == null)
                return;

            CopyGameObjectLayer(target.gameObject, source);

            // SortingGroup이 있으면 렌더러 자신의 값은 그룹 안에서만 쓰이는 죽은 값이다
            SortingGroup group = source.GetComponentInParent<SortingGroup>();
            if (group != null)
            {
                target.sortingLayerID = group.sortingLayerID;
                target.sortingOrder = group.sortingOrder;
                return;
            }

            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder;
        }

        /// <summary>
        /// 대상이 스스로 Y 정렬하도록 부품을 붙이고 레이어를 원본에서 물려받습니다.
        /// 원본이 사라진 뒤에도 남는 오브젝트용입니다.
        /// </summary>
        public static void MakeSelfSorting(GameObject target, SpriteRenderer source)
        {
            if (target == null)
                return;

            CopyGameObjectLayer(target, source);

            SortingGroup group = target.AddComponent<SortingGroup>();
            group.sortingLayerID = ResolveSortingLayer(source, group.sortingLayerID);

            target.AddComponent<YSortByPosition>();
        }

        private static void CopyGameObjectLayer(GameObject target, SpriteRenderer source)
        {
            if (target == null || source == null)
                return;

            target.layer = source.gameObject.layer;
        }

        /// <summary>
        /// 원본이 실제로 그려지는 정렬 레이어입니다. 원본이 없으면 fallback을 그대로 씁니다.
        /// </summary>
        private static int ResolveSortingLayer(SpriteRenderer source, int fallback)
        {
            if (source == null)
                return fallback;

            SortingGroup group = source.GetComponentInParent<SortingGroup>();
            return group != null ? group.sortingLayerID : source.sortingLayerID;
        }
    }
}
