using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// ゲームプレイ中、連打(Space)のたびに衝撃音を鳴らす。
    /// PlayerInputController.OnMash を購読するだけで、GaugeやGameManagerには一切触れない。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MashSfx : MonoBehaviour
    {
        [SerializeField] private PlayerInputController input;
        [SerializeField] private AudioSource audioSource;
        [Tooltip("複数指定すると、連打のたびにランダムで1つ再生する(単調な音の繰り返しを避けるため)")]
        [SerializeField] private AudioClip[] clips;
        [Range(0f, 1f)] [SerializeField] private float volume = 0.8f;
        [Tooltip("毎回ピッチを少しランダムに変える(連打音のバリエーション付け)")]
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            input.OnMash += HandleMash;
        }

        private void OnDisable()
        {
            input.OnMash -= HandleMash;
        }

        private void HandleMash()
        {
            if (clips == null || clips.Length == 0 || audioSource == null) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.pitch = randomizePitch ? Random.Range(pitchRange.x, pitchRange.y) : 1f;
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
