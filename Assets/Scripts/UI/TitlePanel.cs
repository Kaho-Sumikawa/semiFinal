using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiFinal
{
    /// <summary>
    /// タイトル画面。「はじめる」ボタン(Space)でゲームを開始、「あそび方」ボタン(Tab)でチュートリアルを開始する。
    /// unityroomはカーソル非表示運用のため、クリックだけでなくキーボードでも操作できるようにしてある。
    /// ボタン自体のOnClick()に登録済みの処理(あそび方側の TutorialSequence.Play() など)を
    /// そのまま呼び出すだけなので、Inspector側の設定は変更不要。
    /// </summary>
    public class TitlePanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button startButton;
        [Tooltip("Tabキーでも押せるようにする「あそび方」ボタン。OnClick()側の設定はそのまま使う")]
        [SerializeField] private Button howToPlayButton;

        private void OnEnable()
        {
            if (startButton != null) startButton.onClick.AddListener(HandleStartClicked);
        }

        private void OnDisable()
        {
            if (startButton != null) startButton.onClick.RemoveListener(HandleStartClicked);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame && startButton != null)
            {
                startButton.onClick.Invoke();
            }

            if (keyboard.tabKey.wasPressedThisFrame && howToPlayButton != null)
            {
                howToPlayButton.onClick.Invoke();
            }
        }

        private void HandleStartClicked()
        {
            gameManager.StartGame();
        }
    }
}
