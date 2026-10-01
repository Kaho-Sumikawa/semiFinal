using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiFinal.Intro
{
    /// <summary>紙芝居の1コマ。カメラの撮影ポイントとナレーション文のペア。Inspectorで自由に編集する。</summary>
    [System.Serializable]
    public class TutorialPanel
    {
        [Tooltip("このコマでカメラを移動させる先。位置と向きの両方を使う")]
        public Transform cameraViewpoint;
        [TextArea(2, 5)] public string narration;
        [Tooltip("このコマ専用のナレーション表示Text。Sceneビューであらかじめ好きな位置に配置しておいたものを割り当てる")]
        public TMP_Text narrationText;
        [Tooltip("このコマが表示された瞬間(カメラ移動直後・フェードイン開始と同時)に呼びたい処理があれば登録する。" +
                 "例：SemiFallEffect.PlayFall() をこのコマだけで再生する、など")]
        public UnityEvent onShow;
        [Tooltip("このコマを退出する瞬間(次のコマへフェードアウトし終わった直後、画面が黒い間)に呼びたい処理があれば登録する。" +
                 "例：SemiFallEffect.ResetToTree() で役目を終えたセミを初期位置へ戻す、など")]
        public UnityEvent onHide;
    }

    /// <summary>
    /// タイトルの「あそび方」から始まる導入チュートリアル。
    /// 静止画は使わず、シーンに実在するオブジェクトを専用カメラで撮影ポイント間を
    /// 瞬間移動しながら見せる。本編のゲームループとは完全に独立しており、他のスクリプトには依存しない。
    /// 呼び出し側は Play() で再生開始、完了/スキップ時に onComplete イベントが発火する。
    /// </summary>
    public class TutorialSequence : MonoBehaviour
    {
        [Header("紙芝居データ (コマ)")]
        [SerializeField]
        private List<TutorialPanel> panels = new List<TutorialPanel>
        {
            new TutorialPanel { narration = "セミが嫌いな男の子がいます。" },
            new TutorialPanel { narration = "あなたは、もうすぐ尽きるセミ。" },
            new TutorialPanel { narration = "連打（Space）して、死に抗え！" },
            new TutorialPanel { narration = "でも、足を開きすぎると男の子は逃げちゃう。" },
            new TutorialPanel { narration = "男の子が近づいたら……セミファイナル！（Enter）" },
        };

        [Header("参照 (カメラ)")]
        [Tooltip("タイトル画面と共用のカメラ。有効/無効の切り替えはCameraFlowControllerが行うので、" +
                 "ここでは各コマの cameraViewpoint へ移動させるだけ")]
        [SerializeField] private Camera titleAndTutorialCamera;
        [Tooltip("終了/スキップしてタイトルへ戻る時、titleAndTutorialCameraをここへ戻す(未設定なら戻さない)")]
        [SerializeField] private Transform titleViewpoint;

        [Header("参照 (UI)")]
        [Tooltip("画面全体を覆う黒Image用のCanvasGroup。カメラの瞬間移動をフェードで隠す")]
        [SerializeField] private CanvasGroup transitionCanvasGroup;
        [Tooltip("「クリックで進む」等のヒント表示。テキスト送り待ちの間だけ表示される")]
        [SerializeField] private GameObject advanceHint;
        [SerializeField] private Button skipButton;

        [Header("演出設定")]
        [Tooltip("カメラ瞬間移動を隠すフェードの時間(黒→透明、透明→黒 それぞれ)")]
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private float typeCharsPerSecond = 30f;

        [Header("完了時 (本編開始などをInspectorで自由に接続する)")]
        [SerializeField] private UnityEvent onComplete;

        [Header("SE")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("次へ(→キー)を押すたびに鳴らす")]
        [SerializeField] private AudioClip advanceClip;
        [Range(0f, 1f)] [SerializeField] private float advanceVolume = 1f;

        /// <summary>紙芝居を再生中かどうか。</summary>
        public bool IsPlaying => sequenceRoutine != null;
        /// <summary>タイプライター表示で1文字表示されるたびに発火(タイプ音などに使う)。</summary>
        public event Action OnCharacterTyped;

        private Coroutine sequenceRoutine;
        private bool advanceRequested;

        private void OnEnable()
        {
            if (skipButton != null) skipButton.onClick.AddListener(Skip);
        }

        private void OnDisable()
        {
            if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
        }

        private void Update()
        {
            if (!IsPlaying) return;

            if (WasAdvancePressed())
            {
                advanceRequested = true;
                if (audioSource != null && advanceClip != null)
                {
                    audioSource.PlayOneShot(advanceClip, advanceVolume);
                }
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Skip();
            }
        }

        private bool WasAdvancePressed()
        {
            var keyboard = Keyboard.current;

            // 送りは矢印キー(→)のみ。unityroomはカーソル非表示運用のためクリックは使わない。
            // Space/Enterはデモ(足の連打・セミファイナル体験)で使うため、送り操作からは外してある。
            if (keyboard != null && keyboard.rightArrowKey.wasPressedThisFrame)
                return true;
            return false;
        }

        /// <summary>タイトルの「あそび方」ボタンから呼ぶ。最初のコマから再生を開始する。</summary>
        public void Play()
        {
            if (panels == null || panels.Count == 0)
            {
                Finish();
                return;
            }

            gameObject.SetActive(true);

            // 開始前は真っ黒にしておき、最初のカメラ位置合わせを隠す
            if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 1f;
            advanceRequested = false;

            if (sequenceRoutine != null) StopCoroutine(sequenceRoutine);
            sequenceRoutine = StartCoroutine(RunSequence());
        }

        /// <summary>スキップボタン / Escキーから呼ぶ。再生中であれば即座に完了扱いにする。</summary>
        public void Skip()
        {
            if (!IsPlaying) return;

            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
            Finish();
        }

        private IEnumerator RunSequence()
        {
            for (int i = 0; i < panels.Count; i++)
            {
                yield return ShowPanel(panels[i]);
            }

            sequenceRoutine = null;
            Finish();
        }

        private IEnumerator ShowPanel(TutorialPanel panel)
        {
            // 画面は前のコマの終わりで黒に覆われている(最初のコマはPlay()で黒にしてある)。
            // 覆われている間にカメラを瞬間移動させ、移動の瞬間を見せない。
            if (titleAndTutorialCamera != null && panel.cameraViewpoint != null)
            {
                titleAndTutorialCamera.transform.SetPositionAndRotation(
                    panel.cameraViewpoint.position,
                    panel.cameraViewpoint.rotation);
            }

            if (panel.narrationText != null) panel.narrationText.text = string.Empty;
            if (advanceHint != null) advanceHint.SetActive(false);

            panel.onShow?.Invoke();

            yield return Fade(1f, 0f, fadeDuration); // 黒 → 透明 (フェードイン)

            advanceRequested = false;
            yield return TypeNarration(panel.narrationText, panel.narration);

            if (advanceHint != null) advanceHint.SetActive(true);
            yield return new WaitUntil(() => advanceRequested);
            advanceRequested = false;
            if (advanceHint != null) advanceHint.SetActive(false);

            yield return Fade(0f, 1f, fadeDuration); // 透明 → 黒 (次のカットへ向けてフェードアウト)

            // このコマ専用のテキストは、次のコマに移った後もそのままだと残り続けてしまうので、
            // 画面が黒く覆われている間に消しておく。
            if (panel.narrationText != null) panel.narrationText.text = string.Empty;

            // 役目を終えたものを初期位置に戻す等、他の後片付け
            panel.onHide?.Invoke();
        }

        private IEnumerator TypeNarration(TMP_Text targetText, string fullText)
        {
            if (targetText == null || string.IsNullOrEmpty(fullText))
            {
                if (targetText != null) targetText.text = fullText ?? string.Empty;
                yield break;
            }

            float secPerChar = typeCharsPerSecond > 0f ? 1f / typeCharsPerSecond : 0f;
            int shown = 0;
            targetText.text = string.Empty;

            while (shown < fullText.Length)
            {
                if (advanceRequested)
                {
                    // タイプ途中の入力＝全文を即時表示して終わる(送り自体には使わない)
                    advanceRequested = false;
                    targetText.text = fullText;
                    yield break;
                }

                shown++;
                targetText.text = fullText.Substring(0, shown);
                OnCharacterTyped?.Invoke();

                if (secPerChar <= 0f) continue;
                yield return new WaitForSeconds(secPerChar);
            }
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (transitionCanvasGroup == null) yield break;

            if (duration <= 0f)
            {
                transitionCanvasGroup.alpha = to;
                yield break;
            }

            float t = 0f;
            transitionCanvasGroup.alpha = from;
            while (t < duration)
            {
                t += Time.deltaTime;
                transitionCanvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            transitionCanvasGroup.alpha = to;
        }

        private void Finish()
        {
            // タイトルへ戻った時に、最後のコマの視点のままにならないよう、専用の視点があれば戻す
            if (titleAndTutorialCamera != null && titleViewpoint != null)
            {
                titleAndTutorialCamera.transform.SetPositionAndRotation(
                    titleViewpoint.position,
                    titleViewpoint.rotation);
            }

            gameObject.SetActive(false);
            onComplete?.Invoke();
        }
    }
}
