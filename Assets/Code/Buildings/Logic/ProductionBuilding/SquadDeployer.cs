using System.Threading;
using Code.Commandable;
using Code.Core.Bus;
using UnityEngine;

namespace Code.Buildings.Logic.ProductionBuilding
{
    /// <summary>
    /// 완성된 분대를 문 앞에 세우고 집결지로 내보냅니다.
    /// 집결지에 바로 만들면 건물에서 나온 것이 아니라 허공에 나타난 것으로 보입니다.
    /// </summary>
    public class SquadDeployer
    {
        // 나올 때마다 집결지를 좌우로 이만큼씩 벌린다. 한 점에 계속 뱉으면 부대가 서로 겹쳐 선다
        private const float SpreadDegrees = 20f;

        // 한쪽으로 벌릴 수 있는 최대 칸. 순번마다 같은 방향으로 계속 돌리면 세 번째 부대부터
        // 건물 뒤로 돌아가 버린다 — 문 앞을 벗어나지 않도록 좌우 60도로 묶는다
        private const int MaxSpreadSteps = 3;

        private readonly Transform _door;

        private int _rallyIndex;

        public SquadDeployer(Transform door)
            => _door = door;

        /// <summary>
        /// 분대나 지휘관을 문 앞에 만들어 집결지로 걸어가게 합니다.
        /// </summary>
        public async Awaitable DeployAsync(AbstractCommandableSO data, Owner owner, Vector2 rallyOffset, CancellationToken token)
        {
            if (data == null || data.Prefab == null || _door == null)
                return;

            // 집결지와 나오는 자리에 같은 순번을 쓴다. 자리를 안 돌리면 문 앞에서 부대가 포개진다
            int slot = _rallyIndex++;

            Vector3 rally = RallyPointFor(rallyOffset, slot);
            GameObject squad = Object.Instantiate(data.Prefab,
                SpawnScatter.Around(_door.position, slot), Quaternion.identity);
            if (!squad.TryGetComponent(out AbstractCommandable commandable))
                return;

            commandable.Owner = owner;
            commandable.AbstractCommandableSO = data;

            // 분대는 Start에서 명부를 만든다. 그 전에 내린 이동 명령은 조용히 사라진다
            await Awaitable.NextFrameAsync(token);

            if (commandable != null)
                commandable.MoveTo(rally);
        }

        /// <summary>
        /// 집결지입니다. 첫 부대는 문이 보는 쪽 그대로, 다음부터는 좌우로 번갈아 벌립니다.
        /// </summary>
        private Vector3 RallyPointFor(Vector2 rallyOffset, int slot)
        {
            int step = (slot + 1) / 2;
            if (step > MaxSpreadSteps)
                step = (step - 1) % MaxSpreadSteps + 1;

            float sign = slot % 2 == 0 ? 1f : -1f;
            Quaternion spread = Quaternion.Euler(0f, 0f, sign * step * SpreadDegrees);
            return _door.position + (Vector3)(Vector2)(spread * rallyOffset);
        }
    }
}
