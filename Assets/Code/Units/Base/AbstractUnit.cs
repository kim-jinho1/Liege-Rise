using System;
using System.Collections.Generic;
using Code.Animations;
using Code.Commandable;
using Code.Commandable.Interfaces;
using Code.Core.Bus;
using Code.Effects;
using Code.GameEvents.Unit;
using Code.Navigation;
using Code.Units.BT;
using Code.Units.Interfaces;
using Unity.Behavior;
using Unity.Behavior.GraphFramework;
using UnityEngine;

namespace Code.Units.Base
{
    public abstract class AbstractUnit : AbstractCommandable, IMoveable, ISlowAffectable
    {
        [SerializeField] private VariableSO[] btVariables;
        [SerializeField] private ParameterSO dieParam;
        [SerializeField] private ParameterSO indexParam;
        [SerializeField] private int clipCount = 1;

        public BehaviorGraphAgent GraphAgent { get; private set; }
        public PathAgent Agent { get; private set; }
        public float AgentRadius => Agent.Radius;
        public UnitAnimator UnitAnimator { get; private set; }
        public AbstractUnitSO UnitSO { get; private set; }
        public float SeparationRadius { get; set; } // 밀어내기 반경입니다.

        public ParameterSO IndexParam
            => indexParam;

        public int ClipCount
            => clipCount;

        private Dictionary<BTVariables, SerializableGUID> _variablesDict;

        private float _baseSpeed;

        protected override void Awake()
        {
            base.Awake();
            GraphAgent = GetComponent<BehaviorGraphAgent>();
            Agent = GetComponent<PathAgent>();
            UnitAnimator = GetComponentInChildren<UnitAnimator>();
            UnitSO = AbstractCommandableSO as AbstractUnitSO;

            SeparationRadius = UnitSO != null ? UnitSO.SeparationRadius : 0.4f;

            Debug.Assert(Agent != null && UnitAnimator != null, "Unit must have a PathAgent and UnitAnimator component");

            Agent.UpdateRotation = true;
            _baseSpeed = Agent.Speed;

            BindUnitBlackboardVariable();
        }

        protected override void Start()
        {
            base.Start();
            _variablesDict = new Dictionary<BTVariables, SerializableGUID>();
            if (GraphAgent != null)
            {
                foreach (VariableSO variable in btVariables)
                {
                    if (!GraphAgent.GetVariableID(variable.VariableName.ToString(), out var guid))
                    {
                        Debug.LogError($"Missing required variable for unit: {variable.VariableName}");
                        continue;
                    }

                    _variablesDict.Add(variable.VariableName, guid);
                }
            }

            BindUnitBlackboardVariable();

            SpawnScaleEffect.PlayIn(gameObject);
            RaiseSpawnEvent();
        }

        private void BindUnitBlackboardVariable()
        {
            if (GraphAgent != null)
                GraphAgent.SetVariableValue("Unit", this);
        }

        public override async void Die()
        {
            if (!InitDeath())
                return; // _isDead 플래그 설정, 콜라이더 비활성화, 선택 해제

            try
            {
                if (GraphAgent != null) // Unity 오브젝트는 != null로만 판정한다
                    GraphAgent.enabled = false;

                Agent.ResetPath();

                RaiseDeathEvent();

                if (UnitAnimator != null)
                {
                    bool animDone = false;
                    UnitAnimator.OnAnimationEnd += () =>
                    {
                        animDone = true;
                        HideRenderers();
                    };

                    UnitAnimator.SetParameter(dieParam);

                    float deadline = Time.time + 5f;
                    while (!animDone && Time.time < deadline)
                        await Awaitable.NextFrameAsync();
                }
            }
            catch (Exception e)
            {
                // 메시지만 남기면 어느 줄이 던졌는지 알 수 없다 — 스택과 대상까지 같이 남긴다.
                Debug.LogException(e, this);
            }
            finally
            {
                // 사망 연출이 예외로 끊겨도 오브젝트는 반드시 걷어낸다.
                if (this != null)
                    Destroy(gameObject);
            }
        }

