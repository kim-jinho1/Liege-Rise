using System;
using Code.Buildings.Construction;
using Code.Units.Base;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Code.Units.BT.Events
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Behavior/Event Channels/Construction event channel")]
#endif
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "Construction event channel", message: "[Unit] [ConstructionEventType] on [ConstructionDummy]", category: "Events", id: "f022ffe78d12144da6a138a99beb4d8f")]
    public sealed partial class ConstructionEventChannel : EventChannel<AbstractUnit, ConstructionEventType, ConstructionDummy> { }
}
