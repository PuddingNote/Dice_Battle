using UnityEngine;
using DiceBattle.AI;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 난이도(등급 1~5)별 AI 수치. Inspector에서 바로 조정 가능.
    /// Project 창 우클릭 → Create → DiceBattle → Difficulty Config 로 에셋 생성 후
    /// GameBootstrap 의 Difficulty 슬롯에 연결한다. 비우면 코드 기본값을 사용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "DiceBattle/Difficulty Config")]
    public sealed class DifficultyConfig : ScriptableObject
    {
        [System.Serializable]
        public struct LevelSettings
        {
            [Tooltip("기본 배치를 '최선 수'로 둘 확률")]
            [Range(0f, 1f)] public float playBest;
            [Tooltip("기본 배치를 '일부러 최악 수'로 둘 확률(쉬움용)")]
            [Range(0f, 1f)] public float playWorst;
            [Tooltip("추가(특수) 주사위를 똑똑하게 배치할 확률(방해 배치 포함)")]
            [Range(0f, 1f)] public float smartExtra;
            [Tooltip("AI 주사위 하향 편향(0=공정, 1=강하게 낮은 값). 높을수록 AI가 불리=쉬움")]
            [Range(0f, 1f)] public float diceLowBias;
        }

        [Tooltip("Lv1~Lv5 순서. 값이 없으면 코드 기본값 사용.")]
        public LevelSettings[] levels = new LevelSettings[]
        {
            new LevelSettings { playBest = 0.0f, playWorst = 1.0f, smartExtra = 0.0f, diceLowBias = 1.0f }, // Lv1 쉬움
            new LevelSettings { playBest = 0.0f, playWorst = 0.5f, smartExtra = 0.2f, diceLowBias = 0.6f }, // Lv2
            new LevelSettings { playBest = 0.3f, playWorst = 0.0f, smartExtra = 0.4f, diceLowBias = 0.4f }, // Lv3
            new LevelSettings { playBest = 0.7f, playWorst = 0.0f, smartExtra = 0.7f, diceLowBias = 0.2f }, // Lv4
            new LevelSettings { playBest = 1.0f, playWorst = 0.0f, smartExtra = 1.0f, diceLowBias = 0.0f }, // Lv5 어려움
        };

        public bool TryGet(int level, out LevelSettings settings)
        {
            int idx = level - 1;
            if (levels != null && idx >= 0 && idx < levels.Length)
            {
                settings = levels[idx];
                return true;
            }
            settings = default;
            return false;
        }

        /// <summary>설정에 따라 AI 전략을 생성.</summary>
        public IAiStrategy CreateAi(int level)
        {
            if (TryGet(level, out var s))
                return new LeveledAiStrategy(s.playBest, s.playWorst, s.smartExtra);
            return new LeveledAiStrategy(level);
        }

        /// <summary>설정에 따라 난이도 주사위 롤러를 생성.</summary>
        public IDiceRoller CreateRoller(PlayerId aiPlayer, int level)
        {
            if (TryGet(level, out var s))
                return new DifficultyDiceRoller(aiPlayer, (double)s.diceLowBias, new System.Random());
            return new DifficultyDiceRoller(aiPlayer, level);
        }
    }
}
