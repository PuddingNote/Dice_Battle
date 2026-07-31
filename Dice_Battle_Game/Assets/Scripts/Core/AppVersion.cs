using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// "0.9.3" 형태의 점 구분 버전 문자열 비교.
    /// 강제 업데이트 판정(설치된 <c>Application.version</c> vs 서버가 요구하는 최소 버전)에 쓴다.
    ///
    /// 문자열을 그대로 비교하면 "0.10.0" &lt; "0.9.9"가 되므로 반드시 조각별 숫자로 비교해야 한다.
    /// 게임 규칙과는 무관하지만 UnityEngine에 의존하지 않는 순수 계산이라 Core에 둔다.
    /// </summary>
    public static class AppVersion
    {
        /// <summary>비교할 최대 조각 수. "1.2.3.4"까지 받는다.</summary>
        private const int MaxSegments = 4;

        /// <summary>
        /// <paramref name="a"/>가 낮으면 음수, 같으면 0, 높으면 양수.
        /// 조각 수가 다르면 짧은 쪽의 빈 자리를 0으로 본다("1.0"과 "1.0.0"은 같다).
        /// </summary>
        public static int Compare(string a, string b)
        {
            int[] left = Parse(a);
            int[] right = Parse(b);

            for (int i = 0; i < MaxSegments; i++)
            {
                if (left[i] != right[i]) return left[i] < right[i] ? -1 : 1;
            }
            return 0;
        }

        /// <summary>
        /// <paramref name="installed"/>가 <paramref name="required"/>보다 낮은가(= 업데이트가 필요한가).
        /// </summary>
        public static bool IsOlder(string installed, string required)
            => Compare(installed, required) < 0;

        /// <summary>
        /// 버전 문자열을 숫자 조각으로 나눈다.
        /// 숫자로 못 읽는 조각("1.0-beta"의 "0-beta" 등)은 0으로 본다.
        /// 판정 불가를 이유로 게임을 막는 것보다 통과시키는 쪽이 안전하기 때문이다.
        /// </summary>
        private static int[] Parse(string version)
        {
            var segments = new int[MaxSegments];
            if (string.IsNullOrWhiteSpace(version)) return segments;

            string[] parts = version.Trim().Split('.');
            int count = Math.Min(parts.Length, MaxSegments);
            for (int i = 0; i < count; i++)
            {
                if (int.TryParse(parts[i], out int value) && value > 0) segments[i] = value;
            }
            return segments;
        }
    }
}
