using System;
using UnityEngine;

namespace Code.Players
{
    [Serializable]
    public class CameraConfig
    {
        [field: SerializeField] public bool EnableEdgePan { get; private set; } = true;
        [field: SerializeField] public float MousePanSpeed { get; private set; } = 5f;
        [field: SerializeField] public float EdgePanSize { get; private set; } = 50f; //픽셀
        [field: SerializeField] public float KeyboardPanSpeed { get; private set; } = 5f;
        [field: SerializeField] public float ZoomSpeed { get; private set; } = 1f;

        // 휠을 굴린 뒤 목표 배율로 따라붙는 빠르기. 클수록 즉시 반응하고, 작을수록 미끄러진다
        [field: SerializeField] public float ZoomSmoothing { get; private set; } = 12f;
        [field: SerializeField] public float RotationSpeed { get; private set; } = 1f;
        [field: SerializeField] public float MinZoomDistance { get; private set; } = 7.5f;
        [field: SerializeField] public float MaxZoomDistance { get; private set; } = 25f;

        // 맵 경계는 여기 없다. 카메라와 미니맵이 같은 숫자를 봐야 하므로 StageSO가 쥔다
    }
}
