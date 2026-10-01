using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// 指定したAudioClipを1回再生するだけの汎用SEプレイヤー。
    /// ボタンのOnClick()やUnityEvent(TutorialPanel.onShow等)から呼び出して使う。
    /// UnityEventのStatic Parameterで再生したいクリップを直接指定できるので、
    /// これ1つをCanvas等に置いておけば、ボタンごとにスクリプトを増やす必要はない。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [Range(0f, 1f)] [SerializeField] private float volume = 1f;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        /// <summary>UnityEventから呼ぶ。指定したクリップを1回再生する(重なって再生可)。</summary>
        public void PlayClip(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
