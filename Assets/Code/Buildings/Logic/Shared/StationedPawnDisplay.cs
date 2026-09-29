using Code.Units.Pawns;
using UnityEngine;

namespace Code.Buildings.Logic.Shared
{
    /// <summary>
    /// 상주 일꾼을 건물 안에 숨기지 않고 화면에 세우는 건물에 붙입니다.
    /// 이 컴포넌트가 없는 건물은 지금까지처럼 일꾼을 안으로 들여보냅니다.
    /// </summary>
    public class StationedPawnDisplay : MonoBehaviour
    {
        [SerializeField] private Transform stationParent;   // 일꾼을 담을 곳. 건물의 정렬 그룹 안에 넣는다
        [SerializeField] private Transform[] stationSlots;  // 일꾼이 설 자리. 비면 건물 중심에 선다
        [SerializeField] private float toolIndex = 3f;      // 서 있을 때 손에 들 것. PawnIndex 3 = 칼

        /// <summary>
        /// 일꾼을 담을 자리입니다. 비어 있으면 원래 부모에 그대로 둡니다.
        /// </summary>
        public Transform Parent => stationParent;

        /// <summary>
        /// 일꾼이 설 자리와 손에 들 도구를 돌려줍니다.
        /// </summary>
        public void GetPose(Pawn pawn, PawnStationHost host, out Vector3 position, out float tool)
        {
            tool = toolIndex;
            position = transform.position;

            int index = host != null ? host.IndexOf(pawn) : -1;
            if (index < 0 || stationSlots == null || stationSlots.Length == 0)
                return;

            Transform slot = stationSlots[index % stationSlots.Length];
            if (slot != null)
                position = slot.position;
        }
    }
}
