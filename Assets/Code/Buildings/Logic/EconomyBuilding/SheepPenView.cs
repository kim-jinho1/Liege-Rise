using System;
using Code.Audio;
using UnityEngine;

namespace Code.Buildings.Logic.EconomyBuilding
{
    /// <summary>
    /// 목장의 양 재고를 울타리 안 양 오브젝트로 보여 줍니다.
    /// 마릿수가 곧 화면이라, 재고가 마르면 빈 울타리가 그대로 보입니다.
    /// </summary>
    public class SheepPenView : MonoBehaviour
    {
        [SerializeField] private FarmBuilding farm;
        [SerializeField] private SheepView[] sheep;
        [SerializeField] private ShepherdView shepherd; // 없으면 양이 제자리에서 그냥 사라진다
        [SerializeField] private SpriteRenderer meatPop; // 도축 순간에만 뜨는 아이콘. 양과 같은 부모에 비활성으로 둔다
        [SerializeField] private Vector2 penSize = new(3f, 2f); // 양이 벗어나면 안 되는 우리 안쪽
        [SerializeField] private float meatRise = 0.4f;
        [SerializeField] private float meatDuration = 0.7f;

        private bool[] _alive;
        private int _pendingVictim = -1;
        private Vector3 _lastRemovedPosition;
        private int _popId;
        private bool _stopped;

        private void Awake()
        {
            if (farm == null)
                farm = GetComponentInParent<FarmBuilding>();

            if (farm == null)
            {
                Debug.LogWarning($"{name}: FarmBuilding을 찾지 못해 양 표시가 꺼집니다.", this);
                enabled = false;
                return;
            }

            _alive = new bool[sheep.Length];
            for (int i = 0; i < sheep.Length; i++)
            {
                _alive[i] = sheep[i] != null && sheep[i].gameObject.activeSelf;
                if (sheep[i] != null)
                    sheep[i].SetPenBounds(penSize * 0.5f);
            }

            farm.SheepCountChanged += HandleSheepCountChanged;
            farm.Slaughtered += HandleSlaughtered;
        }

        private void OnDestroy()
        {
            if (farm == null)
                return;

            farm.SheepCountChanged -= HandleSheepCountChanged;
            farm.Slaughtered -= HandleSlaughtered;
        }

        private void Update()
        {
            ScatterFromShepherd();

            // 무너지는 동안 양이 그 위를 계속 걸어 다니면 안 된다
            if (_stopped || !farm.IsDead)
                return;

            _stopped = true;
            foreach (SheepView view in sheep)
                if (view != null)
                    view.enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = sheep is { Length: > 0 } && sheep[0] != null ? sheep[0].transform.parent : transform;

            Gizmos.color = new Color(1f, 0.6f, 0.2f);
            Gizmos.DrawWireCube(origin != null ? origin.position : transform.position,
                new Vector3(penSize.x, penSize.y, 0f));
        }

        /// <summary>
        /// 지나가는 일꾼 곁에 있는 양을 비켜서게 합니다. 잡히는 중인 양은 그대로 둡니다.
        /// </summary>
        private void ScatterFromShepherd()
        {
            // 제자리에 선 일꾼은 밀지 않는다. 상주 자리가 우리 한가운데라
            // 매 프레임 밀면 양이 영영 울타리에 붙어 서서 나가려는 것처럼 보인다
            if (_stopped || shepherd == null || !shepherd.IsOnTheMove)
                return;

            Transform pawn = shepherd.ShepherdTransform;
            if (pawn == null)
                return;

            for (int i = 0; i < sheep.Length; i++)
                if (_alive[i] && sheep[i] != null && sheep[i].gameObject.activeSelf)
                    sheep[i].KeepAwayFrom(pawn.position);
        }

        private void HandleSheepCountChanged(int count)
        {
            count = Mathf.Clamp(count, 0, sheep.Length);

            // 잡히는 놈도 자라는 놈도 매번 다시 뽑는다. 인덱스 순으로 자르면 늘 같은 양만 죽는다
            while (AliveCount() > count && TryPick(true, out int victim))
                SetAlive(victim, false);

            while (AliveCount() < count && TryPick(false, out int newborn))
                SetAlive(newborn, true);
        }

        private int AliveCount()
        {
            int alive = 0;
            foreach (bool state in _alive)
                if (state)
                    alive++;

            return alive;
        }

        /// <summary>
        /// 지정한 상태인 양 중 하나를 무작위로 고릅니다.
        /// </summary>
        private bool TryPick(bool state, out int index)
        {
            int candidates = 0;
            for (int i = 0; i < _alive.Length; i++)
                if (_alive[i] == state && sheep[i] != null)
                    candidates++;

            index = -1;
            if (candidates == 0)
                return false;

            int pick = UnityEngine.Random.Range(0, candidates);
            for (int i = 0; i < _alive.Length; i++)
            {
                if (_alive[i] != state || sheep[i] == null)
                    continue;

                if (pick-- > 0)
                    continue;

                index = i;
                return true;
            }

            return false;
        }

        private void SetAlive(int index, bool alive)
        {
            _alive[index] = alive;

            if (alive)
            {
                sheep[index].Appear();
                return;
            }

            // 아직 사라지지 않는다. 일꾼이 쫓아가 잡는 순간까지 기다린다
            _pendingVictim = index;
        }

        private void HandleSlaughtered()
        {
            int victim = _pendingVictim;
            _pendingVictim = -1;

            if (victim < 0 || sheep[victim] == null)
                return;

            SheepView prey = sheep[victim];
            if (shepherd != null && shepherd.TrySendTo(prey.transform, () => Kill(victim)))
                return;

            Kill(victim);
        }

        /// <summary>
        /// 양을 실제로 치웁니다. 기다리는 사이에 다시 자란 자리면 건드리지 않습니다.
        /// </summary>
        private void Kill(int index)
        {
            if (_alive[index] || sheep[index] == null)
                return;

            _lastRemovedPosition = sheep[index].transform.localPosition;
            Vector3 worldPosition = sheep[index].transform.position;
            sheep[index].Vanish();

            EconomyAudio.PlaySheepHarvest(worldPosition);
            PlayMeatPop(_lastRemovedPosition);
        }

        private async void PlayMeatPop(Vector3 origin)
        {
            if (meatPop == null)
                return;

            // 앞의 연출이 아직 돌고 있으면 그쪽이 스스로 물러나게 한다
            int id = ++_popId;
            Color color = meatPop.color;
            color.a = 1f;

            try
            {
                meatPop.transform.localPosition = origin;
                meatPop.color = color;
                meatPop.gameObject.SetActive(true);

                float elapsed = 0f;
                while (elapsed < meatDuration && id == _popId)
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / meatDuration);

                    meatPop.transform.localPosition = origin + Vector3.up * (meatRise * t);
                    color.a = 1f - t;
                    meatPop.color = color;
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (id == _popId && meatPop != null)
                {
                    color.a = 1f;
                    meatPop.color = color;
                    meatPop.gameObject.SetActive(false);
                }
            }
        }
    }
}
