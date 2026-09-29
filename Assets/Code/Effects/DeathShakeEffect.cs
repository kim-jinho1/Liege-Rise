using System;
using System.Threading;
using UnityEngine;

namespace Code.Effects
{
    /// <summary>
    /// 파괴 직전 건물이 흔들리며 가라앉는 연출입니다.
    /// 대상은 본체 스프라이트여야 합니다 — 루트를 흔들면 Y 정렬이 매 프레임 요동칩니다.
    /// </summary>
    public static class DeathShakeEffect
    {
        private const float DefaultDuration = 0.32f;
        private const float ShakeStrength = 0.1f;

        // 스프라이트 높이 대비 비율. 건물 크기가 달라도 같은 인상을 준다.
        private const float SinkRatio = 0.28f;
        private const float SquashRatio = 0.22f;

        /// <summary>
        /// 대상을 무너뜨린 뒤 원래 위치와 크기로 되돌립니다. 취소돼도 복원은 보장됩니다.
        /// </summary>
        public static async Awaitable CollapseAsync(SpriteRenderer renderer, CancellationToken token,
            float duration = DefaultDuration)
        {
            if (renderer == null || duration <= 0f)
                return;

            Transform target = renderer.transform;
            Vector3 originPosition = target.localPosition;
            Vector3 originScale = target.localScale;

            float sink = renderer.bounds.size.y * SinkRatio;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    if (target == null)
                        return;

                    float drop = sink * t * t;

                    Vector2 jitter = UnityEngine.Random.insideUnitCircle * (ShakeStrength * (1f - t));

                    target.localPosition = originPosition + new Vector3(jitter.x, drop * -1f + jitter.y, 0f);

                    float squash = SquashRatio * t;
                    target.localScale = new Vector3(
                        originScale.x * (1f + squash * 0.5f),
                        originScale.y * (1f - squash),
                        originScale.z);

                    await Awaitable.NextFrameAsync(token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                // 잔해가 찌그러진 스케일을 물려받으면 안 된다.
                if (target != null)
                {
                    target.localPosition = originPosition;
                    target.localScale = originScale;
                }
            }
        }
    }
}
