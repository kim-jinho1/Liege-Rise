using System;
using Code.Commandable.Interfaces;
using Code.Commands.Base;
using Code.Core.Bus;
using Code.Core.FactionPalette;
using Code.FogSystem;
using Code.GameEvents.Combat;
using Code.GameEvents.System;
using Code.GameEvents.Unit;
using Code.Sequences;
using UnityEngine;

namespace Code.Commandable
{
    public abstract class AbstractCommandable : MonoBehaviour, ISelectable, IDamageable, IHideable, ICorrodable,
        IGuideHighlightable
    {
        [field: SerializeField] public Owner Owner { get; set; }
        [field: SerializeField] public BaseCommandSO[] AvailableCommands { get; private set; }
        [field: SerializeField] public AbstractCommandableSO AbstractCommandableSO { get; set; }

        [SerializeField] protected FactionPaletteSO factionPaletteSO;
        [SerializeField] protected SpriteRenderer minimapIcon;

        /// <summary>
        /// 시야 범위 비주얼 Transform. FoW Vision 레이어에 원형 스프라이트를 배치합니다.
        /// </summary>
        [SerializeField] protected Transform visionTrm;

        /// <summary>
        /// 주둔지로부터 출격 명령을 이미 받은 병력인지 여부입니다.
        /// </summary>
        public bool WasDispatched { get; set; }

        /// <summary>
        /// SpawnerBuilding에서 생산된 유닛인지 여부입니다. true인 유닛만 출격 대상이 됩니다.
        /// </summary>
        public bool IsSpawnerProduced { get; set; }

        public int CurrentHealth
        {
            get => _health.Current;
            protected set => _health.Current = value;
        }

        public int MaxHealth
        {
            get => _health.Max;
            protected set => _health.Max = value;
        }

        public bool IsDead { get; private set; }

        /// <summary>
        /// 적이 지금 이것을 목표로 삼아도 되는지 여부입니다. 살아 있으면 기본은 참입니다.
        /// </summary>
        public virtual bool IsTargetable => !IsDead;

        public Transform Transform
            => this == null ? null : transform;

        public bool IsSelected { get; private set; }
        public bool IsVisible { get; private set; } = true;

        /// <summary>
        /// 기본 커맨드가 아니라 교체된 커맨드 페이지(건설 메뉴 등)를 들고 있는지 여부입니다.
        /// </summary>
        public bool HasCommandOverrides { get; private set; }

        public delegate void HealthUpdatedEvent(AbstractCommandable commandable, int lastHealth, int newHealth);

        public event HealthUpdatedEvent OnHealthUpdated;
        public event Action<bool> OnSelectionChanged;

        // 선택 외곽선이 켜지고 꺼질 때. 분대원처럼 개별 선택되지 않는 유닛도 이건 받는다
        public event Action<bool> OnSelectionVisualChanged;
        public event IHideable.VisibilityChangedEvent OnVisibilityChanged;

        // 사망이 확정된 순간. 붕괴 연출과 OnDestroy보다 앞서므로 승패 판정이 늦지 않는다
        public event Action OnDied;

        private readonly CommandableHealth _health = new();

        private BaseCommandSO[] _initialCommands;
        private Collider2D _collider2D;
        private CommandableVisuals _visuals;
        private BusSubscriptionScope _subscriptionScope;

        protected virtual void Awake()
        {
            Debug.Assert(AbstractCommandableSO != null, $"UnitSO가 {gameObject.name}에 할당되지 않았습니다.");

            _initialCommands = AvailableCommands;
#if UNITY_EDITOR
            CommandArrayAudit.Report(gameObject, AvailableCommands);
#endif
            _collider2D = GetComponent<Collider2D>();
            _visuals = new CommandableVisuals(gameObject, minimapIcon, visionTrm,
                factionPaletteSO.GetSelectionColor(Owner), factionPaletteSO.HoverAlphaScale);

            // 체력 그릇은 값만 쥔다. 통지는 이 대상의 이름으로 나가야 구독자가 알아본다
            _health.Changed = (lastHealth, newHealth) => OnHealthUpdated?.Invoke(this, lastHealth, newHealth);
        }

        protected virtual void Start()
        {
            _health.Reset(AbstractCommandableSO.Health);

            _subscriptionScope = new BusSubscriptionScope();

            if (minimapIcon != null)
                minimapIcon.color = factionPaletteSO.GetMinimapColor(Owner);

            _visuals.InitializeVision(Owner == Owner.Player,
                AbstractCommandableSO.SightConfig != null ? AbstractCommandableSO.SightConfig.SightRadius : -1f);
        }

        protected virtual void OnDestroy()
        {
            // 종료 중에는 UI와 BT가 이미 헐려 있어 선택 해제가 죽은 객체를 건드린다
            if (IsSelected && !Core.ApplicationLifecycle.IsQuitting)
                DeSelect();

            _subscriptionScope?.Dispose();
        }

        public virtual void Select()
        {
            SetSelectionVisual(true);

            IsSelected = true;
            OnSelectionChanged?.Invoke(true);
            Bus<UnitSelectEvent>.Raise(Owner, new UnitSelectEvent(this));
        }

        public virtual void DeSelect()
        {
            SetSelectionVisual(false);

            IsSelected = false;
            OnSelectionChanged?.Invoke(false);
            SetCommandOverrides(null);
            Bus<UnitDeselectEvent>.Raise(Owner, new UnitDeselectEvent(this));
        }

        public void TakeDamage(int damage, Vector3 position, Vector3 normal)
        {
            if (IsDead)
                return;

            int lastHealth = CurrentHealth;
            ApplyDamage(damage, position, normal);
            OnHealthUpdated?.Invoke(this, lastHealth, CurrentHealth);
            _visuals.Flash();
            Bus<DamageTakenEvent>.Raise(Owner, new DamageTakenEvent(this, damage, transform.position));
            if (CurrentHealth <= 0)
                Die();
        }

        /// <summary>
        /// 지정한 월드 좌표로 이동하도록 명령합니다.
        /// 이동 수단이 없는 파생 클래스(건물 등)는 아무것도 하지 않습니다.
        /// </summary>
        public virtual void MoveTo(Vector3 position) { }

        /// <summary>
        /// 체력을 깎습니다. normal은 피격자에서 공격자를 향하는 단위 벡터이며,
        /// 방향이 없는 피해(지속 피해)는 Vector3.zero가 들어옵니다.
        /// </summary>
        protected virtual void ApplyDamage(int damage, Vector3 position, Vector3 normal)
            => _health.Subtract(damage);

        public void ApplyCorrosion(int damagePerTick, float tickInterval, float duration)
            => _visuals.ApplyCorrosion(this, damagePerTick, tickInterval, duration);

        public void Heal(int amount)
            => _health.Add(amount);

        /// <summary>
        /// 체력 표시 갱신을 알립니다. 스스로 피해를 받지 않는 파생 클래스(분대)가 씁니다.
        /// </summary>
        protected void RaiseHealthUpdated(int lastHealth, int newHealth)
            => OnHealthUpdated?.Invoke(this, lastHealth, newHealth);

        /// <summary>
        /// 최대 체력을 바꾸고 지금 비율을 유지합니다.
        /// </summary>
        protected virtual void RefreshMaxHealth(int newValue)
            => _health.SetMax(newValue);

        /// <summary>
        /// 선택 이벤트 없이 외곽선만 갱신합니다. 분대가 멤버 외곽선을 대신 켤 때 씁니다.
        /// </summary>
        public void SetSelectionVisual(bool selected)
        {
            _visuals.SetSelected(selected);
            OnSelectionVisualChanged?.Invoke(selected);
        }

        /// <summary>
        /// 커서가 올라와 있는 동안의 외곽선만 켭니다.
        /// OnSelectionVisualChanged는 쏘지 않습니다 — 쏘면 체력바가 호버만으로 따라 뜹니다.
        /// </summary>
        public virtual void SetHoveredVisual(bool hovered)
            => _visuals.SetHovered(hovered);

        public Vector3 GuidePosition => transform.position;
        public bool IsGuideAlive => !IsDead;

        /// <summary>
        /// 시퀀스가 짚어 주는 외곽선입니다. 선택·호버와 따로 기억되어 마우스가 스쳐도 꺼지지 않습니다.
        /// </summary>
        public virtual void SetGuideHighlight(bool on)
            => _visuals.SetFlagged(on);

        public virtual void Die()
        {
            if (!InitDeath())
                return;

            Destroy(gameObject);
        }

        protected bool InitDeath()
        {
            if (IsDead)
                return false;

            IsDead = true;
            OnDied?.Invoke();

            // Unity 오브젝트는 반드시 != null로 본다 — is not null은 패턴 매칭이라
            // 파괴된 오브젝트를 걸러내는 Unity의 == 오버로드를 우회한다(분대 전멸 시 MissingComponentException).
            if (_collider2D != null)
                _collider2D.enabled = false;

            if (IsSelected)
                DeSelect();

            return true;
        }

        public virtual void RefreshData(RefreshData evt)
        {
            if (!evt.Unit.Equals(AbstractCommandableSO))
                return;

            MaxHealth = AbstractCommandableSO.Health;
        }

        public void SetCommandOverrides(BaseCommandSO[] commands)
        {
            // null → 초기 커맨드 셋으로 복원
            // 빈 배열 → 커맨드 전체 잠금 (이동·공격 등 불가)
            HasCommandOverrides = commands != null;
            AvailableCommands = commands ?? _initialCommands;
            if (IsSelected)
                Bus<UnitSelectEvent>.Raise(Owner, new UnitSelectEvent(this));
        }

        /// <summary>
        /// 모든 스프라이트 렌더러를 비활성화합니다. 사망 연출 종료 후 본체를 숨기는 용도입니다.
        /// </summary>
        protected void HideRenderers()
            => _visuals.SetRenderersEnabled(false);

        /// <summary>
        /// FogVisibilityManager에 의해 호출됩니다.
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (isVisible == IsVisible)
                return;

            IsVisible = isVisible;
            OnVisibilityChanged?.Invoke(this, IsVisible);

            if (IsVisible)
                OnGainVisibility();
            else
                OnLoseVisibility();
        }

        /// <summary>
        /// 시야에 들어왔을 때 렌더러를 활성화합니다.
        /// </summary>
        protected virtual void OnGainVisibility()
            => _visuals.SetRenderersEnabled(true);

        /// <summary>
        /// 시야에서 벗어났을 때 렌더러를 비활성화합니다.
        /// </summary>
        protected virtual void OnLoseVisibility()
            => HideRenderers();
    }
}
