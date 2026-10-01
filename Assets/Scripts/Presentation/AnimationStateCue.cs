using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// 指定したAnimatorで、指定したStateを再生するだけの汎用スクリプト。
    /// TutorialPanel.onShow のようなUnityEvent経由で、
    /// 「このタイミングで、このアニメーションを再生する」を個別に指定したい場面で使う。
    /// ゲームプレイ側のスクリプト(ApproachingPersonなど)には一切依存しない。
    /// </summary>
    public class AnimationStateCue : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private int layerIndex = 0;
        [Tooltip("再生前にAnimator.speedを1に戻す(足のスクラブ演出等でspeedが0にされている場合の保険)")]
        [SerializeField] private bool resetSpeed = true;

        /// <summary>
        /// UnityEventのStatic Parameter(文字列)からState名を渡して再生する。
        /// 例：TutorialPanel.onShow に登録し、Inspector上でState名を直接指定する。
        /// </summary>
        public void PlayState(string stateName)
        {
            Debug.Log($"[AnimationStateCue] PlayState呼び出し: gameObject={gameObject.name}, animator={(animator != null ? animator.name : "null")}, stateName=\"{stateName}\"", this);

            if (animator == null || string.IsNullOrEmpty(stateName)) return;
            if (resetSpeed) animator.speed = 1f;
            animator.CrossFadeInFixedTime(stateName, 0.1f, layerIndex);
        }
    }
}
