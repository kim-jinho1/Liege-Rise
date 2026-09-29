using System.Collections.Generic;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.System;
using UnityEngine;

namespace Code.Managers
{
    /// <summary>
    /// Owner별 HeadquartersBuilding 생존 수를 추적하고,
    /// 특정 Owner의 건물이 전멸하면 GameOverEvent를 발행합니다.
    /// </summary>
    public class HeadquartersBuildingManager : MonoSingleton<HeadquartersBuildingManager>
    {
        private readonly Dictionary<Owner, int> _aliveCount = new();

        /// <summary>
        /// 마지막으로 무너진 본거지의 자리입니다. 승패가 갈린 지점을 비춰 주는 쪽이 읽습니다.
        /// </summary>
        public Vector3 LastFallenPosition { get; private set; }

        /// <summary>
        /// 해당 Owner의 HeadquartersBuilding 생존 수를 1 증가시킵니다.
        /// </summary>
        public void Register(Owner owner)
        {
            _aliveCount.TryGetValue(owner, out int count);
            _aliveCount[owner] = count + 1;
        }

        /// <summary>
        /// 해당 Owner의 HeadquartersBuilding 생존 수를 1 감소시키고,
        /// 0이 되면 반대편 Owner를 승자로 하는 GameOverEvent를 발행합니다.
        /// </summary>
        public void Unregister(Owner owner, Vector3 position)
        {
            if (!_aliveCount.TryGetValue(owner, out int count))
                return;

            LastFallenPosition = position;
            _aliveCount[owner] = --count;
            if (count == 0)
            {
                Owner winner = owner == Owner.Player ? Owner.Enemy : Owner.Player;
                Bus<GameOverEvent>.Raise(Owner.Unowned, new GameOverEvent(winner));
            }
        }
    }
}
