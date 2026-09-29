using System;
using UnityEngine;

namespace Code.Players.Cursors
{
    [Serializable]
    public class Entry
    {
        [field: SerializeField] public CursorKind Kind { get; private set; }
        [field: SerializeField] public Texture2D Texture { get; private set; }

        /// <summary>
        /// 실제로 클릭되는 한 점입니다. 텍스처의 좌상단이 (0, 0)입니다.
        /// </summary>
        [field: SerializeField] public Vector2 Hotspot { get; private set; }
    }

    /// <summary>
    /// 커서 종류마다 그림과 핫스팟을 담는 표입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CursorSet", menuName = "UI/Cursor Set", order = 0)]
    public class CursorSetSO : ScriptableObject
    {
        [SerializeField] private Entry[] entries;

        /// <summary>
        /// 해당 종류의 그림과 핫스팟을 찾습니다. 칸이 비었거나 그림이 없으면 false입니다.
        /// </summary>
        public bool TryGet(CursorKind kind, out Texture2D texture, out Vector2 hotspot)
        {
            texture = null;
            hotspot = Vector2.zero;

            if (entries == null)
                return false;

            foreach (Entry entry in entries)
            {
                if (entry == null || entry.Kind != kind || entry.Texture == null)
                    continue;

                texture = entry.Texture;
                hotspot = entry.Hotspot;
                return true;
            }

            return false;
        }
    }
}
