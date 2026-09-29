using System;
using Code.Bootstraps;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.System;
using UnityEngine;

namespace Code.Stages
{
    /// <summary>
    /// 지금 열려 있는 맵 범위를 쥐고, 구역이 열릴 때 MapRegionOpenedEvent를 발행합니다.
    /// StageSO의 값은 런타임에 못 바꾸고 Bootstrap은 리스너를 세션당 한 번만 부르므로,
    /// 막마다 변하는 경계는 스테이지 정의가 아니라 여기 둡니다.
    /// </summary>
    public class MapRegionManager : MonoSingleton<MapRegionManager>, IGameStartListener
    {
        /// <summary>
        /// 경계를 읽는 카메라·미니맵보다 먼저 서야 합니다.
        /// </summary>
        public int InitOrder => -50;

        /// <summary>
        /// 지금 열려 있는 범위의 최소 좌표입니다.
        /// </summary>
        public Vector2 Min { get; private set; }

        /// <summary>
        /// 지금 열려 있는 범위의 최대 좌표입니다.
        /// </summary>
        public Vector2 Max { get; private set; }

        /// <summary>
        /// 지금까지 열린 마지막 구역의 인덱스입니다. 아직 잡히지 않았으면 -1입니다.
        /// </summary>
        public int OpenedIndex { get; private set; } = -1;

        /// <summary>
        /// 마지막으로 열린 띠의 한가운데입니다. 여기가 이번에 새로 보이게 된 땅입니다.
        /// </summary>
        public Vector2 LastOpenedCenter { get; private set; }

        private MapRegion[] _regions = Array.Empty<MapRegion>();
        private bool _hasBounds;

        /// <summary>
        /// 지금 열려 있는 범위를 읽습니다. 매니저가 씬에 없거나 스테이지 정의를 못 찾았으면 false입니다.
        /// 읽는 쪽은 false일 때 각자의 폴백으로 돌아가면 됩니다.
        /// </summary>
        public static bool TryGetCurrentBounds(out Vector2 min, out Vector2 max)
        {
            MapRegionManager manager = Instance;
            if (manager == null || !manager._hasBounds)
            {
                min = Vector2.zero;
                max = Vector2.zero;
                return false;
            }

            min = manager.Min;
            max = manager.Max;
            return true;
        }

        /// <summary>
        /// 이번 판의 첫 구역을 엽니다. 구역을 쓰지 않는 스테이지는 맵 전체가 처음부터 열립니다.
        /// </summary>
        public void OnGameStart()
        {
            OpenedIndex = -1;
            _hasBounds = false;
            _regions = Array.Empty<MapRegion>();

            StageSO stage = StageDatabaseSO.Current;
            if (stage == null)
            {
                Debug.LogWarning($"[{name}] 이번 씬의 StageSO를 찾지 못해 구역을 잡지 못했습니다. " +
                                 "Resources/StageDatabase에 이 씬이 등록돼 있는지 확인하세요.", this);
                return;
            }

            _regions = stage.MapRegions ?? Array.Empty<MapRegion>();
            _hasBounds = true;

            if (_regions.Length == 0)
            {
                Min = stage.MapMin;
                Max = stage.MapMax;
                LastOpenedCenter = (Min + Max) * 0.5f;
                RaiseOpened();
                return;
            }

            Min = _regions[0].Min;
            Max = _regions[0].Max;
            OpenedIndex = 0;
            LastOpenedCenter = (Min + Max) * 0.5f;
            RaiseOpened();
        }

        /// <summary>
        /// 0번부터 지정 인덱스까지의 구역을 모두 엽니다.
        /// 합집합이라 두 번 불려도, 순서가 어긋나도 결과가 같습니다.
        /// </summary>
        public void OpenThrough(int index)
        {
            if (!_hasBounds || _regions.Length == 0)
                return;

            int target = Mathf.Clamp(index, 0, _regions.Length - 1);
            if (target <= OpenedIndex)
                return;

            Vector2 previousMin = Min;
            Vector2 previousMax = Max;

            for (int i = OpenedIndex + 1; i <= target; i++)
            {
                Min = Vector2.Min(Min, _regions[i].Min);
                Max = Vector2.Max(Max, _regions[i].Max);
            }

            OpenedIndex = target;
            LastOpenedCenter = ResolveNewStripCenter(previousMin, previousMax);
            RaiseOpened();
        }

        /// <summary>
        /// 이번에 새로 보이게 된 띠의 한가운데를 고릅니다.
        /// 구역은 위로만 자라지 않으므로(Stage1은 남쪽 → 동쪽 순이다) 네 방향을 모두 재고
        /// 가장 넓게 자란 띠를 고릅니다. 자란 곳이 없으면 열린 범위 전체의 한가운데입니다.
        /// </summary>
        private Vector2 ResolveNewStripCenter(Vector2 previousMin, Vector2 previousMax)
        {
            Vector2 center = (Min + Max) * 0.5f;
            float widest = 0f;

            TryStrip(new Vector2(Min.x, previousMax.y), Max, ref widest, ref center);
            TryStrip(Min, new Vector2(Max.x, previousMin.y), ref widest, ref center);
            TryStrip(new Vector2(previousMax.x, Min.y), Max, ref widest, ref center);
            TryStrip(Min, new Vector2(previousMin.x, Max.y), ref widest, ref center);

            return center;
        }

        /// <summary>
        /// 자란 띠가 지금까지 중 가장 넓으면 그 한가운데를 채택합니다.
        /// </summary>
        private static void TryStrip(Vector2 min, Vector2 max, ref float widest, ref Vector2 center)
        {
            float area = (max.x - min.x) * (max.y - min.y);
            if (area <= widest)
                return;

            widest = area;
            center = (min + max) * 0.5f;
        }

        /// <summary>
        /// 지금 열린 범위를 알립니다. 첫 구역도 알려야 카메라가 폴백 경계를 물고 굳지 않습니다.
        /// </summary>
        private void RaiseOpened()
            => Bus<MapRegionOpenedEvent>.Raise(Owner.Unowned, new MapRegionOpenedEvent(Min, Max, OpenedIndex));
    }
}
