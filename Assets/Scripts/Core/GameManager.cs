using System;
using System.Collections;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// ゲーム全体の進行役。足ゲージ/警戒ゲージ/接近する人/入力/スコアを束ね、
    /// タイトル→ゲーム→リザルトの状態遷移を管理する。
    /// UI側はこのクラスが発火するイベントを購読するだけで、GameManagerはUIを直接触らない。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private LegGauge legGauge;
        [SerializeField] private AlertGauge alertGauge;
        [SerializeField] private ApproachingPerson person;
        [SerializeField] private PlayerInputController input;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private JudgeSystem judgeSystem;

        [Header("演出の待ち時間(秒)")]
        [Tooltip("発動しないまま人が通り過ぎた時、リザルト画面に切り替わるまでの待ち時間")]
        [SerializeField] private float delayAfterMiss = 0.3f;
        [Tooltip("死亡が確定してからリザルト画面に切り替わるまでの待ち時間")]
        [SerializeField] private float delayBeforeResult = 0.6f;
        [Tooltip("警戒MAXで逃げられた後、RunAway演出を見せてからリザルト画面に切り替わるまでの待ち時間")]
        [SerializeField] private float delayAfterFlee = 5f;
        [Tooltip("セミファイナル発動後、暴れる演出を見せてからリザルト画面に切り替わるまでの待ち時間")]
        [SerializeField] private float delayAfterFinal = 5f;

        [Header("足の開閉に応じたスコア倍率 (X:LegGauge値 0=全開〜1=全閉, Y:倍率)")]
        [Tooltip("既定では中間(0.5)付近だけ倍率が高い、狭く鋭い山。ガバガバに感じたらキーフレームをドラッグしてさらに狭める/広げる")]
        [SerializeField]
        private AnimationCurve scoreMultiplierByLegOpenness = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.4f, 0.1f),
            new Keyframe(0.5f, 1f),
            new Keyframe(0.6f, 0.1f),
            new Keyframe(1f, 0f));

        public GameState State { get; private set; } = GameState.Title;
        /// <summary>今回のランがどう終わったか。State が Result になった時点で有効。</summary>
        public EndReason LastEndReason { get; private set; }
        /// <summary>直近の得点内訳。State が Result になった時点で有効。
        /// イベントの取りこぼし(ResultRootがアクティブ化されるのと購読が同時に起きる競合)を避けるため、
        /// UI側はイベントではなくこのプロパティを直接読みに行く。</summary>
        public ScoreBreakdown LastScoreBreakdown { get; private set; }

        /// <summary>状態が変わるたびに発火(画面切り替え用)。</summary>
        public event Action<GameState> OnStateChanged;
        /// <summary>発動判定が確定するたびに発火(判定カットイン用)。</summary>
        public event Action<JudgeTier, int> OnJudgeResult;
        /// <summary>ラウンドの得点が確定した時に発火(リザルトの内訳表示用)。</summary>
        public event Action<ScoreBreakdown> OnScoreBreakdown;
        /// <summary>警戒MAXで逃げられた瞬間に発火。</summary>
        public event Action OnFled;

        private Coroutine pendingRoutine;

        private void OnEnable()
        {
            legGauge.OnDeath += HandleDeath;
            alertGauge.OnFlee += HandleFlee;
            person.OnExited += HandlePersonExited;
            input.OnMash += HandleMash;
            input.OnActivate += HandleActivate;
        }

        private void OnDisable()
        {
            legGauge.OnDeath -= HandleDeath;
            alertGauge.OnFlee -= HandleFlee;
            person.OnExited -= HandlePersonExited;
            input.OnMash -= HandleMash;
            input.OnActivate -= HandleActivate;
        }

        private void Start()
        {
            input.SetInputEnabled(false);
            ChangeState(GameState.Title);
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            float dt = Time.deltaTime;
            legGauge.Tick(dt);
            alertGauge.Tick(dt);
            person.Tick(dt);
        }

        /// <summary>タイトル画面の「はじめる」ボタン、およびリザルト画面の「リトライ」ボタンから呼ぶ。</summary>
        public void StartGame()
        {
            StopPendingRoutine();

            scoreManager.ResetScore();
            legGauge.ResetGauge();
            alertGauge.ResetGauge();

            ChangeState(GameState.Playing);
            input.SetInputEnabled(true);
            person.SpawnNext();
        }

        /// <summary>タイトルに戻りたい時に呼ぶ(必要であれば利用)。</summary>
        public void BackToTitle()
        {
            StopPendingRoutine();
            input.SetInputEnabled(false);
            ChangeState(GameState.Title);
        }

        private void HandleMash()
        {
            if (State != GameState.Playing) return;
            legGauge.Mash();
            alertGauge.Mash();
        }

        private void HandleActivate()
        {
            if (State != GameState.Playing) return;
            if (!person.IsActive || person.IsResolved) return;

            JudgeTier tier = person.GetZoneTier();
            float judgeMultiplier = judgeSystem.GetMultiplier(tier);
            float opennessMultiplier = scoreMultiplierByLegOpenness.Evaluate(legGauge.Value);
            float multiplier = judgeMultiplier * opennessMultiplier;

            int gained = scoreManager.AddResult(multiplier);

            person.Resolve();
            OnJudgeResult?.Invoke(tier, gained);
            LastScoreBreakdown = new ScoreBreakdown(judgeMultiplier, opennessMultiplier, gained);
            OnScoreBreakdown?.Invoke(LastScoreBreakdown);

            // セミファイナルは一度きり。発動した時点でラウンド終了(リザルトへ)。
            EndGame(EndReason.Cleared, delayAfterFinal);
        }

        private void HandlePersonExited()
        {
            if (State != GameState.Playing) return;

            // 発動しないまま通り過ぎた＝範囲外でセミファイナルをした時と同じ扱い(0点・ラウンド終了)
            OnJudgeResult?.Invoke(JudgeTier.Miss, 0);
            LastScoreBreakdown = new ScoreBreakdown(0f, 0f, 0);
            OnScoreBreakdown?.Invoke(LastScoreBreakdown);
            EndGame(EndReason.Cleared, delayAfterMiss);
        }

        private void HandleFlee()
        {
            if (State != GameState.Playing) return;

            OnFled?.Invoke();
            person.PlayRunAway();
            EndGame(EndReason.Fled, delayAfterFlee);
        }

        private void HandleDeath()
        {
            if (State != GameState.Playing) return;

            EndGame(EndReason.Died, delayBeforeResult);
        }

        /// <summary>死亡・逃走・セミファイナル成功、どの終わり方でもここに合流する。少し待ってからリザルトへ切り替える。</summary>
        private void EndGame(EndReason reason, float delay)
        {
            StopPendingRoutine();
            input.SetInputEnabled(false);
            LastEndReason = reason;
            pendingRoutine = StartCoroutine(EndGameAfterDelay(delay));
        }

        private IEnumerator EndGameAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            pendingRoutine = null;
            scoreManager.SubmitToRanking();
            ChangeState(GameState.Result);
        }

        private void StopPendingRoutine()
        {
            if (pendingRoutine != null)
            {
                StopCoroutine(pendingRoutine);
                pendingRoutine = null;
            }
        }

        private void ChangeState(GameState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