        /// <summary>
        /// 사망 연출 없이 유닛을 즉시 제거합니다. 사망 이벤트는 정상 발행되어
        /// 레지스트리 정리와 인구 건물의 재생성 쿨타임이 그대로 동작합니다. (예: 징집 소모)
        /// </summary>
        public void DespawnSilently()
        {
            if (!InitDeath())
                return;

            // BT 비활성화 시 실행 중이던 액션의 OnEnd가 렌더러를 복원할 수 있으므로 숨김은 그 뒤에 수행
            if (GraphAgent != null)
                GraphAgent.enabled = false;

            Agent.ResetPath();
            RaiseDeathEvent();
            HideRenderers();
            Destroy(gameObject);
        }

        /// <summary>
        /// 유닛을 지정된 월드 좌표로 이동하도록 명령합니다.
        /// </summary>
        public override void MoveTo(Vector3 position)
        {
            SetVariableValue(BTVariables.TargetLocation, position);
            SetVariableValue(BTVariables.Command, UnitCommands.Move);
        }

        /// <summary>
        /// 유닛을 지정된 Transform의 위치로 이동하도록 명령합니다.
        /// </summary>
        public void MoveTo(Transform targetTrm)
        {
            if (GraphAgent == null)
                return;

            SetVariableValue(BTVariables.TargetGameObject, targetTrm.gameObject);
            SetVariableValue(BTVariables.Command, UnitCommands.Move);
        }

        /// <summary>
        /// 유닛의 현재 행동을 중지하도록 명령합니다.
        /// </summary>
        public void Stop()
        {
            SetVariableValue(BTVariables.TargetLocation, transform.position);
            SetVariableValue(BTVariables.Command, UnitCommands.Stop);
            SetCommandOverrides(null);
        }

        /// <summary>
        /// 스폰 이벤트를 발행합니다.
        /// </summary>
        protected virtual void RaiseSpawnEvent()
        {
            Bus<UnitSpawnEvent>.Raise(Owner, new UnitSpawnEvent(Owner, this));
        }

        /// <summary>
        /// 사망 이벤트를 발행합니다.
        /// </summary>
        protected virtual void RaiseDeathEvent()
        {
            Bus<UnitDeathEvent>.Raise(Owner, new UnitDeathEvent(this));
        }

        /// <summary>
        /// BT 블랙보드 변수에 값을 설정합니다. Command 변수 변경 시 상태 텍스트도 갱신됩니다.
        /// </summary>
        public void SetVariableValue<T>(BTVariables variableName, T value)
        {
            if (GraphAgent == null || _variablesDict == null)
                return; // Awake~Start 사이엔 _variablesDict가 아직 생성되지 않음

            if (_variablesDict.TryGetValue(variableName, out SerializableGUID guid))
            {
                GraphAgent.SetVariableValue(guid, value);
                return;
            }

            Debug.LogError($"Variable not found: {variableName} in unit: {gameObject.name}");
        }

        /// <summary>
        /// BT 블랙보드 변수를 가져옵니다.
        /// </summary>
        public bool GetVariable<T>(BTVariables variableName, out BlackboardVariable<T> variable)
        {
            if (GraphAgent == null)
            {
                variable = null;
                return false;
            }

            if (_variablesDict.TryGetValue(variableName, out SerializableGUID guid))
                return GraphAgent.GetVariable(guid, out variable);

            Debug.LogError($"Variable not found: {variableName} in unit: {gameObject.name}");
            variable = null;
            return false;
        }

        /// <summary>
        /// 프리팹 속도에 배율을 곱해 이동속도를 정합니다. 분대 대형·지휘관 패시브가 쓰는 통로입니다.
        /// </summary>
        // 감속(ApplySlow)과 달리 배율은 스탯 시트가 합산해 넘겨주므로 여러 출처가 서로를 덮지 않는다
        public void ApplySpeedMultiplier(float multiplier)
        {
            Agent.Speed = _baseSpeed * Mathf.Max(0.1f, multiplier);
        }

        /// <summary>
        /// 이동속도를 slowPercent 비율만큼 감속합니다.
        /// </summary>
        public void ApplySlow(float slowPercent)
        {
            Agent.Speed = _baseSpeed * (1f - Mathf.Clamp01(slowPercent));
        }

        /// <summary>
        /// 이동속도를 원래 값으로 복구합니다.
        /// </summary>
        public void ResetSpeed()
        {
            Agent.Speed = _baseSpeed;
        }
    }
}
