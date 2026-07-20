namespace DiceBattle.Core
{
    /// <summary>기본 주사위 배치 결과 정보.</summary>
    public readonly struct PlaceResult
    {
        /// <summary>이번 배치로 상대 주사위 제거가 발생했는지.</summary>
        public bool RemovalOccurred { get; }

        /// <summary>제거된 주사위 개수.</summary>
        public int RemovedCount { get; }

        /// <summary>제거로 인해 추가(특수) 주사위 배치가 대기 중인지.</summary>
        public bool ExtraDicePending { get; }

        public PlaceResult(bool removalOccurred, int removedCount, bool extraDicePending)
        {
            RemovalOccurred = removalOccurred;
            RemovedCount = removedCount;
            ExtraDicePending = extraDicePending;
        }
    }
}
