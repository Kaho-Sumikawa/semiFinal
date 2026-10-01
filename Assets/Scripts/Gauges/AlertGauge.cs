using System;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// 警戒ゲージ：バレないための綱。
    /// 0 = 安全 〜 1 = MAX(逃走)。連打(Mash)のたびに少し上がり、時間経過で自然減衰する。
    /// MAX(1.0)に達すると、その人に逃げられる(ソフト失敗)。
    /// </summary>
    public class AlertGauge : MonoBehaviour
    {
        [Header("バランス調整 (要件定義 3.9 の仮値。足ゲージが固まってから、この2つだけを調整する)")]
        [Tooltip("1連打あたり上がる量")]
        [SerializeField] private float risePerMash = 0.05f;
        [Tooltip("1秒あたり自然に下がる量(連打をやめると冷める)")]
        [SerializeField] private float decayPerSecond = 0.20f;

        /// <summary>現在値。0=安全, 1=MAX(逃走)。</summary>
        public float Value { get; private set; }
        public bool HasFled { get; private set; }

        /// <summary>MAXに達して逃げられた瞬間に発火。</summary>
        public event Action OnFlee;
        /// <summary>値が変化するたびに発火(UI更新用)。</summary>
        public event Action<float> OnValueChanged;

        private void Awake()
        {
            ResetGauge();
        }

        /// <summary>ゲージを0に戻す(ゲーム開始時、および1人逃げられた後の次の人へ向けて呼ぶ)。</summary>
        public void ResetGauge()
        {
            Value = 0f;
            HasFled = false;
            OnValueChanged?.Invoke(Value);
        }

        /// <summary>毎フレーム呼ぶ。自然減衰の処理。</summary>
        public void Tick(float deltaTime)
        {
            if (HasFled) return;
            SetValue(Value - decayPerSecond * deltaTime);
        }

        /// <summary>連打入力があるたびに呼ぶ。警戒を上げる。</summary>
        public void Mash()
        {
            if (HasFled) return;
            SetValue(Value + risePerMash);
        }

        /// <summary>強制的にMAXにして即座に逃走扱いにする(他の条件から確実に逃走させたい時に使う)。</summary>
        public void ForceFlee()
        {
            if (HasFled) return;
            SetValue(1f);
        }

        private void SetValue(float v)
        {
            Value = Mathf.Clamp01(v);
            OnValueChanged?.Invoke(Value);

            if (Value >= 1f && !HasFled)
            {
                HasFled = true;
                OnFlee?.Invoke();
            }
        }
    }
}
