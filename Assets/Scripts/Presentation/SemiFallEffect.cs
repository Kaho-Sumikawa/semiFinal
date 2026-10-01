using System.Collections;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// ゲームプレイ開始時、木に止まっているセミが地面(プレイ位置)へ落ちる演出。
    /// シーン開始時点でのSemiの位置・向きを「木の位置」として自動的に記憶し、
    /// GameManagerがPlaying状態になるたびにそこから groundPoint へ移動させる。
    /// GameManager側には一切手を加えない(状態変化を購読するだけ)。
    /// </summary>
    public class SemiFallEffect : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [Tooltip("落下させる対象(セミ本体のTransform)")]
        [SerializeField] private Transform semi;
        [Tooltip("落下後、プレイに使う着地位置・向き")]
        [SerializeField] private Transform groundPoint;

        [Header("演出設定")]
        [SerializeField] private float fallDuration = 0.6f;
        [Tooltip("落下中に何回転させるか(見た目の勢い付け。0でそのまま移動)")]
        [SerializeField] private float tumbleRotations = 0f;
        [Tooltip("落下の進み方。0=開始, 1=着地")]
        [SerializeField] private AnimationCurve fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 treePosition;
        private Quaternion treeRotation;
        private Coroutine fallRoutine;

        private void Awake()
        {
            // シーン開始時点のSemiの位置＝「木に止まっている状態」として記憶しておく
            if (semi != null)
            {
                treePosition = semi.position;
                treeRotation = semi.rotation;
            }
        }

        private void OnEnable()
        {
            gameManager.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            gameManager.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Playing) return;
            PlayFall();
        }

        /// <summary>
        /// 落下演出を再生する。ゲームプレイ開始時は自動で呼ばれるが、
        /// チュートリアルの各コマなど、他の場面から手動で呼び出すこともできる。
        /// </summary>
        public void PlayFall()
        {
            if (fallRoutine != null) StopCoroutine(fallRoutine);
            fallRoutine = StartCoroutine(FallRoutine());
        }

        /// <summary>
        /// 木の位置・向きへ即座に戻す。チュートリアルで落下デモを見せた後、
        /// そのコマの役目が終わったタイミング(onHide等)で呼んで初期状態に戻すために使う。
        /// </summary>
        public void ResetToTree()
        {
            if (fallRoutine != null)
            {
                StopCoroutine(fallRoutine);
                fallRoutine = null;
            }

            if (semi == null) return;
            semi.SetPositionAndRotation(treePosition, treeRotation);
        }

        private IEnumerator FallRoutine()
        {
            if (semi == null || groundPoint == null)
            {
                Debug.LogWarning("SemiFallEffect: semi / groundPoint が未設定です。", this);
                yield break;
            }

            // リトライ時などに備えて、毎回「木の位置」からやり直す
            semi.SetPositionAndRotation(treePosition, treeRotation);

            Vector3 toPos = groundPoint.position;
            Quaternion toRot = groundPoint.rotation;

            float t = 0f;
            while (t < fallDuration)
            {
                t += Time.deltaTime;
                float ratio = fallDuration > 0f ? Mathf.Clamp01(t / fallDuration) : 1f;
                float eased = fallCurve.Evaluate(ratio);

                semi.position = Vector3.Lerp(treePosition, toPos, eased);

                Quaternion tumble = Quaternion.Euler(0f, 0f, 360f * tumbleRotations * ratio);
                semi.rotation = Quaternion.Slerp(treeRotation, toRot, eased) * tumble;

                yield return null;
            }

            semi.SetPositionAndRotation(toPos, toRot);
            fallRoutine = null;
        }
    }
}
