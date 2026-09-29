using Code.Players;
using Code.Stages;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Sequences.Effects
{
    /// <summary>
    /// 맵 구역을 열어 카메라와 미니맵이 비출 범위를 넓히는 효과입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Eff_OpenRegion", menuName = "Sequence/Effects/OpenMapRegion")]
    public class OpenMapRegionEffectSO : SequenceStepEffectSO
    {
        [BoxGroup("구역 개방"), LabelText("여기까지 연다"), MinValue(0)]
        [field: SerializeField] public int RegionIndex { get; private set; } = 1;

        [BoxGroup("구역 개방"), LabelText("새로 열린 곳을 비춘다")]
        [field: SerializeField] public bool FocusOpenedArea { get; private set; } = true;

        public override void Execute()
        {
            MapRegionManager manager = MapRegionManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning($"[{name}] MapRegionManager를 찾지 못해 구역을 열지 못했습니다. _Core에 있는지 확인하세요.");
                return;
            }

            manager.OpenThrough(RegionIndex);

            if (!FocusOpenedArea)
                return;

            // EnemyBase 하이라이트는 씬에 GoblinCore 하나뿐이라 늘 같은 곳을 가리킨다.
            // 새로 열린 띠는 구역만 알고 있으므로 여기서 직접 비춘다
            CameraController camera = FindAnyObjectByType<CameraController>();
            if (camera != null)
                camera.JumpTo(manager.LastOpenedCenter);
        }
    }
}
