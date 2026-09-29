using System;
using Code.Core.Bus;
using UnityEngine;

namespace Code.Core.FactionPalette
{
    /// <summary>
    /// 진영 팔레트
    /// </summary>
    [Serializable]
    public struct FactionProfile
    {
        [field: SerializeField] public Owner Owner { get; private set; }
        [field: SerializeField] public Color SelectionColor { get; private set; }
        [field: SerializeField] public Color MinimapColor { get; private set; }
    }
}
