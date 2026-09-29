using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Buildings.Interfaces;
using Code.Buildings.Logic.Shared;
using Code.Effects;
using UnityEngine;

namespace Code.Buildings.Logic.CoreBuilding
{
    /// <summary>
    /// 기지 건물입니다. 승패 판정 등록은 같은 오브젝트의 HeadquartersMarker가 맡습니다.
    /// BuildingSO가 폰 공급 데이터를 가지고 있으면 본부도 직접 폰을 공급하며, 주변은 분대 보충 범위가 됩니다.
    /// </summary>
    [RequireComponent(typeof(HeadquartersMarker))]
    public class HeadquartersBuilding : AbstractBuilding, IReinforcePoint
    {
        private PawnSupplySource _supply;

        private HeadquartersBuildingSO HeadquartersSO
            => BuildingSO as HeadquartersBuildingSO;

        /// <summary>
        /// 분대원 1기를 보충하는 데 걸리는 시간(초)입니다.
        /// </summary>
        public float ReinforceInterval
            => HeadquartersSO != null ? HeadquartersSO.ReinforceInterval : 0f;

        /// <summary>
        /// 보충 범위 사각형의 한 변 길이입니다.
        /// </summary>
        public float ReinforceSize
            => HeadquartersSO != null ? HeadquartersSO.ReinforceRange : 0f;

        /// <summary>
        /// 현재 이 본부가 유지 중인 폰 수입니다. 공급 데이터가 없으면 0입니다.
        /// </summary>
        // 폰 수는 UserPopulation이 센다. 본부는 판 시작에 폰을 세워 둘 뿐 유지하지 않는다

        protected override void Awake()
        {
            base.Awake();

            // RequireComponent는 에디터가 스크립트를 붙일 때만 챙긴다. 프리팹 4개가 표식 없이 저장돼 있어
            // 빌드에서는 아무 본부도 등록되지 않고 승패가 영원히 안 난다 — 여기서 확실히 붙인다
            if (!TryGetComponent(out HeadquartersMarker _))
                gameObject.AddComponent<HeadquartersMarker>();
        }

        protected override void Start()
        {
            base.Start();
            ReinforcePointRegistry.Register(this);
            ReinforceRangeView.Attach(this);

            // PawnSO가 비어 있으면 Initialize가 아무것도 하지 않으므로 기존 본부는 동작이 바뀌지 않는다.
            if (BuildingSO is not IPawnSupplier supplier)
                return;

            _supply = PawnSupplySource.GetOrAdd(this);
            _supply.Initialize(this, supplier);
        }

        /// <summary>
        /// 목록에서 자신을 해제합니다.
        /// </summary>
        protected override void OnDestroy()
        {
            ReinforcePointRegistry.Unregister(this);
            base.OnDestroy();
        }

        /// <summary>
        /// 지정 위치가 보충 범위(사각형) 안에 있는지 확인합니다.
        /// </summary>
        public bool Covers(Vector3 position)
        {
            if (IsDead || HeadquartersSO == null || HeadquartersSO.ReinforceRange <= 0f)
                return false; // 파괴된 본부는 잔해가 남아 있어도 보충하지 않는다

            float half = HeadquartersSO.ReinforceRange * 0.5f;
            Vector3 delta = position - transform.position;
            return Mathf.Abs(delta.x) <= half && Mathf.Abs(delta.y) <= half;
        }
    }
}
