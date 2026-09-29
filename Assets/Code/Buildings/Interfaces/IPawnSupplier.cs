using Code.Units.Pawns;

namespace Code.Buildings.Interfaces
{
    /// <summary>
    /// 판이 시작될 때 폰을 몇 기 세워 두는지입니다. 자동 재생성은 없습니다(2026-09-07) —
    /// 폰은 본부·집의 생산 버튼으로 뽑고, 상한은 IPawnCapacityProvider가 따로 공급합니다.
    /// </summary>
    public interface IPawnSupplier
    {
        PawnSO PawnSO { get; }

        int InitialPawns { get; }
    }
}
