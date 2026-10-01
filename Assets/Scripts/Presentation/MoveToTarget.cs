using System.Collections;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// 指定した目標地点へ向かって、一定時間だけ移動させる汎用スクリプト。
    /// TutorialPanel.onShow などのUnityEvent経由で呼び出して使う。
    /// Root Motionに頼らず、Transformを直接動かす。
    /// </summary>
    public class MoveToTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float speed = 3f;
        [SerializeField] private float duration = 2f;
        [Tooltip("開始時に目標方向を自動で向く")]
        [SerializeField] private bool faceMoveDirection = true;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private Coroutine routine;

        private void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        /// <summary>UnityEventから呼ぶ。targetへ向かって移動を開始する。</summary>
        public void Play()
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(MoveRoutine());
        }

        /// <summary>
        /// 開始位置・向きへ即座に戻す。役目を終えたコマのonHide等から呼び、
        /// 次にこのチュートリアルを見た時にまた同じ位置から動けるようにするために使う。
        /// </summary>
        public void ResetPosition()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            transform.SetPositionAndRotation(startPosition, startRotation);
        }

        private IEnumerator MoveRoutine()
        {
            if (target == null) yield break;

            if (faceMoveDirection)
            {
                Vector3 dir = target.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;

                Vector3 dir = target.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    transform.position += dir.normalized * speed * Time.deltaTime;
                }

                yield return null;
            }

            routine = null;
        }
    }
}
