using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    /// <summary>버전 문자열 비교 테스트(강제 업데이트 판정의 근거).</summary>
    public class AppVersionTests
    {
        [Test]
        public void Same_Version_Is_Not_Older()
        {
            Assert.IsFalse(AppVersion.IsOlder("0.9.3", "0.9.3"));
            Assert.AreEqual(0, AppVersion.Compare("1.2.3", "1.2.3"));
        }

        [Test]
        public void Lower_Version_Is_Older()
        {
            Assert.IsTrue(AppVersion.IsOlder("0.9.2", "0.9.3"), "패치 자리가 낮으면 구버전이다.");
            Assert.IsTrue(AppVersion.IsOlder("0.9.9", "1.0.0"), "메이저 자리가 낮으면 구버전이다.");
        }

        [Test]
        public void Higher_Version_Is_Not_Older()
        {
            // 스토어 반영이 늦어 요구 버전보다 앞선 빌드를 들고 있어도 막히면 안 된다.
            Assert.IsFalse(AppVersion.IsOlder("1.0.0", "0.9.3"));
        }

        [Test]
        public void Segments_Are_Compared_As_Numbers_Not_Text()
        {
            // 문자열 비교라면 "0.10.0" < "0.9.9"가 되어 최신 버전이 구버전으로 판정된다.
            Assert.IsFalse(AppVersion.IsOlder("0.10.0", "0.9.9"), "10은 9보다 크다.");
            Assert.IsTrue(AppVersion.IsOlder("0.9.9", "0.10.0"));
        }

        [Test]
        public void Missing_Segments_Are_Treated_As_Zero()
        {
            Assert.AreEqual(0, AppVersion.Compare("1.0", "1.0.0"), "빈 자리는 0으로 본다.");
            Assert.IsTrue(AppVersion.IsOlder("1.0", "1.0.1"));
        }

        [Test]
        public void Missing_Required_Version_Never_Blocks()
        {
            // 원격 파일에 minVersion이 비어 있거나 깨졌을 때 전원이 막히면 안 된다.
            Assert.IsFalse(AppVersion.IsOlder("0.9.2", null));
            Assert.IsFalse(AppVersion.IsOlder("0.9.2", ""));
            Assert.IsFalse(AppVersion.IsOlder("0.9.2", "알 수 없음"));
        }
    }
}
