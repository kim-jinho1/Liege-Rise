using UnityEngine;

namespace Code.Animations
{
    [CreateAssetMenu(fileName = "Animator Parameter", menuName = "Units/Parameter", order = 20)]
    public class ParameterSO : ScriptableObject
    {
        [field: SerializeField] public string ParameterName { get; set; }
        [field: SerializeField] public int HashValue { get; private set; }

        private void OnValidate()
        {
            HashValue = Animator.StringToHash(ParameterName);
        }
    }
}
