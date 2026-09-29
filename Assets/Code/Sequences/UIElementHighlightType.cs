namespace Code.Sequences
{
    // 에셋에 숫자로 저장된다. 순서를 바꾸거나 중간에 끼워 넣지 말 것 — 뒤에만 붙인다
    public enum UIElementHighlightType
    {
        None,
        ResourceHUD,
        BuildMenuButton,      // 일꾼의 Q 칸
        SquadProduceButton,   // 병영의 분대 생산 칸
        PawnProduceButton,    // 성·민가의 일꾼 생산 칸
        PopulationHUD,
        AppointButton,        // 분대의 지휘관 임명 칸
        FarmBuildButton,      // 건설 메뉴가 안 열려 있으면 Q 칸으로 내려간다
        CottageBuildButton,
        BarracksBuildButton,
        Minimap,
        CommanderProduceButton, // 병영의 지휘관 생산 칸
        ObjectiveHUD,           // 지금 할 일이 적힌 칸
        CommanderPortrait1,     // 오른쪽 초상 줄에서 깃발 1번 지휘관의 칸
        CommanderPortrait2,
    }
}
