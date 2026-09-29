using System;
using UnityEngine;

namespace Code.Economics
{
    /// <summary>
    /// 특정 자원 타입과 수량을 묶은 구조체입니다.
    /// ResourceCostSO 비용 항목 및 UserSupplies 초기값 설정에 사용됩니다.
    /// </summary>
    [Serializable]
    public struct ResourceAmount
    {
        [field: SerializeField] public ResourceSO Resource { get; private set; }
        [field: SerializeField] public int Amount { get; private set; }
    }
}
