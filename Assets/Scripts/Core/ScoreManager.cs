using System;
using System.Collections.Generic;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// スコア ＝ タイミング判定倍率(ゾーン判定) × 足の開閉倍率。
    /// 各係数は0〜1なので、既定値(100点)のままなら常に0〜100点に収まる(=100点満点)。
    /// ローカルランキングはPlayerPrefsに保存(端末内のみ。オンライン共有はしない)。
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Tooltip("倍率1.0(Perfect かつ 足の開閉が最良)のときの得点。100のままなら100点満点になる")]
        [SerializeField] private float scorePerFullDistance = 100f;

        [Tooltip("ローカルランキングに保存する件数")]
        [SerializeField] private int rankingSize = 5;

        private const string RankingKey = "SemiFinal_Ranking";

        public int TotalScore { get; private set; }
        /// <summary>ローカル保存されている上位スコア(降順)。1位が実質のハイスコア。</summary>
        public IReadOnlyList<int> Ranking => ranking;

        private readonly List<int> ranking = new List<int>();

        /// <summary>スコアが変化するたびに発火(UI更新用)。</summary>
        public event Action<int> OnScoreChanged;

        private void Awake()
        {
            LoadRanking();
        }

        /// <summary>ゲーム開始時に呼ぶ。</summary>
        public void ResetScore()
        {
            TotalScore = 0;
            OnScoreChanged?.Invoke(TotalScore);
        }

        /// <summary>1回の発動結果を加点する。multiplierは0〜1(タイミング倍率×足の開閉倍率)。返り値は今回の獲得点。</summary>
        public int AddResult(float multiplier)
        {
            int gained = Mathf.RoundToInt(Mathf.Clamp01(multiplier) * scorePerFullDistance);
            TotalScore += gained;
            OnScoreChanged?.Invoke(TotalScore);
            return gained;
        }

        /// <summary>ラン終了時に呼ぶ。今回のスコアをローカルランキングに登録する。</summary>
        public void SubmitToRanking()
        {
            ranking.Add(TotalScore);
            ranking.Sort((a, b) => b.CompareTo(a)); // 降順
            if (ranking.Count > rankingSize)
            {
                ranking.RemoveRange(rankingSize, ranking.Count - rankingSize);
            }
            SaveRanking();
        }

        private void LoadRanking()
        {
            ranking.Clear();
            string raw = PlayerPrefs.GetString(RankingKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;

            foreach (string part in raw.Split(','))
            {
                if (int.TryParse(part, out int value))
                {
                    ranking.Add(value);
                }
            }
        }

        private void SaveRanking()
        {
            PlayerPrefs.SetString(RankingKey, string.Join(",", ranking));
            PlayerPrefs.Save();
        }
    }
}
