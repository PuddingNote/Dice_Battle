using System;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

namespace DiceBattle.Managers
{
    /// <summary>
    /// 친선대전 전용 Firebase 초기화(종속성 확인 + 익명 로그인 + Realtime Database 핸들).
    ///
    /// AI 대전만 하는 대다수 플레이어는 이 클래스를 절대 타지 않는다 — 그래서 앱 부팅
    /// 시점이 아니라 <b>친선대전 메뉴에 처음 들어갈 때</b> <see cref="EnsureReady"/>로
    /// 지연 초기화한다. 나머지 게임과는 완전히 무관한 부가 기능이라, 대다수에게 불필요한
    /// 네트워크 요청·초기화 지연을 안길 이유가 없다.
    ///
    /// Firebase SDK 타입(<see cref="FirebaseAuth"/>, <see cref="DatabaseReference"/> 등)은
    /// 이 클래스와 그 위의 네트워크 레이어 밖으로 새어 나가지 않게 <see cref="Root"/> 외에는
    /// 감춘다 — <c>AdManager</c>가 AdMob 타입을 가두는 것과 같은 이유다.
    /// </summary>
    public static class FirebaseBootstrapper
    {
        /// <summary>
        /// Realtime Database 위치(리전)는 프로젝트 생성 시 한 번 고정되고, google-services.json에는
        /// 실리지 않는다 — Firebase 콘솔의 Realtime Database 페이지 상단에 적힌 URL 그대로 적는다.
        /// </summary>
        private const string DatabaseUrl = "https://dicebattle-ea31e-default-rtdb.asia-southeast1.firebasedatabase.app/";

        public enum State { NotStarted, Initializing, Ready, Failed }

        public static State CurrentState { get; private set; } = State.NotStarted;

        /// <summary>마지막 실패 사유(로그/디버그용). 성공하면 null로 돌아간다.</summary>
        public static string LastError { get; private set; }

        /// <summary>지금 로그인된 익명 사용자 UID. 준비되기 전에는 null.</summary>
        public static string Uid => FirebaseAuth.DefaultInstance?.CurrentUser?.UserId;

        /// <summary>Realtime Database 루트. <see cref="State.Ready"/>가 되기 전에는 null.</summary>
        public static DatabaseReference Root { get; private set; }

        private static event Action<bool> Pending;

        /// <summary>
        /// 이미 준비됐으면 즉시, 아니면 초기화를 (필요하면) 시작하고 끝나는 대로 콜백한다.
        /// 여러 곳에서 동시에 불러도 초기화는 한 번만 일어난다.
        /// </summary>
        public static void EnsureReady(Action<bool> onDone)
        {
            if (CurrentState == State.Ready)
            {
                onDone?.Invoke(true);
                return;
            }

            Pending += onDone;
            if (CurrentState == State.Initializing) return;

            // Failed였다면 재시도로 취급한다(예: 아까는 네트워크가 없었을 뿐일 수 있다).
            CurrentState = State.Initializing;
            LastError = null;

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(depTask =>
            {
                if (depTask.IsCanceled || depTask.IsFaulted)
                {
                    Fail($"Firebase 종속성 확인 중 예외: {depTask.Exception}");
                    return;
                }
                if (depTask.Result != DependencyStatus.Available)
                {
                    Fail($"Firebase 종속성 사용 불가: {depTask.Result}");
                    return;
                }

                var app = FirebaseApp.DefaultInstance;
                FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(authTask =>
                {
                    if (authTask.IsCanceled || authTask.IsFaulted)
                    {
                        Fail($"익명 로그인 실패: {authTask.Exception}");
                        return;
                    }

                    Root = FirebaseDatabase.GetInstance(app, DatabaseUrl).RootReference;
                    CurrentState = State.Ready;
                    Succeed();
                });
            });
        }

        private static void Succeed()
        {
            var cb = Pending;
            Pending = null;
            cb?.Invoke(true);
        }

        private static void Fail(string message)
        {
            LastError = message;
            CurrentState = State.Failed;
            Debug.LogError($"[FirebaseBootstrapper] {message}");

            var cb = Pending;
            Pending = null;
            cb?.Invoke(false);
        }
    }
}
