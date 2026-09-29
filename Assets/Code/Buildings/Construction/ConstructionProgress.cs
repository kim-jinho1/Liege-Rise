using System;
using UnityEngine;

namespace Code.Buildings.Construction
{
    public enum BuildingState
    {
        Ghost,
        Constructing,
        Paused,
        Completed,
        Destroyed
    }

    [Serializable]
    public struct ConstructionProgress
    {
        [field: SerializeField] public float StartTime { get; private set; }
        [field: SerializeField] public float Progress { get; private set; }
        [field: SerializeField] public BuildingState State { get; private set; }

        public ConstructionProgress(BuildingState state, float startTime, float progress)
        {
            State = state;
            StartTime = startTime;
            Progress = progress;
        }
    }
}
