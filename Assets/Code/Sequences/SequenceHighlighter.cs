using System.Collections.Generic;
using Code.Core;
using Code.Players;
using Code.Sequences.Guide;
using UnityEngine;

namespace Code.Sequences
{
    /// <summary>
    /// 스텝이 짚어 주는 월드 대상을 켜 둡니다. 대상은 HighlightTargetFinder가 종류로 찾으므로 프리팹에 태그가 없습니다.
    /// 스텝이 끝날 때까지 켜 두고, 죽거나 새로 생긴 대상은 주기적으로 다시 찾습니다.
    /// </summary>
    public class SequenceHighlighter : MonoSingleton<SequenceHighlighter>
    {
        // 짚은 대상으로 카메라를 옮긴다. 강조만 하고 화면 밖에 두면 처음 보는 사람은 무엇을 누르라는지 모른다
        [SerializeField] private bool focusCameraOnShow = true;

        [SerializeField, Min(0.1f)] private float refreshInterval = 1f; // 뽑힌 일꾼·분대가 이 안에 목록에 든다

        private readonly List<IGuideHighlightable> _active = new(16);
        private readonly List<IGuideHighlightable> _scratch = new(16);
        private GameObjectHighlightType _currentType = GameObjectHighlightType.None;
        private float _nextRefresh;

        /// <summary>
        /// 지정 종류를 켭니다. 이전 강조는 자동 해제됩니다.
        /// </summary>
        public void Show(GameObjectHighlightType type)
        {
            Clear();
            _currentType = type;
            if (type == GameObjectHighlightType.None)
                return;

            Refresh();

            if (focusCameraOnShow)
                FocusCamera(type);
        }

        /// <summary>
        /// 테두리 없이 카메라만 옮깁니다. 안내 손이 짚는 스텝은 테두리를 겹치지 않습니다.
        /// </summary>
        public void Focus(GameObjectHighlightType type)
        {
            Clear();
            if (type == GameObjectHighlightType.None || !focusCameraOnShow)
                return;

            FocusCamera(type);
        }

        private void Update()
        {
            if (_currentType == GameObjectHighlightType.None || Time.time < _nextRefresh)
                return;

            Refresh();
        }

        /// <summary>
        /// 대상을 다시 찾아 빠진 것은 끄고 새로 든 것은 켭니다.
        /// </summary>
        private void Refresh()
        {
            _nextRefresh = Time.time + refreshInterval;
            HighlightTargetFinder.Collect(_currentType, _scratch);

            foreach (IGuideHighlightable old in _active)
                if (!_scratch.Contains(old))
                    SetSafely(old, false);

            foreach (IGuideHighlightable fresh in _scratch)
                if (!_active.Contains(fresh))
                    fresh.SetGuideHighlight(true);

            _active.Clear();
            _active.AddRange(_scratch);
            _scratch.Clear();
        }

        /// <summary>
        /// 여럿이면 성에서 가장 가까운 것으로 옮깁니다.
        /// </summary>
        private void FocusCamera(GameObjectHighlightType type)
        {
            if (!TryGetNearestPosition(type, ReferencePoint(), out Vector3 position))
                return;

            CameraController camera = FindAnyObjectByType<CameraController>();
            if (camera != null)
                camera.JumpTo(position);
        }

        /// <summary>
        /// 종류의 대표 좌표(성에서 가장 가까운 것)입니다. 없으면 false.
        /// </summary>
        public bool TryGetPosition(GameObjectHighlightType key, out Vector3 position)
            => HighlightTargetFinder.TryGetNearest(key, ReferencePoint(), out position);

        /// <summary>
        /// 종류 중 reference에 가장 가까운 대상의 좌표입니다. 없으면 false.
        /// </summary>
        public bool TryGetNearestPosition(GameObjectHighlightType key, Vector3 reference, out Vector3 position)
            => HighlightTargetFinder.TryGetNearest(key, reference, out position);

        private static Vector3 ReferencePoint()
            => GuidePointFinder.TryGetPlayerCastle(out Vector3 castle) ? castle : Vector3.zero;

        /// <summary>
        /// 켜 둔 강조를 전부 끕니다.
        /// </summary>
        public void Clear()
        {
            foreach (IGuideHighlightable target in _active)
                SetSafely(target, false);

            _active.Clear();
            _currentType = GameObjectHighlightType.None;
        }

        /// <summary>
        /// 파괴된 Unity 오브젝트에 손대지 않고 끕니다. 인터페이스 참조라 == null 오버로드가 안 걸립니다.
        /// </summary>
        private static void SetSafely(IGuideHighlightable target, bool on)
        {
            if (target is Object unityObject && unityObject == null)
                return;

            target.SetGuideHighlight(on);
        }
    }
}
