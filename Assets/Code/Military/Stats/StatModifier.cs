namespace Code.Military.Stats
{
    /// <summary>
    /// 스탯에 적용되는 보정값을 나타냅니다.입니다.
    /// </summary>
    public readonly struct StatModifier
    {
        public ModifierOperator ModifierOp { get; }
        public float Value { get; }
        public object Source { get; }

        public StatModifier(ModifierOperator modifierOp, float value, object source)
        {
            ModifierOp = modifierOp;
            Value = value;
            Source = source;
        }
    }
}

