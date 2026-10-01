using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiFinal
{
    /// <summary>
    /// リザルト画面。今回スコア(100点満点)・内訳・ハイスコア・ローカルランキング・
    /// 終了理由に応じた一言コメントを表示し、「リトライ」ボタン(Space)で再開、
    /// 「タイトルへ戻る」ボタン(Tab)でタイトルに戻る。
    /// 死亡(足ゲージMAX)/逃走(警戒ゲージMAX)/セミファイナル成功、どの終わり方でもここに来る。
    /// unityroomはカーソル非表示運用のため、クリックだけでなくキーボードでも操作できるようにしてある。
    /// </summary>
    public class ResultPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private TMP_Text scoreText;
        [Tooltip("距離・タイミング・足の開閉、それぞれの小計を表示するText")]
        [SerializeField] private TMP_Text breakdownText;
        [Tooltip("ローカルランキング(端末内保存の上位スコア)を表示するText")]
        [SerializeField] private TMP_Text rankingText;
        [SerializeField] private TMP_Text commentText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button backToTitleButton;

        [Header("SE")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("リザルト画面でスコアが表示された瞬間に鳴らす")]
        [SerializeField] private AudioClip scoreRevealClip;
        [Range(0f, 1f)] [SerializeField] private float scoreRevealVolume = 1f;

        [Header("一言コメント (終了理由ごとにランダムで1つ表示)")]
        [SerializeField]
        private string[] diedComments =
        {
            "力尽きた……でも、驚かせられた気がする。",
            "最後の力を出し切った。ナイスセミ生。",
            "抗いきれなかった……次はもっと粘れ。",
        };

        [SerializeField]
        private string[] fledComments =
        {
            "逃げられた……油断大敵。",
            "ビクッとしすぎたか。次はもう少し慎重に。",
            "警戒されて終了。連打は加減が大事。",
        };

        [SerializeField]
        private string[] clearedComments =
        {
            "セミファイナル、決まった。",
            "最後の力で、驚かせてやった。",
            "これが死に際の一撃。",
        };

        private void OnEnable()
        {
            gameManager.OnStateChanged += HandleStateChanged;
            if (retryButton != null) retryButton.onClick.AddListener(HandleRetryClicked);
            if (backToTitleButton != null) backToTitleButton.onClick.AddListener(HandleBackToTitleClicked);

            // ResultRoot自体がこの直前にアクティブ化されたばかりで、
            // 既にResult状態になっている(=イベントを取りこぼしている)可能性があるので、
            // 購読後に必ず現在の状態を直接反映しておく。
            HandleStateChanged(gameManager.State);
        }

        private void OnDisable()
        {
            gameManager.OnStateChanged -= HandleStateChanged;
            if (retryButton != null) retryButton.onClick.RemoveListener(HandleRetryClicked);
            if (backToTitleButton != null) backToTitleButton.onClick.RemoveListener(HandleBackToTitleClicked);
        }

        private void Update()
        {
            if (gameManager.State != GameState.Result) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame && retryButton != null)
            {
                retryButton.onClick.Invoke();
            }

            if (keyboard.tabKey.wasPressedThisFrame && backToTitleButton != null)
            {
                backToTitleButton.onClick.Invoke();
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Result) return;

            if (scoreText != null) scoreText.text = $"SCORE {scoreManager.TotalScore} / 100";
            if (breakdownText != null) breakdownText.text = BuildBreakdownText(gameManager.LastScoreBreakdown);
            if (rankingText != null) rankingText.text = BuildRankingText();
            if (commentText != null) commentText.text = PickComment(gameManager.LastEndReason);

            if (audioSource != null && scoreRevealClip != null)
            {
                audioSource.PlayOneShot(scoreRevealClip, scoreRevealVolume);
            }
        }

        private string BuildBreakdownText(ScoreBreakdown b)
        {
            return "【内訳】\n"
                + $"タイミング: {Mathf.RoundToInt(b.JudgeMultiplier * 100f)}%\n"
                + $"足の開閉　: {Mathf.RoundToInt(b.OpennessMultiplier * 100f)}%\n"
                + $"────────\n"
                + $"合計　　　: {b.TotalScore} / 100点";
        }

        private string BuildRankingText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("【ランキング】");

            var ranking = scoreManager.Ranking;
            if (ranking.Count == 0)
            {
                sb.Append("記録なし");
                return sb.ToString();
            }

            for (int i = 0; i < ranking.Count; i++)
            {
                sb.AppendLine($"{i + 1}位  {ranking[i]}点");
            }

            return sb.ToString().TrimEnd();
        }

        private string PickComment(EndReason reason)
        {
            string[] pool;
            switch (reason)
            {
                case EndReason.Died: pool = diedComments; break;
                case EndReason.Fled: pool = fledComments; break;
                default: pool = clearedComments; break;
            }

            if (pool == null || pool.Length == 0) return string.Empty;
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        private void HandleRetryClicked()
        {
            gameManager.StartGame();
        }

        private void HandleBackToTitleClicked()
        {
            gameManager.BackToTitle();
        }
    }
}
