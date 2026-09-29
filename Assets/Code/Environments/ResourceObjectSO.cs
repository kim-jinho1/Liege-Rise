using Code.Economics;
using UnityEngine;

namespace Code.Environments
{
    /// <summary>
    /// 맵에 배치되는 자원 오브젝트의 데이터 SO.
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceObjectSO", menuName = "Environment/Resource Object")]
    public class ResourceObjectSO : ScriptableObject
    {
        [field: SerializeField] public ResourceSO ResourceSO { get; private set; }
        [field: SerializeField] public int MaxAmount { get; private set; } = 100;

        /// <summary>
        /// 고갈 시 표시할 스프라이트입니다. null이면 오브젝트를 비활성화합니다.
        /// </summary>
        [field: SerializeField] public Sprite DepletedSprite { get; private set; }
    }
}
