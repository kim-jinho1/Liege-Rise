using System;
using UnityEngine;

namespace Code.Economics
{
    /// <summary>
    /// 유닛 생산·건물 건설에 필요한 자원 비용 정의입니다.
    /// 자원 종류 제한 없이 ResourceAmount 배열로 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Resource cost", menuName = "Units/Cost", order = 5)]
    public class ResourceCostSO : ScriptableObject
    {
        [field: SerializeField] public ResourceAmount[] Costs { get; private set; } = Array.Empty<ResourceAmount>();
    }
}
