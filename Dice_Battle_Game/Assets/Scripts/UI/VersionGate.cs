using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 강제 업데이트 검사. 실행 직후 원격 JSON 하나를 받아
    /// 설치된 버전이 요구 최소 버전보다 낮으면 <see cref="UpdateRequiredView"/>로 게임을 막는다.
    ///
    /// 원격 파일로 판정하는 이유는 <b>어떤 버전을 막을지 앱 재빌드 없이 정하기 위해서</b>다.
    /// 스토어에 새 버전이 있다고 무조건 막으면 오타 수정 같은 사소한 업데이트까지 강제된다.
    ///
    /// 설계 원칙: <b>확인에 실패하면 통과시킨다(fail-open).</b>
    /// 오프라인·서버 장애·JSON 오타로 게임이 아예 켜지지 않는 쪽이 구버전 플레이보다 훨씬 나쁘다.
    /// </summary>
    public sealed class VersionGate : MonoBehaviour
    {
        /// <summary>
        /// 버전 정보 파일. 게임별 저장소가 아니라 별도의 공개 GitHub Pages 저장소
        /// (PuddingNote.github.io)의 게임 이름 폴더에 둔다 — 개인정보처리방침과 함께
        /// 이쪽으로 옮겼다(2026-08-11). 게임 저장소 자체는 이후 private으로 바뀔 예정이라,
        /// 강제 업데이트 확인 파일만 별도의 공개 저장소에 남겨 둔다.
        ///
        /// GitHub Pages도 CDN 캐시가 있으나, minVersion은 새 빌드가 Play에 완전히
        /// 배포된 뒤에 올리므로 문제가 되지 않는다.
        ///
        /// <b>주의</b>: 이 URL은 빌드 시점에 앱 안에 그대로 박힌다. 이미 설치된 구버전은
        /// 옛 주소(raw.githubusercontent.com/PuddingNote/Dice_Battle/master/version.json)를
        /// 계속 본다 — 그 주소를 지우는 건 이 URL로 바뀐 빌드가 충분히 퍼진 뒤여야 한다
        /// (fail-open이라 크래시는 안 나지만, 구버전의 강제 업데이트 체크가 조용히 무력화된다).
        /// </summary>
        private const string ConfigUrl =
            "https://puddingnote.github.io/dicebattle/version.json";

        /// <summary>응답 대기 한도(초). 넘기면 통과시킨다.</summary>
        private const int TimeoutSeconds = 5;

        private const string DefaultMessage =
            "새로운 버전이 나왔습니다.\n업데이트 후 이용해 주세요.";

        /// <summary>
        /// 차단 창이 떠 있는가. 뒤로가기 처리(<see cref="GameManager"/>)가 이 값을 먼저 본다.
        /// </summary>
        public static bool IsBlocking { get; private set; }

        private UpdateRequiredView _view;

        /// <summary>원격에서 받는 JSON 형식. 필드 이름이 곧 JSON 키다.</summary>
        [Serializable]
        private sealed class RemoteConfig
        {
            /// <summary>이 버전보다 낮으면 차단한다. 비어 있으면 아무도 막지 않는다.</summary>
            public string minVersion;
            /// <summary>[업데이트] 버튼이 여는 주소. 비우면 패키지명으로 만든 Play 주소를 쓴다.</summary>
            public string storeUrl;
            /// <summary>차단 창 문구. 비우면 기본 문구를 쓴다.</summary>
            public string message;
        }

        /// <summary>
        /// 검사기를 만들고 즉시 조회를 시작한다.
        /// 조회는 비동기라 그동안 게임은 정상적으로 뜬다 — 응답이 오면 그때 위에 덮는다.
        /// </summary>
        public static VersionGate Create(Transform parent, RectTransform canvasRoot)
        {
            IsBlocking = false; // 에디터에서 도메인 리로드를 끄면 이전 실행 값이 남는다

            var go = new GameObject("VersionGate");
            go.transform.SetParent(parent, false);

            var gate = go.AddComponent<VersionGate>();
            gate._view = new UpdateRequiredView(canvasRoot);
            gate.StartCoroutine(gate.Check());
            return gate;
        }

        private IEnumerator Check()
        {
            string json;
            using (var request = UnityWebRequest.Get(ConfigUrl))
            {
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // 비행기 모드/지하철/서버 장애. 막지 않는다.
                    Debug.Log($"[VersionGate] 버전 확인 실패로 통과: {request.error}");
                    yield break;
                }
                json = request.downloadHandler.text;
            }

            RemoteConfig config = Parse(json);
            if (config == null || string.IsNullOrWhiteSpace(config.minVersion)) yield break;

            string installed = Application.version;
            if (!AppVersion.IsOlder(installed, config.minVersion))
            {
                Debug.Log($"[VersionGate] 버전 확인 통과: {installed} >= {config.minVersion}");
                yield break;
            }

            Debug.Log($"[VersionGate] 업데이트 필요: {installed} < {config.minVersion}");
            IsBlocking = true;
            _view.Open(Message(config), StoreUrl(config));
        }

        /// <summary>JSON이 깨져 있으면 null. 파일 오타로 전원을 막지 않기 위해 예외를 삼킨다.</summary>
        private static RemoteConfig Parse(string json)
        {
            try
            {
                return JsonUtility.FromJson<RemoteConfig>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VersionGate] version.json 해석 실패로 통과: {e.Message}");
                return null;
            }
        }

        private static string Message(RemoteConfig config)
            => string.IsNullOrWhiteSpace(config.message) ? DefaultMessage : config.message;

        /// <summary>
        /// 스토어 주소. 원격에 없으면 패키지명으로 만든다.
        /// https 형식의 Play 주소는 안드로이드에서 Play 앱이 직접 받아 연다.
        /// </summary>
        private static string StoreUrl(RemoteConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.storeUrl)) return config.storeUrl;
            return "https://play.google.com/store/apps/details?id=" + Application.identifier;
        }
    }
}
