using System.Collections.Generic;
using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 앵커 하나를 채우며 생긴 미달과 완화를 모아 마지막에 한 줄로 알립니다.
    /// 항목마다 경고를 내면 구역 하나가 콘솔 대여섯 줄을 먹습니다.
    /// </summary>
    public class PlacementNotes
    {
        private readonly List<string> _notes = new(4);

        /// <summary>
        /// 목표 개수에 못 미친 항목을 적어 둡니다.
        /// </summary>
        public void Shortfall(string label, int missing)
        {
            if (missing > 0)
                _notes.Add($"{label} {missing}개 미달");
        }

        /// <summary>
        /// 뭉침 반경을 풀고 놓은 횟수를 적어 둡니다.
        /// </summary>
        public void Relaxed(int count)
        {
            if (count > 0)
                _notes.Add($"뭉침 반경을 {count}회 해제");
        }

        /// <summary>
        /// 모아 둔 내용을 한 줄로 알리고 비웁니다. 적을 것이 없으면 아무 말도 하지 않습니다.
        /// </summary>
        public void Flush(string ownerLabel, Object context)
        {
            if (_notes.Count > 0)
                Debug.LogWarning($"[MapGenerator] {ownerLabel} — {string.Join(" · ", _notes)}", context);

            _notes.Clear();
        }
    }
}
