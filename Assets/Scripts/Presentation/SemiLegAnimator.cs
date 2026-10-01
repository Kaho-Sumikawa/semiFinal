using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// 足ゲージの値(0=全開/安全 〜 1=全閉/死)を、セミの「閉じる」アニメーションクリップの
    /// 再生位置(normalizedTime)に直接連動させる。
    /// クリップは「0%地点=足全開、100%地点=足全閉」の1本(閉じるパターンのみ)で作られている前提。
    /// Animatorのパラメータ/Blend Treeは使わず、毎フレーム Animator.Play() で時間を直接指定する
    /// (連打のたびに/自動で閉じるたびにLegGaugeがOnValueChangedを発火するので、それに追従するだけ)。
    /// LegGauge側には一切手を加えない(値の変化を購読するだけ)。
    /// </summary>
    public class SemiLegAnimator : MonoBehaviour
    {
        [SerializeField] private LegGauge legGauge;
        [SerializeField] private Animator animator;

        [Tooltip("閉じるアニメーションのState名(Animatorウィンドウの実際の名前と合わせる)")]
        [SerializeField] private string legStateName = "アーマチュア|アーマチュアアクション";
        [SerializeField] private int layerIndex = 0;

        [Tooltip("クリップの向きが逆(0%=全閉, 100%=全開)になっている場合はチェック")]
        [SerializeField] private bool invert = false;

        private bool suspended;

        /// <summary>
        /// trueの間は足の開閉スクラブ更新を止める(セミファイナル等、別のアニメーションに
        /// Animatorを明け渡している間に使う)。falseに戻すと現在の足ゲージ値へ即座に再同期する。
        /// </summary>
        public void SetSuspended(bool value)
        {
            suspended = value;
            if (!suspended && legGauge != null)
            {
                HandleValueChanged(legGauge.Value);
            }
        }

        private void Awake()
        {
            if (animator != null)
            {
                // 自動再生させず、常にこちらから再生位置を指定する
                animator.speed = 0f;
            }
        }

        private void OnEnable()
        {
            legGauge.OnValueChanged += HandleValueChanged;
            HandleValueChanged(legGauge.Value);
        }

        private void OnDisable()
        {
            legGauge.OnValueChanged -= HandleValueChanged;
        }

        private void HandleValueChanged(float value)
        {
            if (suspended) return;
            if (animator == null || string.IsNullOrEmpty(legStateName)) return;

            float normalizedTime = Mathf.Clamp01(invert ? 1f - value : value);
            animator.Play(legStateName, layerIndex, normalizedTime);
        }
    }
}
