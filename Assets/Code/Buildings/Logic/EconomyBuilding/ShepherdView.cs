using System;
using Code.Buildings.Logic.Shared;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Buildings.Logic.EconomyBuilding
{
    /// <summary>
    /// 우리에 상주한 일꾼을 움직입니다. 평소에는 제자리를 지키다가,
    /// 잡을 양이 정해지면 그 양에게 걸어가 칼을 쓰고 다시 제자리로 돌아옵니다.
    /// </summary>
    public class ShepherdView : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.1f;
        [SerializeField] private float butcherTime = 0.55f; // 칼을 쓰는 시간
        [SerializeField] private float reachDistance = 0.4f; // 양에 닿았다고 보는 거리
        [SerializeField] private float chaseTimeout = 4f; // 못 잡아도 이 시간이 지나면 포기하고 넘긴다
        [SerializeField] private float idleToolIndex = 3f; // 서거나 걸을 때 3 = 칼
        [SerializeField] private float workToolIndex = 2f; // 칼질할 때 2 = 칼. 블렌드 표가 서거나 걸을 때와 한 칸씩 어긋나 있다

        private PawnStationHost _host;
        private Pawn _tracked;
        private Vector3 _home;

        private Transform _prey;
        private Action _onCaught;
        private float _butcherTimer;
        private float _chaseTimer;
        private bool _returning;

        /// <summary>
        /// 일꾼이 양을 쫓거나 제자리로 돌아가는 중인지 여부입니다.
        /// </summary>
        public bool IsOnTheMove
            => _prey != null || _returning;

        /// <summary>
        /// 상주 중인 일꾼입니다. 양이 이 자리를 피해 다닙니다. 없으면 null입니다.
        /// </summary>
        public Transform ShepherdTransform
        {
            get
            {
                Pawn pawn = Shepherd();
                return pawn != null ? pawn.transform : null;
            }
        }

        /// <summary>
        /// 일꾼에게 양 한 마리를 잡으라고 보냅니다. 상주 일꾼이 없으면 false입니다.
        /// </summary>
        public bool TrySendTo(Transform prey, Action onCaught)
        {
            if (prey == null || Shepherd() == null)
                return false;

            _prey = prey;
            _onCaught = onCaught;
            _butcherTimer = 0f;
            _chaseTimer = chaseTimeout;
            _returning = false;

            return true;
        }

        private void Update()
        {
            Pawn pawn = Shepherd();
            if (pawn == null)
            {
                // 일꾼이 빠졌는데 잡기로 한 양이 남아 있으면 그대로 처리해 준다. 안 그러면 재고가 어긋난다
                if (_onCaught != null)
                    Finish();

                _tracked = null;
                return;
            }

            // 처음 본 자리가 제자리다. 건물이 정해 준 상주 위치를 그대로 쓴다
            if (_tracked != pawn)
            {
                _tracked = pawn;
                _home = pawn.transform.position;
            }

            if (_prey != null)
                Chase(pawn);
            else if (_returning)
                ReturnHome(pawn);
        }

        private void Chase(Pawn pawn)
        {
            if (_butcherTimer > 0f)
            {
                _butcherTimer -= Time.deltaTime;
                if (_butcherTimer <= 0f)
                {
                    pawn.SetStationPose(false, false, idleToolIndex);
                    Finish();
                }

                return;
            }

            _chaseTimer -= Time.deltaTime;
            if (_chaseTimer <= 0f)
            {
                Finish();
                return;
            }

            Vector3 preyPosition = _prey.position;
            if ((preyPosition - pawn.transform.position).sqrMagnitude <= reachDistance * reachDistance)
            {
                pawn.Agent.FaceTowards(preyPosition);
                pawn.SetStationPose(false, true, workToolIndex);
                _butcherTimer = butcherTime;
                return;
            }

            Step(pawn, preyPosition);
        }

        private void ReturnHome(Pawn pawn)
        {
            if (!Step(pawn, _home))
                return;

            _returning = false;
            pawn.SetStationPose(false, false, idleToolIndex);
        }

        /// <summary>
        /// 목적지로 한 걸음 옮기고, 도착했으면 true를 돌려줍니다.
        /// </summary>
        private bool Step(Pawn pawn, Vector3 destination)
        {
            Vector3 current = pawn.transform.position;
            Vector3 next = Vector3.MoveTowards(current, destination, moveSpeed * Time.deltaTime);
            next.z = current.z;

            pawn.transform.position = next;
            pawn.Agent.FaceTowards(destination);
            pawn.SetStationPose(true, false, idleToolIndex);

            return next == destination;
        }

        private void Finish()
        {
            Action callback = _onCaught;

            _prey = null;
            _onCaught = null;
            _butcherTimer = 0f;
            _returning = true; // 잡았으면 제자리로 돌아간다

            callback?.Invoke();
        }

        private Pawn Shepherd()
        {
            if (_host == null)
                _host = GetComponent<PawnStationHost>(); // 목장이 실행 중에 붙이므로 늦게 잡힌다

            return _host != null ? _host.Primary : null;
        }
    }
}
