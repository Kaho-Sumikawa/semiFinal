using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// 足ゲージが全開(見た目のアニメーションで言う0%側)になった瞬間、
    /// 警戒ゲージを強制的にMAXにして確実に逃走させる。
    /// 連打のリズム(開閉を繰り返す等)によって警戒の積み上げ計算だけでは
    /// 「全開になったのに逃げない/逃げていないのに逃げる」というズレが起きるため、
    /// 見た目の全開状態と逃走を直接結びつけるための橋渡し役。
    /// LegGauge / AlertGauge、どちらの中身にも手を加えない(両方のイベント/メソッドを使うだけ)。
    /// </summary>
    public class LegOpenTriggersFlee : MonoBehaviour
    {
        [SerializeField] private LegGauge legGauge;
        [SerializeField] private AlertGauge alertGauge;
        [Tooltip("この値以下になったら「全開」とみなす(0=完全に全開。少し手前で反応させたい場合は0.02などに)")]
        [SerializeField, Range(0f, 0.2f)] private float openThreshold = 0f;

        private void OnEnable()
        {
            legGauge.OnValueChanged += HandleLegValueChanged;
        }

        private void OnDisable()
        {
            legGauge.OnValueChanged -= HandleLegValueChanged;
        }

        private void HandleLegValueChanged(float value)
        {
            if (value <= openThreshold)
            {
                alertGauge.ForceFlee();
            }
        }
    }
}
