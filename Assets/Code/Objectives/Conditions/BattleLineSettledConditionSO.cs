using System;
using System.Threading;
using Code.Core.Bus;
using Code.Military.Combat.FSM;
using Code.Military.Commanders;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 선을 받은 아군 지휘관의 분대가 모두 자리에 서면 완료됩니다. 선을 긋는 순간이 아니라 선 자리를 조건으로 두는 이유는
    /// 다음 스텝이 공세를 내보내는데 매복 분대가 아직 걸어가는 중이면 숨기 전에 들키기 때문입니다.
    /// 지휘관이 선을 스스로 지웠으면(전원이 섰고 적이 없다) 이미 끝난 것으로 봅니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_LineSettled", menuName = "Objectives/Conditions/BattleLineSettled")]
    public class BattleLineSettledConditionSO : ObjectiveConditionSO
    {
        public override ConditionTracker CreateTracker()
            => new BattleLineSettledTracker();
    }

    public class BattleLineSettledTracker : ConditionTracker
    {
        private const float PollInterval = 0.5f;

        private CancellationTokenSource _polling;

        // 도착 이벤트가 없어 분대 상태를 주기적으로 본다
        public override void Subscribe()
        {
            Unsubscribe();
            _polling = new CancellationTokenSource();
            PollAsync(_polling.Token);
        }

        public override void Unsubscribe()
        {
            _polling?.Cancel();
            _polling?.Dispose();
            _polling = null;
        }

        private async void PollAsync(CancellationToken token)
        {
            try
            {
                // 지휘관은 선을 받고 한 틱(0.4초) 뒤에야 분대를 세운다. 그 전에 보면 아직 Idle이라 바로 통과한다
                await Awaitable.WaitForSecondsAsync(PollInterval * 2f, token);
                while (!token.IsCancellationRequested)
                {
                    if (AllSettled())
                    {
                        Complete();
                        return;
                    }

                    await Awaitable.WaitForSecondsAsync(PollInterval, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        // 걷는 중(Moving·Advancing·Approaching)이 아니면 선 자리다. 붙어 싸우는 분대는 어차피 더 안 움직인다
        private static bool AllSettled()
        {
            foreach (Commander commander in Commander.AliveCommanders)
            {
                if (commander == null || commander.IsDead || commander.Owner != Owner.Player || !commander.HasLine)
                    continue;

                foreach (Squad squad in commander.Roster.Squads)
                {
                    if (squad == null || squad.IsDead)
                        continue;

                    SquadStateType state = squad.State;
                    if (state == SquadStateType.Moving || state == SquadStateType.Advancing || state == SquadStateType.Approaching)
                        return false;
                }
            }

            return true;
        }
    }
}
