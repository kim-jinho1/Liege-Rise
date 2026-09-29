using System;
using System.Threading;
using UnityEngine;

namespace Code.Effects
{
    /// <summary>
    /// 건설이 끝난 건물이 위에서 내려앉으며 자리를 잡는 연출입니다.
    /// DeathShakeEffect를 뒤집은 것이라 두 연출의 인상이 짝을 이룹니다.
    /// 대상은 본체 스프라이트여야 합니다 — 루트를 움직이면 Y 정렬이 매 프레임 요동칩니다.
    /// </summary>
    public static class BuildCompleteEffect
    {
        private const float DefaultDuration = 0.34f;

        // 낙하가 끝나는 지점. 나머지는 착지 반동에 쓴다
        private const float DropPortion = 0.32f;

        // 전부 스프라이트 높이 대비 비율이라 건물 크기가 달라도 같은 인상을 준다.
        // 높이 띄우는 것은 '툭 내려앉는' 정도면 충분하다 — 크게 잡으면 하늘에서 떨어진다
        private const float RiseRatio = 0.12f;
        private const float StretchRatio = 0.07f;
        private const float SquashRatio = 0.2f;

        // 착지 후 남은 시간 동안 흔들릴 횟수. 키우면 젤리처럼 보인다
        private const float BounceFrequency = 7.5f;

        /// <summary>
        /// 대상을 위에서 떨어뜨려 납작해졌다가 원래 크기로 되돌립니다. 취소돼도 복원은 보장됩니다.
        /// </summary>
        public static async Awaitable SettleAsync(SpriteRenderer renderer, CancellationToken token,
            float duration = DefaultDuration)
        {
            if (renderer == null || duration <= 0f)
                return;

            Transform target = renderer.transform;
            Vector3 originPosition = target.localPosition;
            Vector3 originScale = target.localScale;

            float rise = renderer.bounds.size.y * RiseRatio;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    if (target == null)
                        return;

                    // squash가 양수면 납작, 음수면 늘어난 것이다
                    float height;
                    float squash;

                    if (t < DropPortion)
                    {
                        float k = t / DropPortion;
                        float eased = k * k; // 떨어질수록 빨라진다
                        height = rise * (1f - eased);
                        squash = -StretchRatio * eased;
                    }
                    else
                    {
                        float k = (t - DropPortion) / (1f - DropPortion);
                        height = 0f;
                        squash = SquashRatio * (1f - k) * Mathf.Cos(k * BounceFrequency);
                    }

                    target.localPosition = originPosition + new Vector3(0f, height, 0f);
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
                // 연출이 끊겨도 건물이 찌그러진 채로 남으면 안 된다
                if (target != null)
                {
                    target.localPosition = originPosition;
                    target.localScale = originScale;
                }
            }
        }
    }
}
