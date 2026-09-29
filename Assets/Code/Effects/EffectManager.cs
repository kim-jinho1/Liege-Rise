using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using GondrLib.Effects;
using ObjectPool.RunTime;
using UnityEngine;

namespace Code.Effects
{
    /// <summary>
    /// PoolEffectPlayEvent를 수신해 오브젝트 풀에서 2D 스프라이트 이펙트를 꺼내 재생하는 씬 싱글톤입니다.
    /// </summary>
    public class EffectManager : MonoSingleton<EffectManager>
    {
        [SerializeField] private PoolManagerMono poolManager;

        private void OnEnable()
        {
            Bus<PoolEffectPlayEvent>.RegisterForAll(HandleEffectPlay);
        }

        private void OnDisable()
        {
            Bus<PoolEffectPlayEvent>.UnRegisterForAll(HandleEffectPlay);
        }

        /// <summary>
        /// 이벤트로 지정된 이펙트를 풀에서 꺼내 해당 위치/회전에서 재생합니다.
        /// </summary>
        private void HandleEffectPlay(PoolEffectPlayEvent evt)
        {
            if (evt.ImpactEffect == null || poolManager == null)
                return;

            PoolingEffect effect = poolManager.Pop<PoolingEffect>(evt.ImpactEffect);
            if (effect != null)
                effect.PlayVFX(evt.ImpactPos, evt.Identity);
        }

        /// <summary>
        /// 풀에서 이펙트를 꺼내 핸들을 반환합니다. 자동 반환되지 않으므로
        /// 호출자가 PlayLooping/StopGracefully로 직접 제어해야 합니다.
        /// </summary>
        public PoolingEffect Acquire(PoolingItemSO item)
        {
            if (item == null || poolManager == null)
                return null;

            return poolManager.Pop<PoolingEffect>(item);
        }
    }
}
