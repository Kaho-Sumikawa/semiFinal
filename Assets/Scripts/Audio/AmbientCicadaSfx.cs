using System.Collections;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// セミの単体の鳴き声などを、ランダムな間隔で繰り返し再生するアンビエントSE。
    /// タイトル画面やゲーム画面など、常時アクティブなオブジェクトに置いておくだけで鳴り続ける。
    /// 他のスクリプトには一切依存しない。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AmbientCicadaSfx : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] clips;
        [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;

        [Header("再生間隔(秒) この範囲でランダムに待つ")]
        [SerializeField] private float minInterval = 3f;
        [SerializeField] private float maxInterval = 8f;

        [Tooltip("毎回ピッチを少しランダムに変える")]
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

        private bool paused;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            StartCoroutine(PlayLoop());
        }

        /// <summary>他のSE(セミファイナルの声など)と被らないよう、一時的に鳴らなくする。</summary>
        public void Pause()
        {
            paused = true;
        }

        /// <summary>Pause()を解除する。</summary>
        public void Resume()
        {
            paused = false;
        }

        private IEnumerator PlayLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
                if (!paused) PlayRandomClip();
            }
        }

        private void PlayRandomClip()
        {
            if (clips == null || clips.Length == 0 || audioSource == null) return;

            // PlayOneShotだと前の声が鳴り終わる前に次が重なってしまうため、
            // Play()で「前の声を止めて差し替える」形にし、常に1つだけ鳴るようにする。
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.pitch = randomizePitch ? Random.Range(pitchRange.x, pitchRange.y) : 1f;
            audioSource.volume = volume;
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
