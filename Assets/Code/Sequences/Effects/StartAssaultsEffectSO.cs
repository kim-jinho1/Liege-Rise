using Code.EnemyAI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Sequences.Effects
{
    /// <summary>
    /// 적 공세 시간표를 이 시점부터 돌리기 시작하는 효과입니다.
    /// 스테이지가 DeferAssaults로 미뤄 둔 압박을 시퀀스가 원하는 순간에 풉니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Eff_StartAssaults", menuName = "Sequence/Effects/StartAssaults")]
    public class StartAssaultsEffectSO : SequenceStepEffectSO
    {
        [BoxGroup("공세 개시"), LabelText("첫 웨이브까지(초). 음수면 시간표 그대로")]
        [field: SerializeField] public float FirstWaveDelay { get; private set; } = 60f;

        public override void Execute()
        {
            EnemyAIManager manager = EnemyAIManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning($"[{name}] EnemyAIManager를 찾지 못해 공세를 시작하지 못했습니다. _Core에 있는지 확인하세요.");
                return;
            }

            // 두 번 불려도 시계는 하나만 돈다
            manager.BeginAssaults(FirstWaveDelay);
        }
    }
}
