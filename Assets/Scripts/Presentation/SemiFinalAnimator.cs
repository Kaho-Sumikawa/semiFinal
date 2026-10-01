using System.Collections;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// 発動(Enter/クリック)で、セミの「ファイナル」アニメーションを1ラウンドにつき一度だけ再生する。
    /// 一度発動したら、そのラウンド中の以降のEnter/クリックは無視する(使い切りの演出)。
    /// 再生中は位置・回転にランダムなジッターを加えて暴れているように見せ、
    /// SemiLegAnimatorによる足の開閉スクラブ更新は発動と同時に止めたままにする。
    /// GameManagerがPlaying状態になるたび(スタート/リトライ)、使い切り状態をリセットする。
    /// PlayerInputController / LegGauge / GameManager には一切手を加えない(状態を読むだけ)。
    /// </summary>
    public class SemiFinalAnimator : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PlayerInputController input;
        [SerializeField] private Animator animator;
        [Tooltip("発動と同時に足の開閉スクラブを止めるために参照する")]
        [SerializeField] private SemiLegAnimator legAnimator;
        [Tooltip("発動した瞬間の足の開閉度を読み取り、荒ぶり度に反映するために参照する")]
        [SerializeField] private LegGauge legGauge;

        [Tooltip("Animator上の「セミファイナル」アニメーションのState名。Loop Timeをオンにしておくこと")]
        [SerializeField] private string finalStateName = "アーマチュア|final";
        [SerializeField] private int layerIndex = 0;
        [Tooltip("暴れる時間(秒)。この間アニメーションはループし続ける")]
        [SerializeField] private float duration = 5f;

        [Header("暴れる動き (位置・回転のランダムジッター)")]
        [Tooltip("荒ぶり度が最大(intensity=1)の時の、元の位置からのランダムなずれ幅(メートル)")]
        [SerializeField] private float positionJitterRadius = 0.05f;
        [Tooltip("荒ぶり度が最大(intensity=1)の時の、元の向きからのランダムな回転幅(度)")]
        [SerializeField] private float rotationJitterDegrees = 15f;
        [Tooltip("発動した瞬間の足ゲージ値(X:0=全開〜1=全閉)から荒ぶり度(Y:0〜1)を求める。既定では全開ほど大きく、全閉ほど小さく荒ぶる")]
        [SerializeField]
        private AnimationCurve intensityByLegOpenness = AnimationCurve.Linear(0f, 1f, 1f, 0f);

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

        /// <summary>今のラウンドで、既にセミファイナルを発動したかどうか。</summary>
        public bool HasPlayed { get; private set; }

        private Coroutine playRoutine;

        private void OnEnable()
        {
            input.OnActivate += HandleActivate;
            gameManager.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            input.OnActivate -= HandleActivate;
            gameManager.OnStateChanged -= HandleStateChanged;
        }

        /// <summary>Playingになるたび(スタート/リトライ)、使い切り状態と足のスクラブ停止をリセットする。</summary>
        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Playing) return;

            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }

            HasPlayed = false;
            animator.speed = 0f;
            if (legAnimator != null) legAnimator.SetSuspended(false);
            if (ambientCicadaSfx != null) ambientCicadaSfx.Resume();
        }

        private void HandleActivate()
        {
            if (HasPlayed) return; // 一回きり。以降のEnter/クリックは完全に無視する
            if (animator == null || string.IsNullOrEmpty(finalStateName)) return;

            HasPlayed = true;
            playRoutine = StartCoroutine(PlayFinalRoutine());
        }

        private IEnumerator PlayFinalRoutine()
        {
            if (legAnimator != null) legAnimator.SetSuspended(true);
            if (ambientCicadaSfx != null) ambientCicadaSfx.Pause();

            // 発動した瞬間の足の開閉度をそのまま荒ぶり度として使う(以降ゲージが動いても更新しない)
            float intensity = legGauge != null ? Mathf.Clamp01(intensityByLegOpenness.Evaluate(legGauge.Value)) : 1f;
            float posRadius = positionJitterRadius * intensity;
            float rotDegrees = rotationJitterDegrees * intensity;

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

                Vector3 posOffset = Random.insideUnitSphere * posRadius;
                Vector3 rotOffset = Random.insideUnitSphere * rotDegrees;
                transform.SetPositionAndRotation(
                    basePosition + posOffset,
                    baseRotation * Quaternion.Euler(rotOffset));

                yield return null;
            }

            // 暴れ終わったら元の位置・向きに戻して静止させる。
            // 次にPlaying状態になる(リトライ含む)までは、このスクリプトも含めSemiには一切触れない。
            transform.SetPositionAndRotation(basePosition, baseRotation);
            animator.speed = 0f;
            playRoutine = null;
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
