using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 두 <see cref="GameStateData"/>(직전에 그리고 있던 상태 → 상대의 수가 반영된 새 상태)를
    /// 비교해 "무엇이 어디에 놓였고, 제거가 있었다면 무엇이 사라졌는지"를 계산한다.
    ///
    /// 친선대전은 완성된 상태 스냅샷만 오갈 뿐 행동 자체를 전송하지 않는다(문서
    /// FriendlyMatch.md 1장 — 로컬 시뮬레이션 발산을 막기 위한 설계). 그래서 상대 턴의
    /// 이동/제거 연출을 보여주려면 받는 쪽이 직전 상태와 diff해서 무슨 일이 있었는지
    /// 역산해야 한다. 이 클래스가 그 역산 전담이다 — 순수 데이터만 다루므로(Unity 비의존)
    /// GameController(UI)와 분리해 단위 테스트할 수 있다.
    /// </summary>
    public sealed class RemoteMoveDiff
    {
        /// <summary>주사위가 놓인 필드(항상 "놓은 사람 자신의 필드" — 상대 필드 견제 배치도 포함).</summary>
        public PlayerId PlaceField;
        public int Line;
        public int InsertIndex;
        public DiceData Placed;

        /// <summary>
        /// 그룹화 삽입으로 밀려난 기존 주사위들(놓인 자리 뒤쪽, 옛 상태 기준 순서).
        /// 놓은 주사위가 상호 소멸로 즉시 사라진 경우(자기 필드에 아예 남지 않음)는
        /// 밀림 자체가 없으므로 항상 빈 배열이다.
        /// </summary>
        public DiceData[] Shifted;

        public bool RemovalOccurred;

        /// <summary>제거가 일어난 필드(놓은 필드의 반대쪽, 같은 라인).</summary>
        public PlayerId RemovedField;

        /// <summary>제거 전(옛 상태) 그 라인의 주사위 전체.</summary>
        public DiceData[] PreRemoval;

        /// <summary>PreRemoval과 같은 길이. true면 그 자리 주사위가 이번에 제거됐다.</summary>
        public bool[] Removed;

        /// <summary>
        /// 정확히 하나의 배치(± 그에 딸린 제거)로 설명되는 변화라면 계산해 돌려준다.
        /// 설명할 수 없는 모양이면(변화 없음, 여러 곳이 동시에 바뀜 등) null이다 —
        /// 호출자는 애니메이션 없이 스냅으로 반영하는 안전한 경로로 폴백해야 한다.
        /// </summary>
        public static RemoteMoveDiff Compute(GameStateData oldData, GameStateData newData)
        {
            PlayerId? growthField = null;
            int growthLine = -1;
            PlayerId? shrinkField = null;
            int shrinkLine = -1;

            for (int p = 0; p < 2; p++)
            {
                var player = (PlayerId)p;
                for (int i = 0; i < Field.LineCount; i++)
                {
                    int oldCount = oldData.Fields[p].Lines[i].Dice?.Length ?? 0;
                    int newCount = newData.Fields[p].Lines[i].Dice?.Length ?? 0;
                    if (newCount == oldCount) continue;

                    if (newCount == oldCount + 1)
                    {
                        if (growthField != null) return null; // 성장 라인이 둘 이상 — 설명 불가
                        growthField = player;
                        growthLine = i;
                    }
                    else if (newCount < oldCount)
                    {
                        if (shrinkField != null) return null; // 축소 라인이 둘 이상 — 설명 불가
                        shrinkField = player;
                        shrinkLine = i;
                    }
                    else
                    {
                        return null; // +2 이상 등 한 번의 배치로 설명되지 않는 변화
                    }
                }
            }

            if (growthField == null)
            {
                // 자란 라인이 없다. 변화도 전혀 없다면 메아리(호출자가 StatesEqual로 먼저
                // 거르는 게 정상 경로지만, 안전하게 여기서도 폴백)이고, 축소만 있다면 "놓은
                // 주사위가 특수가 아니어서 상호 소멸에 자신도 함께 사라진" 경우다 — 상대
                // 필드에는 아무 흔적도 안 남지만 여전히 제거 연출은 재현해야 한다.
                if (shrinkField == null) return null;
                return ComputeSelfDestroyedRemoval(oldData, newData, shrinkField.Value, shrinkLine);
            }

            DiceData[] oldGrowth = LineOf(oldData, growthField.Value, growthLine);
            DiceData[] newGrowth = LineOf(newData, growthField.Value, growthLine);

            int insertIndex = newGrowth.Length - 1; // 못 찾으면 맨 끝에 붙은 것
            for (int i = 0; i < oldGrowth.Length; i++)
            {
                if (!DiceEqual(oldGrowth[i], newGrowth[i])) { insertIndex = i; break; }
            }

            var shifted = new DiceData[oldGrowth.Length - insertIndex];
            for (int k = 0; k < shifted.Length; k++)
                shifted[k] = oldGrowth[insertIndex + k];

            var diff = new RemoteMoveDiff
            {
                PlaceField = growthField.Value,
                Line = growthLine,
                InsertIndex = insertIndex,
                Placed = newGrowth[insertIndex],
                Shifted = shifted,
            };

            if (shrinkField == null)
            {
                diff.RemovalOccurred = false;
                return diff;
            }

            // 제거는 오직 "놓은 사람 자신의 필드"에 배치했을 때만, 반대쪽 같은 라인에서 일어난다
            // (DiceGame.PlacePrimary 규칙). 그 모양이 아니면 설명 불가 — 폴백.
            if (shrinkLine != growthLine || shrinkField.Value != growthField.Value.Other())
                return null;

            if (!TryComputeRemoval(oldData, newData, shrinkField.Value, shrinkLine,
                    out DiceData[] pre, out bool[] removed))
                return null;

            diff.RemovalOccurred = true;
            diff.RemovedField = shrinkField.Value;
            diff.PreRemoval = pre;
            diff.Removed = removed;
            return diff;
        }

        /// <summary>
        /// 놓은 주사위가 특수가 아니어서 상호 소멸에 함께 사라진 경우(자기 필드에는 아무
        /// 성장도 없다). 제거된 값(=놓은 값)은 사라진 주사위들의 값으로 역산한다.
        /// </summary>
        private static RemoteMoveDiff ComputeSelfDestroyedRemoval(
            GameStateData oldData, GameStateData newData, PlayerId shrinkField, int shrinkLine)
        {
            if (!TryComputeRemoval(oldData, newData, shrinkField, shrinkLine, out var pre, out var removed))
                return null;

            int removedValue = -1;
            for (int k = 0; k < pre.Length; k++)
                if (removed[k]) { removedValue = pre[k].Value; break; }
            if (removedValue < 0) return null;

            PlayerId placeField = shrinkField.Other();
            return new RemoteMoveDiff
            {
                PlaceField = placeField,
                Line = shrinkLine,
                InsertIndex = 0,
                Placed = new DiceData { Value = removedValue, IsSpecial = false, Owner = placeField },
                Shifted = Array.Empty<DiceData>(),
                RemovalOccurred = true,
                RemovedField = shrinkField,
                PreRemoval = pre,
                Removed = removed,
            };
        }

        /// <summary>
        /// 한 라인의 옛/새 상태에서 어떤 자리가 제거됐는지 역산한다. RemoveAll은 순서를
        /// 보존하므로 새 목록은 옛 목록의 부분수열이다 — 왼쪽부터 그리디로 매칭한다.
        /// 그 가정이 깨지면(부분수열이 아니면) false를 돌려줘 호출자가 폴백하게 한다.
        /// </summary>
        private static bool TryComputeRemoval(
            GameStateData oldData, GameStateData newData, PlayerId field, int line,
            out DiceData[] pre, out bool[] removed)
        {
            pre = LineOf(oldData, field, line);
            DiceData[] post = LineOf(newData, field, line);

            removed = new bool[pre.Length];
            int j = 0;
            for (int k = 0; k < pre.Length; k++)
            {
                if (j < post.Length && DiceEqual(pre[k], post[j])) { j++; continue; }
                removed[k] = true;
            }
            return j == post.Length;
        }

        private static DiceData[] LineOf(GameStateData data, PlayerId field, int line)
            => data.Fields[field.Index()].Lines[line].Dice ?? Array.Empty<DiceData>();

        private static bool DiceEqual(DiceData a, DiceData b)
        {
            if (a == null || b == null) return a == b;
            return a.Value == b.Value && a.IsSpecial == b.IsSpecial && a.Owner == b.Owner;
        }
    }
}
