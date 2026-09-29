using Code.Core.Input;
using Code.Stages;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.Players
{
    /// <summary>
    /// 카메라의 이동과 줌같은 카메라에 대한 기능을 담당하는 클래스 입니다.
    /// 실행 순서를 뒤로 미뤄, LateUpdate의 경계 보정이 Cinemachine Brain이 카메라를 옮긴 뒤에 돌게 합니다.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private CameraConfig cameraConfig;
        [SerializeField] private Rigidbody2D cameraTarget;
        [SerializeField] private CinemachineCamera cinemachineCamera;

        private CameraBoundsClamper _bounds;

        // 휠이 미는 목표 배율. 렌즈는 ApplyZoom이 매 프레임 여기로 따라붙는다.
        // 음수는 "아직 휠을 안 굴렸다" — 그동안은 씬에 설정된 배율을 건드리지 않는다
        private float _zoomTarget = -1f;

        /// <summary>
        /// 카메라 표적의 위치입니다. Rigidbody2D가 아니라 Transform을 직접 읽고 씁니다.
        /// 강체는 무엇을 쓰든 Transform이 다음 물리 스텝(50Hz)에야 따라와서, 매 프레임 그리는
        /// 화면이 그것을 쫓으면 이동이 계단으로 보입니다.
        /// 이 표적은 충돌할 것이 없어 Transform을 직접 옮겨도 안전합니다.
        /// </summary>
        private Vector2 TargetPosition
        {
            get => cameraTarget != null ? (Vector2)cameraTarget.transform.position : Vector2.zero;
            set
            {
                if (cameraTarget == null)
                    return;

                Transform target = cameraTarget.transform;
                target.position = new Vector3(value.x, value.y, target.position.z);
            }
        }

        /// <summary>
        /// 현재 카메라가 추적하는 월드 좌표입니다(시점 저장용).
        /// </summary>
        public Vector2 FocusPosition => TargetPosition;

        /// <summary>
        /// 카메라 줌(직교 크기)입니다. 설정 시 최소/최대 줌으로 클램프됩니다.
        /// </summary>
        public float ZoomSize
        {
            get => cinemachineCamera != null ? cinemachineCamera.Lens.OrthographicSize : 0f;
            set
            {
                if (cinemachineCamera == null)
                    return;

                float size = Mathf.Clamp(value, cameraConfig.MinZoomDistance, cameraConfig.MaxZoomDistance);

                LensSettings lens = cinemachineCamera.Lens;
                lens.OrthographicSize = size;
                cinemachineCamera.Lens = lens;

                // 목표 배율도 같이 옮긴다. 안 옮기면 휠을 한 번이라도 굴린 뒤에는 ApplyZoom이
                // 매 프레임 옛 목표로 렌즈를 끌고 가, 시퀀스 초점·시점 복원의 줌이 그대로 풀린다
                _zoomTarget = size;

                // 줌 아웃으로 화면이 넓어지면 현재 위치가 경계를 넘길 수 있어 즉시 되돌립니다.
                if (cameraTarget != null)
                    TargetPosition = _bounds.Clamp(TargetPosition);
            }
        }

        /// <summary>
        /// 카메라를 지정한 월드 좌표로 즉시 이동시킵니다(화면 전체가 맵 경계 안에 들어오도록 클램프).
        /// </summary>
        public void JumpTo(Vector2 worldPosition)
            => TargetPosition = _bounds.Clamp(worldPosition);

        /// <summary>
        /// 켜져 있는 동안 키보드·가장자리 이동과 줌을 받지 않습니다. 안내 손이 시연하는 동안 화면을 붙들어 둡니다.
        /// </summary>
        public bool InputLocked { get; set; }

        /// <summary>
        /// 켜져 있는 동안 화면 가장자리 이동만 받지 않습니다. 공세선을 끌다가 마우스가 화면 끝에 닿으면
        /// 카메라가 밀리면서 놓는 자리가 달아나므로 긋는 동안 잠급니다. 키보드 이동은 그대로입니다.
        /// </summary>
        public bool EdgePanSuppressed { get; set; }

        private void Awake()
            => _bounds = new CameraBoundsClamper(cinemachineCamera, this);

        private void OnDestroy()
            => _bounds?.Dispose();

        private void Update()
        {
            if (!InputLocked)
            {
                HandlePanning();
                HandleZoom();
            }

            ApplyZoom();
            _bounds.SnapIfOutOfBounds();
        }

        /// <summary>
        /// Cinemachine이 카메라를 옮긴 뒤 마지막으로 한 번 더 경계 안으로 밀어 넣습니다.
        /// Update의 보정은 지난 프레임 위치를 보고 판단하므로 줌·리사이즈 순간에 한 프레임이 새어 나갑니다.
        /// 렌더는 모든 LateUpdate 뒤에 일어나므로, 여기서 고치면 그 프레임부터 반영됩니다.
        /// </summary>
        private void LateUpdate()
        {
            Camera output = _bounds.OutputCamera;
            if (output == null)
                return;

            Vector3 position = output.transform.position;
            Vector2 clamped = _bounds.Clamp(position);
            output.transform.position = new Vector3(clamped.x, clamped.y, position.z);
        }

        private void HandlePanning()
        {
            Vector2 movement = inputReader.KeyboardMovement * cameraConfig.KeyboardPanSpeed;
            movement += GetMouseMoveAmount();

            // MovePosition은 다음 물리 스텝에야 반영돼, 매 프레임 그리는 화면에서 계단으로 보인다.
            // 카메라 표적은 충돌할 것이 없으므로 위치를 직접 옮긴다 — 부드러움은 Cinemachine 댐핑이 맡는다
            TargetPosition = _bounds.Clamp(TargetPosition + movement * Time.deltaTime);
        }

        private void HandleZoom()
        {
            if (cinemachineCamera == null)
                return;

            float scroll = inputReader.ConsumeScrollDelta();
            if (Mathf.Approximately(scroll, 0f))
                return;

            // 포인터가 UI(노드 캔버스 등) 위에 있으면 카메라 줌을 막습니다.
            // 휠은 이미 소비했으므로(지연 누적 방지) UI 위에선 해당 UI만 휠을 처리합니다.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // 휠은 목표 배율만 움직인다. 실제 렌즈는 ApplyZoom이 매 프레임 따라붙는다 —
            // 여기서 바로 대입하면 한 칸에 1.0씩 튀어 툭툭 끊긴다
            if (_zoomTarget < 0f)
                _zoomTarget = cinemachineCamera.Lens.OrthographicSize;

            _zoomTarget = Mathf.Clamp(_zoomTarget - Mathf.Sign(scroll) * cameraConfig.ZoomSpeed,
                cameraConfig.MinZoomDistance, cameraConfig.MaxZoomDistance);
        }

        /// <summary>
        /// 목표 배율로 렌즈를 미끄러뜨립니다. 프레임 시간에 무관한 지수 보간이라
        /// 프레임이 튀어도 같은 속도로 붙습니다.
        /// </summary>
        private void ApplyZoom()
        {
            if (cinemachineCamera == null || _zoomTarget < 0f)
                return;

            LensSettings lens = cinemachineCamera.Lens;
            if (Mathf.Approximately(lens.OrthographicSize, _zoomTarget))
                return;

            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, cameraConfig.ZoomSmoothing) * Time.unscaledDeltaTime);
            float size = Mathf.Lerp(lens.OrthographicSize, _zoomTarget, t);

            // 0.001 안쪽이면 붙여 버린다 — 지수 보간은 영원히 도착하지 않는다
            lens.OrthographicSize = Mathf.Abs(size - _zoomTarget) < 0.001f ? _zoomTarget : size;
            cinemachineCamera.Lens = lens;

            // 줌 아웃으로 허용 범위가 좁아졌으면 즉시 되돌립니다.
            TargetPosition = _bounds.Clamp(TargetPosition);
        }

        private Vector2 GetMouseMoveAmount()
        {
            Vector2 moveAmount = Vector2.zero;

            if (!cameraConfig.EnableEdgePan || EdgePanSuppressed)
                return moveAmount;

            Vector2 mousePosition = inputReader.MousePosition;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            if (mousePosition.x < cameraConfig.EdgePanSize)
                moveAmount.x -= cameraConfig.MousePanSpeed;
            else if (mousePosition.x > screenSize.x - cameraConfig.EdgePanSize)
                moveAmount.x += cameraConfig.MousePanSpeed;

            if (mousePosition.y < cameraConfig.EdgePanSize)
                moveAmount.y -= cameraConfig.MousePanSpeed;
            else if (mousePosition.y > screenSize.y - cameraConfig.EdgePanSize)
                moveAmount.y += cameraConfig.MousePanSpeed;

            return moveAmount;
        }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            StageSO stage = StageDatabaseSO.Current;
            if (stage == null)
                return;

            Vector2 min = stage.MapMin;
            Vector2 max = stage.MapMax;
            Vector3 center = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
            Vector3 size = new Vector3(max.x - min.x, max.y - min.y, 0f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, size);

            // 안쪽 사각형은 추적 좌표가 실제로 움직일 수 있는 범위입니다.
            // 화면 비율을 Screen에서 읽으므로 씬 뷰에서는 근사치이고, 플레이 중에 정확합니다.
            Vector2 extents = CameraBoundsClamper.ViewExtents(cinemachineCamera, Camera.main);
            Vector3 inner = size - new Vector3(extents.x * 2f, extents.y * 2f, 0f);
            if (inner.x <= 0f || inner.y <= 0f)
                return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(center, inner);
        }

#endif
    }
}
