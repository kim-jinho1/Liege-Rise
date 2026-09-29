using Code.Stages;
using UnityEngine;

namespace Code.UI
{
    /// <summary>
    /// 화면에 찍는 값의 표기를 모아 둔 곳입니다.
    /// 계산은 여기가 하고, 부르는 말은 UITextSO와 NoticeTextSO가 쥡니다.
    /// </summary>
    public static class TextFormat
    {
        /// <summary>
        /// 기록이 없을 때 쓰는 표기입니다.
        /// </summary>
        public static string NoRecord => UITextSO.Load().NoRecord;

        /// <summary>
        /// 잠긴 영지의 이름 대신 보여 주는 표기입니다.
        /// </summary>
        public static string HiddenStageName => UITextSO.Load().HiddenStageName;

        /// <summary>
        /// 자원이 모자라 명령을 받을 수 없을 때의 안내입니다.
        /// 건설·훈련 등 여러 커맨드가 같은 말을 쓰므로 각자 들고 있으면 두 벌이 됩니다.
        /// </summary>
        public static string NotEnoughSupplies => NoticeTextSO.Load().NotEnoughSupplies;

        /// <summary>
        /// 지급 안내에 들어갈 자원 한 가지입니다. "목재 +30" 꼴입니다.
        /// </summary>
        public static string SupplyItem(string resourceName, int amount)
            => $"{resourceName} +{amount}";

        /// <summary>
        /// 시퀀스가 자원을 쥐여 줬을 때의 안내입니다. HUD의 +150만으로는 어디서 온 것인지 알 수 없습니다.
        /// </summary>
        public static string SuppliesGranted(string items)
        {
            // 에셋이 이 칸을 갖기 전에 저장됐으면 빈 값이다. string.Format이 던지지 않게 막는다
            string format = NoticeTextSO.Load().SuppliesGrantedFormat;
            return string.IsNullOrEmpty(format) ? items : string.Format(format, items);
        }

        /// <summary>
        /// 초를 "28분 41초" 꼴로 적습니다. 음수는 기록 없음으로 봅니다.
        /// </summary>
        public static string Duration(float seconds)
        {
            UITextSO text = UITextSO.Load();
            if (seconds < 0f)
                return text.NoRecord;

            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return string.Format(text.DurationFormat, total / 60, total % 60);
        }

        /// <summary>
        /// 목표 진행도를 "24 / 30" 꼴로 적습니다.
        /// </summary>
        public static string ObjectiveProgress(int current, int target)
            => string.Format(UITextSO.Load().ObjectiveProgressFormat, current, target);

        /// <summary>
        /// 영지의 상태 줄에 찍는 라벨입니다.
        /// </summary>
        public static string StageState(StageStatus status)
        {
            UITextSO text = UITextSO.Load();
            return status switch
            {
                StageStatus.Cleared => text.StageCleared,
                StageStatus.Open => text.StageOpen,
                _ => text.StageLocked
            };
        }

        /// <summary>
        /// 출정 버튼에 찍는 라벨입니다.
        /// </summary>
        public static string StageLaunch(StageStatus status)
        {
            UITextSO text = UITextSO.Load();
            return status switch
            {
                StageStatus.Cleared => text.LaunchCleared,
                StageStatus.Open => text.LaunchOpen,
                _ => text.StageLocked
            };
        }
    }
}
