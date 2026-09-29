namespace Code.Sequences
{
    // 에셋에 숫자로 저장된다. 순서를 바꾸거나 중간에 끼워 넣지 말 것 — 뒤에만 붙인다
    public enum GameObjectHighlightType
    {
        None,
        Pawn,
        FlowerObject,        // 옛 값. 쓰는 곳 없음
        ProcessingBuilding,  // 옛 값. 쓰는 곳 없음
        Enemy,               // 성에서 가장 가까운 적 무리
        MainCastle,
        Tree,
        GoldOre,             // 옛 값. 쓰는 곳 없음
        EnemyBase,
        Commander,
        Barracks,
        Farm,
        Cottage,
        Squad,               // 지휘관 없는 분대가 있으면 그것만
        AmbushSpot,          // 안내 손 전용 좌표 — 성↔소굴 축에서 옆으로 벗어난 곳. 강조 대상은 없다
        Commander1,          // 깃발 1번 지휘관. "1번은 성 앞으로"처럼 특정 지휘관을 짚을 때
        Commander2,
        FreeArmy,            // 분대 없는 지휘관 + 지휘관 없는 분대. 같이 담고 긋기용

        // 아래는 전부 StageSO.GuidePoints 에 사람이 찍어 둔 좌표다. 강조 대상은 없고 자리만 낸다.
        // 코드가 성 기준 오프셋으로 추측하면 지형을 못 봐서 나무·물 위를 짚는다 — 지도를 보고 찍는 쪽이 맞다
        FarmSpot,
        CottageSpot,
        BarracksSpot,
        DefendSpot,          // 성 앞에 선을 긋는 자리
        MoveSpot,            // "빈 풀밭으로 가 보세요"
    }
}
