using UnityEngine;
using UnityEngine.InputSystem;

namespace SemiFinal
{
    /// <summary>
    /// チュートリアル内の体験デモ用。Space連打でLegGaugeを操作できるようにするだけの、
    /// ゲーム進行(死亡判定・スコア・GameManager)とは完全に無関係な入力ドライバ。
    /// SetArmed(true)されるまではTick(自動で閉じる処理)自体を回さないので、
    /// Semiオブジェクトがチュートリアル序盤から既にアクティブでも、対象のコマに入るまで足は閉じない。
    /// </summary>
    public class TutorialLegGaugeDemo : MonoBehaviour
    {
        [SerializeField] private LegGauge legGauge;

        [Header("SE")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("連打のたびに鳴らす衝撃音。複数指定するとランダムで1つ再生する")]
        [SerializeField] private AudioClip[] mashClips;
        [Range(0f, 1f)] [SerializeField] private float mashVolume = 0.8f;
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        private bool armed;

        /// <summary>UnityEvent(対象コマのOnShow)から呼ぶ。trueで受付開始と同時にLegGaugeをリセットする。</summary>
        public void SetArmed(bool value)
        {
            armed = value;
            if (armed && legGauge != null)
            {
                legGauge.ResetGauge();
            }
        }

        private void Update()
        {
            if (!armed || legGauge == null) return;

            legGauge.Tick(Time.deltaTime);

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                legGauge.Mash();
                PlayMashSfx();
            }
        }

        private void PlayMashSfx()
        {
            if (mashClips == null || mashClips.Length == 0 || audioSource == null) return;

            AudioClip clip = mashClips[Random.Range(0, mashClips.Length)];
            audioSource.pitch = randomizePitch ? Random.Range(pitchRange.x, pitchRange.y) : 1f;
            audioSource.PlayOneShot(clip, mashVolume);
        }
    }
}
