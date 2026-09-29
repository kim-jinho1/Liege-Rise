using System;
using DG.Tweening;
using UnityEngine;

namespace Code.Effects
{
    /// <summary>
    /// 유닛이 나타날 때 작은 상태에서 제 크기로 튀어나오게 하고, 건물로 들어갈 때 그 반대로 줄입니다.
    /// </summary>
    public static class SpawnScaleEffect
    {
        // 크기는 본체가 아니라 그림에만 건다. 루트를 키우면 콜라이더와 센서 반경까지 함께 흔들린다
        private const string VisualName = "Visual";

        private const float InSeconds = 0.22f;
        private const float OutSeconds = 0.18f;
        private const float SmallScale = 0.15f;

        /// <summary>
        /// 작은 상태에서 제 크기로 커집니다.
        /// </summary>
        public static void PlayIn(GameObject owner)
        {
            Transform visual = FindVisual(owner);
            if (visual == null)
                return;

            visual.DOKill();
            visual.localScale = Vector3.one * SmallScale;
            visual.DOScale(Vector3.one, InSeconds)
                .SetEase(Ease.OutBack)
                .SetLink(owner, LinkBehaviour.KillOnDestroy);
        }

        /// <summary>
        /// 제 크기에서 작아진 뒤 onComplete를 부릅니다. 숨기는 일은 호출자가 그때 합니다 —
        /// 먼저 숨기면 줄어드는 모습이 한 프레임도 보이지 않습니다.
        /// </summary>
        public static void PlayOut(GameObject owner, Action onComplete)
        {
            Transform visual = FindVisual(owner);
            if (visual == null)
            {
                onComplete?.Invoke();
                return;
            }

            visual.DOKill();
            visual.DOScale(Vector3.one * SmallScale, OutSeconds)
                .SetEase(Ease.InBack)
                // 트윈이 시작되기 전에 폰이 징집·사망으로 파괴되면 대상이 사라진다.
                // 링크가 없으면 DOTween이 매번 경고를 뱉고, OnComplete가 파괴된 Transform을 만진다
                .SetLink(owner, LinkBehaviour.KillOnDestroy)
                .OnComplete(() =>
                {
                    visual.localScale = Vector3.one; // 다시 나올 때를 위해 되돌려 둔다
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// 그림을 담은 자식입니다. 이름으로 못 찾으면 첫 스프라이트 렌더러를 씁니다.
        /// </summary>
        private static Transform FindVisual(GameObject owner)
        {
            if (owner == null)
                return null;

            Transform named = owner.transform.Find(VisualName);
            if (named != null)
                return named;

            foreach (SpriteRenderer renderer in owner.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.transform != owner.transform)
                    return renderer.transform;

            return null;
        }
    }
}
