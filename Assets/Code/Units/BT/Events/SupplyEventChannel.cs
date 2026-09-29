using System;
using Code.Economics;
using Code.Units.Base;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Code.Units.BT.Events
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Behavior/Event Channels/SupplyEventChannel")]
#endif
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "SupplyEventChannel", message: "[Unit] gather [Amount] [Supplies]", category: "Events", id: "f6a47e2f79f80b9b8eb3b5c6d16cc1ce")]
    public sealed partial class SupplyEventChannel : EventChannel<AbstractUnit, int, ResourceSO> { }
}
