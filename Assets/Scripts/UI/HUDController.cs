using TMPro;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// ゲーム画面のHUD。スコア表示のみを更新する。
    /// (足ゲージ・警戒ゲージのUI表示、判定カットインは廃止した)
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private ScoreManager scoreManager;

        [Header("UI")]
        [SerializeField] private TMP_Text scoreText;

        private void OnEnable()
        {
            scoreManager.OnScoreChanged += HandleScoreChanged;

            // 購読前に発火済みの初期値を取りこぼさないよう、現在値を手動で反映しておく
            HandleScoreChanged(scoreManager.TotalScore);
        }

        private void OnDisable()
        {
            scoreManager.OnScoreChanged -= HandleScoreChanged;
        }

        private void HandleScoreChanged(int score)
        {
            if (scoreText != null) scoreText.text = $"SCORE {score}";
        }
    }
}
