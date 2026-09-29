using Code.Buildings.Base;
using Code.Managers;
using UnityEngine;

namespace Code.Buildings.Logic.CoreBuilding
{
    /// <summary>
    /// 이 건물을 승패 판정 대상(본거지)으로 등록합니다.
    /// SpawnerBuilding과 HeadquartersBuilding이 둘 다 AbstractBuilding이라 한 오브젝트에 못 얹히므로,
    /// 등록만 떼어내 적 진지도 본거지가 될 수 있게 합니다.
    /// </summary>
    public class HeadquartersMarker : MonoBehaviour
    {
        /// <summary>
        /// 이 마커가 붙은 건물입니다.
        /// </summary>
        public AbstractBuilding Building { get; private set; }

        private bool _registered;

        private void Awake()
        {
            Building = GetComponent<AbstractBuilding>();
            if (Building == null)
                Debug.LogWarning($"[{name}] 같은 오브젝트에 건물이 없어 본거지로 등록하지 않습니다.", this);
        }

        private void Start()
        {
            if (Building == null || HeadquartersBuildingManager.Instance == null)
                return;

            HeadquartersBuildingManager.Instance.Register(Building.Owner);
            _registered = true;
            Building.OnDied += HandleDied;
        }

        /// <summary>
        /// 구독만 끊습니다. 여기서 해제하면 씬 언로드에도 승패가 발행돼 오발합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (Building != null)
                Building.OnDied -= HandleDied;
        }

        /// <summary>
        /// 붕괴 연출을 기다리지 않고 사망이 확정된 순간에 해제합니다.
        /// </summary>
        private void HandleDied()
        {
            if (!_registered)
                return;

            _registered = false;
            if (HeadquartersBuildingManager.Instance != null)
                HeadquartersBuildingManager.Instance.Unregister(Building.Owner, transform.position);
        }
    }
}
