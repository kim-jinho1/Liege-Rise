using System.Collections.Generic;
using Code.Commandable;
using UnityEngine;

namespace Code.Core
{
    public abstract class AbstractCommandableFinderSO<T> : ScriptableObject
    {
        [field: SerializeField] public T Value { get; set; }

        public void Set(T newValue)
        {
            Value = newValue;
        }
    }

    [CreateAssetMenu(fileName = "Commandable Finder", menuName = "SO/Commandable Finder", order = 0)]
    public class CommandableFinderSO : AbstractCommandableFinderSO<AbstractCommandable> { }

    [CreateAssetMenu(fileName = "Commandable List Finder", menuName = "SO/Commandable List Finder", order = 0)]
    public class CommandableListFinderSO : AbstractCommandableFinderSO<List<AbstractCommandable>>
    {
        public void Add(AbstractCommandable item)
        {
            Value ??= new List<AbstractCommandable>();

            Value.Add(item);
        }
    }
}
