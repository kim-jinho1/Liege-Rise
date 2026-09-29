using Code.Commandable;
using Code.Military.Data;
using Code.Rendering;
using TMPro;
using UnityEngine;

namespace Code.Military.Squads
{
    /// <summary>
    /// 분대·지휘관 위에 깃발을 띄웁니다. 분대 루트는 매 프레임 분대원 중심으로 옮겨지므로 자식으로 두면 따라옵니다.
    /// 기본은 선택했을 때만 보이고, 배지(지휘관 색·번호)가 붙으면 늘 보입니다 —
    /// 그래서 깃발 없는 분대가 곧 배속 안 된 분대입니다.
    /// </summary>
    public class SquadFlagView : MonoBehaviour
    {
        private const string ChildName = "__SquadFlag";
        private const string BadgeName = "__SquadFlagNumber";
        private static readonly Vector2 BadgeSize = new(3f, 2f);

        private AbstractCommandable _owner;
        private SquadMarkerStyleSO _style;
        private Transform _pivot;
        private SpriteRenderer _renderer;
        private TextMeshPro _badge;
        private bool _alwaysOn;
        private bool _badged;
        private int _shownNumber = -1;

        /// <summary>
        /// 깃발을 붙입니다. 스프라이트가 없으면 아무것도 붙이지 않고 null입니다.
        /// </summary>
        public static SquadFlagView Attach(AbstractCommandable owner, Sprite flag, bool alwaysOn = false)
        {
            if (owner == null || flag == null)
                return null;

            SquadFlagView view = owner.gameObject.AddComponent<SquadFlagView>();
            view._owner = owner;
            view._alwaysOn = alwaysOn;
            view.Build(flag);
            return view;
        }

        private void OnDestroy()
        {
            if (_owner != null)
                _owner.OnSelectionChanged -= HandleSelectionChanged;
        }

        /// <summary>
        /// 지휘관 색으로 물들이고 번호를 붙입니다. 붙은 동안 깃발은 늘 보입니다.
        /// </summary>
        public void SetBadge(Color color, int number)
        {
            _renderer.color = color;
            _badged = true;

            if (_badge == null)
                BuildBadge();

            if (number != _shownNumber)
            {
                _shownNumber = number;
                _badge.SetText("{0}", number);
            }

            Refresh();
        }

        public void ClearBadge()
        {
            _renderer.color = Color.white;
            _badged = false;
            Refresh();
        }

        private void Build(Sprite flag)
        {
            _style = SquadMarkerStyleSO.Load();

            GameObject child = new(ChildName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3(0f, _style.FlagHeightOffset, 0f);
            RenderLayers.PlaceOnMarkerLayer(child, gameObject);
            _pivot = child.transform;

            _renderer = child.AddComponent<SpriteRenderer>();
            _renderer.sprite = flag;
            _renderer.sortingLayerName = RenderLayers.EntitySorting;
            _renderer.sortingOrder = _style.FlagSortingOrder;

            _owner.OnSelectionChanged += HandleSelectionChanged;
            Refresh();
        }

        /// <summary>
        /// 번호는 처음 배지가 붙을 때만 만듭니다. 배속되지 않는 분대까지 TMP를 들고 있을 이유가 없습니다.
        /// </summary>
        private void BuildBadge()
        {
            GameObject child = new(BadgeName);
            child.transform.SetParent(_pivot, false);
            RenderLayers.PlaceOnMarkerLayer(child, gameObject);

            _badge = child.AddComponent<TextMeshPro>();
            _badge.rectTransform.sizeDelta = BadgeSize;
            _badge.alignment = TextAlignmentOptions.Center;
            _badge.fontSize = _style.FlagNumberSize;
            _badge.fontStyle = FontStyles.Bold;
            _badge.color = Color.white;
            _badge.sortingLayerID = SortingLayer.NameToID(RenderLayers.EntitySorting);
            _badge.sortingOrder = _style.FlagSortingOrder + 1;

            if (_style.FlagFont != null)
                _badge.font = _style.FlagFont;
        }

        private void HandleSelectionChanged(bool selected)
            => Refresh();

        private void Refresh()
        {
            bool selected = _owner.IsSelected;
            bool visible = _alwaysOn || _badged || selected;

            _renderer.enabled = visible;
            if (_badge != null)
                _badge.enabled = visible && _badged;

            // 선택한 지휘관 밑의 분대는 한 단계 커져 누구 것인지 드러난다
            float scale = _badged && selected ? _style.SelectedFlagScale : 1f;
            _pivot.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
