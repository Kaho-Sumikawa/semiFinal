namespace SemiFinal
{
    /// <summary>ゲームが終了した理由。リザルト画面の一言コメント出し分けに使う。</summary>
    public enum EndReason
    {
        /// <summary>足ゲージMAXで死亡。</summary>
        Died,
        /// <summary>警戒ゲージMAXで逃げられた。</summary>
        Fled,
        /// <summary>セミファイナル(発動)を使い切ってラウンド終了。</summary>
        Cleared,
    }
}
