using Code.Core.Bus;
using UnityEngine;

namespace Code.Commandable.Interfaces
{
    public interface IDamageable
    {
        int MaxHealth { get; }
        int CurrentHealth { get;  }
        Transform Transform { get; }
        public Owner Owner { get; }

        // 죽는 연출이 끝날 때까지 오브젝트가 살아 있어 Transform만으로는 사망을 못 본다
        bool IsDead { get; }

        // 지금 목표로 삼아도 되는가. 타워에 탄 유닛은 타워를 쳐야 하므로 여기서 빠진다
        bool IsTargetable { get; }

        void TakeDamage(int damage, Vector3 position, Vector3 normal);
        void Die();
    }
}
