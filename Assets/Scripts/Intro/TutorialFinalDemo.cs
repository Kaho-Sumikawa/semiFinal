using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SemiFinal
{
    /// <summary>
    /// チュートリアル内で「セミファイナル」を体験させるデモ。
    /// SetArmed(true)された後、Enterキーで本編のSemiFinalAnimatorと同じような
    /// 暴れる演出を再生する。ゲーム進行(スコア・一度きり制限)とは無関係で、何度でも再生できる。
    /// </summary>
    public class TutorialFinalDemo : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Tooltip("再生中、足の開閉スクラブを止めるために参照する(任意)")]
        [SerializeField] private SemiLegAnimator legAnimator;

        [Tooltip("Animator上の「セミファイナル」アニメーションのState名")]
        [SerializeField] private string finalStateName = "アーマチュア|final";
        [SerializeField] private int layerIndex = 0;
        [SerializeField] private float duration = 3f;

        [Header("暴れる動き (位置・回転のランダムジッター)")]
        [SerializeField] private float positionJitterRadius = 0.05f;
        [SerializeField] private float rotationJitterDegrees = 15f;

        [Header("SE")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("暴れている間、繰り返し鳴らす「声」。複数指定するとそのたびランダムで1つ再生する")]
        [SerializeField] private AudioClip[] voiceClips;
        [Range(0f, 1f)] [SerializeField] private float voiceVolume = 1f;
        [Tooltip("声を鳴らす間隔(秒)")]
        [SerializeField] private float voiceRepeatInterval = 1f;
        [SerializeField] private bool randomizeVoicePitch = true;
        [SerializeField] private Vector2 voicePitchRange = new Vector2(0.95f, 1.1f);
        [Tooltip("暴れている間、鳴き声アンビエントSEと被らないよう一時的に止めておく(任意)")]
        [SerializeField] private AmbientCicadaSfx ambientCicadaSfx;

        private bool armed;
        private Coroutine routine;

        /// <summary>UnityEventから呼ぶ。trueでEnter入力の受付を開始する。</summary>
        public void SetArmed(bool value)
        {
            armed = value;
        }

        private void Update()
        {
            if (!armed) return;
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                Trigger();
            }
        }

        private void Trigger()
        {
            if (routine != null) return; // 再生中は多重起動しない
            if (animator == null || string.IsNullOrEmpty(finalStateName)) return;

            routine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            if (legAnimator != null) legAnimator.SetSuspended(true);
            if (ambientCicadaSfx != null) ambientCicadaSfx.Pause();

            Vector3 basePosition = transform.position;
            Quaternion baseRotation = transform.rotation;

            animator.speed = 1f;
            animator.Play(finalStateName, layerIndex, 0f);

            PlayVoice(); // 開始と同時に一声

            float t = 0f;
            float voiceTimer = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;

                voiceTimer += Time.deltaTime;
                if (voiceTimer >= voiceRepeatInterval)
                {
                    voiceTimer -= voiceRepeatInterval;
                    PlayVoice();
                }

                Vector3 posOffset = Random.insideUnitSphere * positionJitterRadius;
                Vector3 rotOffset = Random.insideUnitSphere * rotationJitterDegrees;
                transform.SetPositionAndRotation(
                    basePosition + posOffset,
                    baseRotation * Quaternion.Euler(rotOffset));

                yield return null;
            }

            transform.SetPositionAndRotation(basePosition, baseRotation);
            animator.speed = 0f;
            if (legAnimator != null) legAnimator.SetSuspended(false);
            if (ambientCicadaSfx != null) ambientCicadaSfx.Resume();
            routine = null;
        }

        private void PlayVoice()
        {
            if (voiceClips == null || voiceClips.Length == 0 || audioSource == null) return;

            AudioClip clip = voiceClips[Random.Range(0, voiceClips.Length)];
            audioSource.pitch = randomizeVoicePitch ? Random.Range(voicePitchRange.x, voicePitchRange.y) : 1f;
            audioSource.PlayOneShot(clip, voiceVolume);
        }
    }
}
