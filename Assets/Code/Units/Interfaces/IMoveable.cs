using UnityEngine;

namespace Code.Units.Interfaces
{
    public interface IMoveable
    {
        void MoveTo(Vector3 position);
        void Stop();
    }
}
