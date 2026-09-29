using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Buildings.Interfaces;
using Code.Buildings.Logic.Shared;
using Code.Effects;
using UnityEngine;

namespace Code.Buildings.Logic.EconomyBuilding
{
    /// <summary>
    /// 전초 건물입니다.
    /// 전초는 분대원 보충 지점으로 사용됩니다.
    /// </summary>
    public class OutpostBuilding : AbstractBuilding, IReinforcePoint
    {
        private OutpostBuildingSO OutpostSO
            => BuildingSO as OutpostBuildingSO;

        /// <summary>
        /// 분대원 1기를 보충하는 데 걸리는 시간(초)입니다.
        /// </summary>
        public float ReinforceInterval
            => OutpostSO != null ? OutpostSO.ReinforceInterval : 0f;

        /// <summary>
        /// 보충 범위 사각형의 한 변 길이입니다.
        /// </summary>
        public float ReinforceSize
            => OutpostSO != null ? OutpostSO.Range : 0f;

        /// <summary>
        /// 전초를 레지스트리에 등록합니다.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            OutpostRegistry.Register(this);
            ReinforcePointRegistry.Register(this);
            ReinforceRangeView.Attach(this);
        }

        /// <summary>
        /// 전초를 레지스트리에서 제거합니다.
        /// </summary>
        protected override void OnDestroy()
        {
            OutpostRegistry.Unregister(this);
            ReinforcePointRegistry.Unregister(this);
            base.OnDestroy();
        }

        /// <summary>
        /// 지정 위치가 보충 범위(사각형) 안에 있는지 확인합니다.
        /// </summary>
        public bool Covers(Vector3 position)
        {
            // 파괴된 전초는 잔해가 남아 있어도 보충하지 않는다
            if (IsDead || OutpostSO == null)
                return false;

            float half = OutpostSO.Range * 0.5f;
            Vector3 delta = position - transform.position;
            return Mathf.Abs(delta.x) <= half && Mathf.Abs(delta.y) <= half;
        }

#if UNITY_EDITOR
        // 범위 정식 시각화는 5단계 — 3단계는 기즈모로 확인
        private void OnDrawGizmosSelected()
        {
            if (OutpostSO == null)
                return;

            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(OutpostSO.Range, OutpostSO.Range, 0f));
        }
#endif
    }
}
