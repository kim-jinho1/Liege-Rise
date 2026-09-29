using UnityEngine;

namespace Code.Units.BT
{
    public enum BTVariables
    {
        TargetLocation = 0,
        Command = 1,
        TargetGameObject = 2,
        SupplyAmountHeld = 3,
        SupplyType = 4,
        TargetCell = 5,
        ResourceFilter = 13,
        HeadQuarter = 6,
        SupplyEventChannel = 7,
        ConstructionDummySO = 8,
        ConstructBuildingSO = 9,
        ConstructionEventChannel = 10,
        AttackConfig = 11,
        FirePositionTrm = 12,
    }

    [CreateAssetMenu(fileName = "Variable data", menuName = "BT/Variable", order = 10)]
    public class VariableSO : ScriptableObject
    {
        [field: SerializeField] public BTVariables VariableName { get; set; }
    }
}
