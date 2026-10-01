using System;
using System.Collections;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// 画面奥から近づいてくる人。
    /// spawnPoint → exitPoint へ一直線に等速移動する。
    /// targetPoint(セミ)に最も近づく時刻を出現時に解析的に計算しておき、
    /// その時刻との差でジャスト判定を行う。
    /// 「近いほど高得点」は targetPoint との現在距離から算出する(CurrentDistanceFactor)。
    /// </summary>
    public class ApproachingPerson : MonoBehaviour
    {
        [Header("移動経路 (シーン上の空オブジェクトを割り当てる)")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;
        [Tooltip("セミの位置。この点への距離が近いほど高得点になる")]
        [SerializeField] private Transform targetPoint;

        [Header("見た目")]
        [SerializeField] private Animator animator;

        [Header("移動速度 (m/s)")]
        [SerializeField] private float moveSpeed = 1.5f;

        [Header("Animator の State 名 (Animator ウィンドウの実際の名前と合わせる)")]
        [SerializeField] private string walkStateName = "Walking";
        [Tooltip("驚きが一番大きい(一番近い)時の反応")]
        [SerializeField] private string perfectReactionState = "Flying Back Death";
        [Tooltip("驚きが中くらいの時の反応")]
        [SerializeField] private string greatReactionState = "Falling Back Death";
        [Tooltip("驚きが小さめの時の反応")]
        [SerializeField] private string goodReactionState = "Sitting Dodges";
        [Tooltip("警戒MAXで逃げられた時の反応(振り返って逃げるアニメーション)")]
        [SerializeField] private string runAwayStateName = "RunAway";

        [Header("驚きパターンの距離ゾーン (シーン上に空オブジェクトを置き、targetPointからの距離で管理)")]
        [Tooltip("targetPointからここまでの距離より近ければPerfectReactionState")]
        [SerializeField] private Transform perfectZoneMarker;
        [Tooltip("targetPointからここまでの距離より近ければGreatReactionState")]
        [SerializeField] private Transform greatZoneMarker;
        [Tooltip("targetPointからここまでの距離より近ければGoodReactionState。それより遠ければIdleState")]
        [SerializeField] private Transform goodZoneMarker;

        [Header("RunAway(逃走)時の移動 (Root Motionを使わず、スクリプトでspawnPointへ後退させる)")]
        [SerializeField] private float runAwaySpeed = 3f;
        [Tooltip("逃げ続ける時間(秒)")]
        [SerializeField] private float runAwayDuration = 2f;

        [Header("Perfect時のジャンプ演出 (Animatorとは別に位置を一時的に持ち上げる)")]
        [SerializeField] private float perfectBounceHeight = 0.3f;
        [SerializeField] private float perfectBounceDuration = 0.4f;
        [Tooltip("X:0〜1(経過割合) Y:高さの倍率。既定では山なりに上がって降りる")]
        [SerializeField]
        private AnimationCurve perfectBounceCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f));

        /// <summary>出現してからの経過時間(秒)。</summary>
        public float ElapsedTime { get; private set; }
        /// <summary>targetPointに最も近づく時刻(秒)。発動タイミングの基準。</summary>
        public float ClosestTime { get; private set; }
        /// <summary>この人が画面を通過しきるまでの合計時間(秒)。</summary>
        public float DurationTotal { get; private set; }
        /// <summary>0=遠い, 1=最短距離。スコアの距離係数として使う。</summary>
        public float CurrentDistanceFactor { get; private set; }
        /// <summary>現在、接近〜通過の最中かどうか。</summary>
        public bool IsActive { get; private set; }
        /// <summary>発動済み(判定確定済み)かどうか。</summary>
        public bool IsResolved { get; private set; }

        /// <summary>発動されないまま通り過ぎた時に発火。</summary>
        public event Action OnExited;

        private float spawnDistanceToTarget;
        private Coroutine runAwayRoutine;

        /// <summary>次の人を出現させる。spawnPointに配置し直して移動を開始する。</summary>
        public void SpawnNext()
        {
            if (spawnPoint == null || exitPoint == null || targetPoint == null)
            {
                Debug.LogWarning("ApproachingPerson: spawnPoint / exitPoint / targetPoint が未設定です。", this);
                return;
            }

            if (runAwayRoutine != null)
            {
                StopCoroutine(runAwayRoutine);
                runAwayRoutine = null;
            }

            ElapsedTime = 0f;
            IsActive = true;
            IsResolved = false;

            float totalDistance = Vector3.Distance(spawnPoint.position, exitPoint.position);
            DurationTotal = totalDistance / Mathf.Max(moveSpeed, 0.0001f);

            // spawn→exit の移動方向を求め、位置だけでなく「その方向を向いた状態」でスポーンさせる。
            // spawnPoint自体の向きには依存しないので、spawnPoint側の回転を厳密に合わせる必要はない。
            Vector3 dir = exitPoint.position - spawnPoint.position;
            float dirLen = dir.magnitude;
            Vector3 dirNorm = dirLen > 0.0001f ? dir / dirLen : Vector3.forward;

            Quaternion facing = dirNorm.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(dirNorm)
                : spawnPoint.rotation;
            transform.SetPositionAndRotation(spawnPoint.position, facing);

            float projected = Vector3.Dot(targetPoint.position - spawnPoint.position, dirNorm);
            projected = Mathf.Clamp(projected, 0f, dirLen);
            ClosestTime = projected / Mathf.Max(moveSpeed, 0.0001f);

            spawnDistanceToTarget = Vector3.Distance(spawnPoint.position, targetPoint.position);

            PlayState(walkStateName);
            UpdateDistanceFactor();
        }

        /// <summary>毎フレーム呼ぶ。移動と距離係数の更新。</summary>
        public void Tick(float deltaTime)
        {
            if (!IsActive) return;

            ElapsedTime += deltaTime;
            float ratio = DurationTotal > 0f ? Mathf.Clamp01(ElapsedTime / DurationTotal) : 1f;
            transform.position = Vector3.Lerp(spawnPoint.position, exitPoint.position, ratio);
            UpdateDistanceFactor();

            if (ElapsedTime >= DurationTotal)
            {
                IsActive = false;
                if (!IsResolved)
                {
                    OnExited?.Invoke();
                }
            }
        }

        private void UpdateDistanceFactor()
        {
            float dist = Vector3.Distance(transform.position, targetPoint.position);
            CurrentDistanceFactor = spawnDistanceToTarget > 0.0001f
                ? Mathf.Clamp01(1f - dist / spawnDistanceToTarget)
                : 0f;
        }

        /// <summary>
        /// 発動判定が確定した時にGameManagerから呼ぶ。反応アニメーションを再生する。
        /// 驚きパターンは GetZoneTier() と同じ距離ゾーン判定から選ぶ(近いほど大きく驚く)。
        /// どのゾーンにも入っていない(遠すぎる)場合は反応させず、そのまま歩き続けさせる。
        /// </summary>
        public void Resolve()
        {
            IsResolved = true;

            JudgeTier tier = GetZoneTier();
            bool reacted = PlayReactionByTier(tier);
            if (reacted)
            {
                IsActive = false; // その場に留まって反応を見せる
            }
            // 反応しなかった場合はIsActiveをtrueのままにして、Tick()による移動を継続させる
        }

        /// <summary>
        /// 現在の実距離を、シーン上のゾーンマーカーと比較してPerfect/Great/Good/Missを判定する。
        /// 反応アニメーションの選択とスコアの判定倍率、両方でこの結果を共通して使う
        /// (見た目の反応と得点が食い違わないようにするため)。
        /// </summary>
        public JudgeTier GetZoneTier()
        {
            float currentDist = DistanceToTarget(transform.position);

            if (IsWithinZone(currentDist, perfectZoneMarker)) return JudgeTier.Perfect;
            if (IsWithinZone(currentDist, greatZoneMarker)) return JudgeTier.Great;
            if (IsWithinZone(currentDist, goodZoneMarker)) return JudgeTier.Good;
            return JudgeTier.Miss;
        }

        /// <summary>反応アニメーションを再生できたら true、Missならどのゾーンにも該当しないので false。</summary>
        private bool PlayReactionByTier(JudgeTier tier)
        {
            string stateName;
            bool bounce = false;
            switch (tier)
            {
                case JudgeTier.Perfect: stateName = perfectReactionState; bounce = true; break;
                case JudgeTier.Great: stateName = greatReactionState; break;
                case JudgeTier.Good: stateName = goodReactionState; break;
                default: return false; // Miss＝どのゾーンより遠い＝気づかずそのまま歩き続ける
            }

            FaceTarget();
            PlayState(stateName);
            if (bounce) StartCoroutine(BounceRoutine());
            return true;
        }

        /// <summary>targetPoint(セミ)の方向を向く(水平方向のみ、見上げ/見下ろしはしない)。</summary>
        private void FaceTarget()
        {
            if (targetPoint == null) return;

            Vector3 direction = targetPoint.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(direction);
        }

        private IEnumerator BounceRoutine()
        {
            Vector3 basePosition = transform.position;

            float t = 0f;
            while (t < perfectBounceDuration)
            {
                t += Time.deltaTime;
                float ratio = perfectBounceDuration > 0f ? Mathf.Clamp01(t / perfectBounceDuration) : 1f;
                float heightOffset = perfectBounceCurve.Evaluate(ratio) * perfectBounceHeight;
                transform.position = basePosition + Vector3.up * heightOffset;
                yield return null;
            }

            transform.position = basePosition;
        }

        /// <summary>
        /// 警戒ゲージMAXで逃げられた時にGameManagerから呼ぶ。
        /// spawnPointの方向へ振り返らせ、逃げるアニメーションを再生しながら
        /// (Root Motionを使わず)スクリプトでその場から後退させる。
        /// </summary>
        public void PlayRunAway()
        {
            IsResolved = true;
            IsActive = false;

            FaceAwayFromTarget();
            PlayState(runAwayStateName);

            if (runAwayRoutine != null) StopCoroutine(runAwayRoutine);
            runAwayRoutine = StartCoroutine(RunAwayRoutine());
        }

        /// <summary>spawnPointの方向(＝逃げる方向)を向く(水平方向のみ)。</summary>
        private void FaceAwayFromTarget()
        {
            if (spawnPoint == null) return;

            Vector3 direction = spawnPoint.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(direction);
        }

        private IEnumerator RunAwayRoutine()
        {
            if (spawnPoint == null) yield break;

            float t = 0f;
            while (t < runAwayDuration)
            {
                t += Time.deltaTime;

                Vector3 direction = spawnPoint.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    transform.position += direction.normalized * runAwaySpeed * Time.deltaTime;
                }

                yield return null;
            }

            runAwayRoutine = null;
        }

        /// <summary>現在距離が、指定したゾーンマーカーの距離以内(＝マーカーと同じかそれより近い)かどうか。</summary>
        private bool IsWithinZone(float currentDist, Transform zoneMarker)
        {
            if (zoneMarker == null) return false;
            return currentDist <= DistanceToTarget(zoneMarker.position);
        }

        private float DistanceToTarget(Vector3 position)
        {
            if (targetPoint == null) return float.MaxValue;
            return Vector3.Distance(position, targetPoint.position);
        }

        private void PlayState(string stateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return;
            animator.CrossFadeInFixedTime(stateName, 0.1f);
        }
    }
}
