using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using ObjectPool.RunTime;
using UnityEngine;

namespace Code.Units.Combat
{
    /// <summary>
    /// ProjectileLaunchEvent를 수신해 오브젝트 풀에서 발사체를 꺼내 초기화하는 씬 싱글톤입니다.
    /// 씬에 하나 배치하고 PoolManager를 연결해주세요입니다.
    /// </summary>
    public class ProjectileManager : MonoSingleton<ProjectileManager>
    {
        [SerializeField] private PoolManagerMono poolManager;

        private void OnEnable()
        {
            Bus<ProjectileLaunchEvent>.RegisterForAll(HandleProjectileLaunch);
        }

        private void OnDisable()
        {
            Bus<ProjectileLaunchEvent>.UnRegisterForAll(HandleProjectileLaunch);
        }

        private void HandleProjectileLaunch(ProjectileLaunchEvent evt)
        {
            if (evt.Item == null || evt.Target == null)
                return;

            Projectile p = poolManager.Pop<Projectile>(evt.Item);
            p.transform.SetPositionAndRotation(evt.StartPosition, Quaternion.identity);
            p.Init(evt.Target, evt.Speed, evt.Damage, evt.IsSplash, evt.SplashRadius, evt.DamageableLayers);
        }
    }
}
