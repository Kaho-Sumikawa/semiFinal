using System;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// 足ゲージ：死なないための綱。
    /// 0 = 全開(安全) 〜 1 = 全閉(死)。時間経過で閉じる方向へ進み、連打(Mash)で開く方向へ戻る。
    /// MAX(1.0)に達すると死亡。
    /// </summary>
    public class LegGauge : MonoBehaviour
    {
        [Header("初期値")]
        [Tooltip("開始時点の閉じ度。0=全開/安全, 1=全閉/死")]
        [SerializeField, Range(0f, 1f)] private float startValue = 0.3f;

        [Header("バランス調整 (要件定義 3.9 の仮値。再生しながら回して決める)")]
        [Tooltip("1秒あたり自動で閉じる量")]
        [SerializeField] private float closeSpeedPerSecond = 0.10f;
        [Tooltip("1連打あたり開く量")]
        [SerializeField] private float openAmountPerMash = 0.03f;

        /// <summary>現在値。0=安全, 1=死。</summary>
        public float Value { get; private set; }
        public bool IsDead { get; private set; }

        /// <summary>MAXに達して死亡した瞬間に発火。</summary>
        public event Action OnDeath;
        /// <summary>値が変化するたびに発火(UI更新用)。</summary>
        public event Action<float> OnValueChanged;

        private void Awake()
        {
            ResetGauge();
        }

        /// <summary>ゲージを初期状態に戻す(タイトル→ゲーム開始時に呼ぶ)。</summary>
        public void ResetGauge()
        {
            Value = Mathf.Clamp01(startValue);
            IsDead = false;
            OnValueChanged?.Invoke(Value);
        }

        /// <summary>毎フレーム呼ぶ。時間経過で閉じていく処理。</summary>
        public void Tick(float deltaTime)
        {
            if (IsDead) return;
            SetValue(Value + closeSpeedPerSecond * deltaTime);
        }

        /// <summary>連打入力があるたびに呼ぶ。足を開く。</summary>
        public void Mash()
        {
            if (IsDead) return;
            SetValue(Value - openAmountPerMash);
        }

        private void SetValue(float v)
        {
            Value = Mathf.Clamp01(v);
            OnValueChanged?.Invoke(Value);

            if (Value >= 1f && !IsDead)
            {
                IsDead = true;
                OnDeath?.Invoke();
            }
        }
    }
}
