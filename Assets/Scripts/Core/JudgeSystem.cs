using System;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>驚きパターンと連動する4段階のジャスト判定。</summary>
    public enum JudgeTier
    {
        Perfect,
        Great,
        Good,
        Miss,
    }

    /// <summary>
    /// JudgeTier(ApproachingPersonの距離ゾーン判定と共通)ごとの得点倍率テーブル。
    /// MonoBehaviourではないので、GameManagerのInspector上にそのままフィールドとして展開される。
    /// </summary>
    [Serializable]
    public class JudgeSystem
    {
        [Header("判定ごとの得点倍率")]
        [SerializeField] private float perfectMultiplier = 1.0f;
        [SerializeField] private float greatMultiplier = 0.7f;
        [SerializeField] private float goodMultiplier = 0.4f;
        [SerializeField] private float missMultiplier = 0f;

        public float GetMultiplier(JudgeTier tier)
        {
            switch (tier)
            {
                case JudgeTier.Perfect: return perfectMultiplier;
                case JudgeTier.Great: return greatMultiplier;
                case JudgeTier.Good: return goodMultiplier;
                default: return missMultiplier;
            }
        }
    }
}
