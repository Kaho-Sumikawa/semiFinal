namespace SemiFinal
{
    /// <summary>
    /// 1回のセミファイナル判定の得点内訳。すべて0〜1(%表示用)＋最終得点(0〜100点)。
    /// リザルト画面の小計表示に使う。
    /// </summary>
    public readonly struct ScoreBreakdown
    {
        /// <summary>タイミング判定倍率(0=Miss〜1=Perfect)。</summary>
        public readonly float JudgeMultiplier;
        /// <summary>足の開閉による倍率(0〜1。中間が最大、全開/全閉で0)。</summary>
        public readonly float OpennessMultiplier;
        /// <summary>最終得点。scorePerFullDistanceが100のままなら0〜100点。</summary>
        public readonly int TotalScore;

        public ScoreBreakdown(float judgeMultiplier, float opennessMultiplier, int totalScore)
        {
            JudgeMultiplier = judgeMultiplier;
            OpennessMultiplier = opennessMultiplier;
            TotalScore = totalScore;
        }
    }
}
