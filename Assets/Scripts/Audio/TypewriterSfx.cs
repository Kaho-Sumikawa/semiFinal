using SemiFinal.Intro;
using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// チュートリアルのタイプライター表示に合わせて、1文字ごとにクリック音を鳴らす。
    /// TutorialSequence.OnCharacterTyped を購読するだけで、他のスクリプトには一切依存しない。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class TypewriterSfx : MonoBehaviour
    {
        [SerializeField] private TutorialSequence tutorialSequence;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clip;
        [Range(0f, 1f)] [SerializeField] private float volume = 0.5f;
        [Tooltip("毎回ピッチを少しランダムに変える(単調なタイプ音を避けるため)")]
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (tutorialSequence != null) tutorialSequence.OnCharacterTyped += HandleCharacterTyped;
        }

        private void OnDisable()
        {
            if (tutorialSequence != null) tutorialSequence.OnCharacterTyped -= HandleCharacterTyped;
        }

        private void HandleCharacterTyped()
        {
            if (clip == null || audioSource == null) return;

            audioSource.pitch = randomizePitch ? Random.Range(pitchRange.x, pitchRange.y) : 1f;
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
