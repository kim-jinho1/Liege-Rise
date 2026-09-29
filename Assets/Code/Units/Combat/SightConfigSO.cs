using UnityEngine;

namespace Code.Units.Combat
{
    /// <summary>
    /// 유닛의 시야 반경을 정의하는 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "Sight config", menuName = "Units/Sight config", order = 7)]
    public class SightConfigSO : ScriptableObject
    {
        [field: SerializeField] public float SightRadius { get; private set; } = 5;
    }
}
