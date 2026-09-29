using Code.Core.Bus;
using Code.Economics;
using Code.GameEvents.Economy;
using Code.GameEvents.System;
using Code.Progression;
using Code.Sequences;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Players
{
    /// <summary>
    /// 부스 전시용 탈출구입니다. 관람객이 막히거나 판이 꼬였을 때 진행자가 눌러 풀어 줍니다.
    /// 씬 배선이 없습니다. 스스로 붙어 씬을 넘나들며 살아남습니다 — 맵 씬마다 배선하면
    /// 하나를 빠뜨렸을 때 하필 그 판에서 안 먹습니다.
    /// </summary>
    public class BoothCheats : MonoBehaviour
    {
        private const int SupplyGrant = 500;   // 한 번에 자원마다 이만큼
        private const string ObjectName = "__BoothCheats";

        // 같은 조합을 이 시간 안에 한 번 더 눌러야 실행된다. 전투 중에는 Shift가 늘 눌려 있고
        // 옛 습관으로 Ctrl까지 짚을 수 있어, 한 번 눌린 것만으로 판을 끝내면 안 된다
        private const float ConfirmWindow = 2f;

        private Key _armedKey = Key.None;
        private float _armedUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            GameObject host = new(ObjectName);
            host.AddComponent<BoothCheats>();
            DontDestroyOnLoad(host);
        }

        // 조합은 Ctrl + Shift + F1~F4다. 관람객 손이 닿는 자리(숫자·문자·마우스)를 피하고,
        // 한 손으로는 누르기 어려운 조합을 골랐다 — 실수로 눌리면 전시 중에 판이 끝난다
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.ctrlKey.isPressed || !keyboard.shiftKey.isPressed)
                return;

            if (keyboard.f1Key.wasPressedThisFrame)
                Trigger(Key.F1, "자원 지급", GrantSupplies);
            else if (keyboard.f2Key.wasPressedThisFrame)
                Trigger(Key.F2, "강제 클리어", ForceClear);
            else if (keyboard.f3Key.wasPressedThisFrame)
                Trigger(Key.F3, "안내 단계 건너뛰기", SkipGuideStep);
            else if (keyboard.f4Key.wasPressedThisFrame)
                Trigger(Key.F4, "세이브 초기화", ResetSave);
        }

        /// <summary>
        /// 첫 번째 누름은 예약만 하고, 같은 조합을 창 안에 한 번 더 눌러야 실행합니다.
        /// 다른 치트를 누르면 예약이 그쪽으로 옮겨갑니다 — 잘못 짚었을 때 기다릴 필요가 없습니다.
        /// </summary>
        private void Trigger(Key key, string label, System.Action run)
        {
            if (_armedKey == key && Time.unscaledTime <= _armedUntil)
            {
                _armedKey = Key.None;
                run();
                return;
            }

            _armedKey = key;
            _armedUntil = Time.unscaledTime + ConfirmWindow;
            Notice($"치트: {label} — 한 번 더 누르면 실행");
        }

        /// <summary>
        /// 이 판이 세는 자원 전부를 지급합니다. 목록은 UserSupplies가 쥔 것을 그대로 씁니다 —
        /// 여기에 없는 자원은 아무리 보내도 무시되므로 따로 적어 두면 조용히 어긋납니다.
        /// </summary>
        private static void GrantSupplies()
        {
            UserSupplies supplies = UserSupplies.Instance;
            if (supplies == null)
            {
                Notice("치트: 자원 관리자가 없습니다");
                return;
            }

            foreach (ResourceSO resource in supplies.TrackedResources)
            {
                if (resource == null)
                    continue;

                Bus<SupplyEvent>.Raise(Owner.Player, new SupplyEvent(Owner.Player, SupplyGrant, resource));
            }

            Notice($"치트: 자원 +{SupplyGrant}");
        }

        /// <summary>
        /// 플레이어 승리로 판을 끝냅니다. 승패를 듣는 쪽이 전부 같은 이벤트를 보므로
        /// 결과 화면·클리어 기록·마무리 대사가 정상 승리와 똑같이 돕니다.
        /// </summary>
        private static void ForceClear()
        {
            Notice("치트: 강제 클리어");
            Bus<GameOverEvent>.Raise(Owner.Unowned, new GameOverEvent(Owner.Player));
        }

        /// <summary>
        /// 지금 안내 단계를 완료로 치고 다음으로 넘깁니다. 조건이 안 걸려 막혔을 때 씁니다.
        /// </summary>
        private static void SkipGuideStep()
        {
            SequenceManager sequence = SequenceManager.Instance;
            if (sequence == null || !sequence.HasPendingSteps)
            {
                Notice("치트: 넘길 안내 단계가 없습니다");
                return;
            }

            sequence.SkipCurrentStep();
            Notice("치트: 안내 단계 건너뜀");
        }

        /// <summary>
        /// 클리어 기록·최고 기록·해금을 전부 지웁니다. 다음 판부터 처음 상태로 보입니다.
        /// </summary>
        private static void ResetSave()
        {
            ProgressService.ResetAll();
            Notice("치트: 세이브 초기화 (메뉴로 나가면 반영)");
        }

        /// <summary>
        /// 콘솔에만 남깁니다. 배너로 띄우면 관람객 화면에 치트가 그대로 보입니다 —
        /// 진행자는 콘솔이나 화면 변화로 확인합니다.
        /// </summary>
        private static void Notice(string message)
            => Debug.Log($"[BoothCheats] {message}");
    }
}
