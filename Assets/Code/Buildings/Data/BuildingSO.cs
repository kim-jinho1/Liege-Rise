using Code.Commandable;
using UnityEngine;

namespace Code.Buildings.Data
{
    [CreateAssetMenu(fileName = "Building", menuName = "Buildings/Building", order = 11)]
    public class BuildingSO : AbstractCommandableSO
    {
        /// <summary>
        /// 건설 중일 때 사용할 더미 프리팹입니다. 건설 시작 시 한 번 생성됩니다.
        /// </summary>
        [field: SerializeField] public GameObject ConstructionDummyPrefab { get; private set; }

        /// <summary>
        /// 건물이 파괴될 때 사용할 스프라이트입니다. 잔해 시스템에서 사용됩니다.
        /// </summary>
        [field: SerializeField] public Sprite DestroyedSprite { get; private set; }

        /// <summary>
        /// 건물이 파괴된 후 잔해가 남아 있는 시간입니다. 0이면 잔해가 남지 않습니다.
        /// </summary>
        [field: SerializeField, Min(0f)] public float RuinsLifetime { get; private set; } = 30f;

        /// <summary>
        /// 건물의 크기입니다. 1x1은 1타일, 2x2는 4타일을 차지합니다.
        /// </summary>
        [field: SerializeField] public Vector2Int Size { get; private set; } = Vector2Int.one;

        /// <summary>
        /// 건설 중인 일꾼이 발자국 경계에서 얼마나 떨어져 서는지입니다. 0이면 경계에 붙습니다.
        /// </summary>
        [field: SerializeField, Min(0f)] public float WorkMargin { get; private set; } = 0.1f;

        /// <summary>
        /// 건물에 배정할 수 있는 최대 일꾼 수입니다. 0이면 일꾼을 배정할 수 없습니다.
        /// </summary>
        [field: SerializeField] public int MaxBuilders { get; private set; } = 4;

        public override object Clone()
        {
            return (BuildingSO)base.Clone();
        }
    }
}
