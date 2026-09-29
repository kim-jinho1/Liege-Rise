using Code.Commandable.Interfaces;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Units.Interfaces
{
    public interface IAttacker
    {
        public DamageableSensor Sensor { get;}
        Transform Transform { get; }
        void Attack(IDamageable damageable);
        void Attack(Vector3 location);
    }
}
