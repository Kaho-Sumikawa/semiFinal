using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// GameManagerの状態に応じてカメラを切り替える。
    /// タイトル画面とチュートリアル(あそび方)は共通のカメラを使い、
    /// ゲームプレイ中(Playing)だけ専用カメラに切り替える。
    /// ScreenFlowControllerと同じく、GameManagerのイベントを購読するだけで
    /// GameManager側には一切手を加えない。
    /// </summary>
    public class CameraFlowController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [Tooltip("タイトル画面・あそび方(チュートリアル)で共用するカメラ")]
        [SerializeField] private Camera titleAndTutorialCamera;
        [Tooltip("ゲームプレイ中だけ使う専用カメラ")]
        [SerializeField] private Camera gameplayCamera;
        [Tooltip("Title状態になった時、titleAndTutorialCameraをここへ戻す(未設定なら戻さない)。" +
                 "リザルトの「タイトルへ戻る」等、チュートリアルを経由しない経路でタイトルに戻った時のため")]
        [SerializeField] private Transform titleViewpoint;

        private void OnEnable()
        {
            gameManager.OnStateChanged += HandleStateChanged;
            HandleStateChanged(gameManager.State);
        }

        private void OnDisable()
        {
            gameManager.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            bool playing = state == GameState.Playing;
            if (titleAndTutorialCamera != null) titleAndTutorialCamera.gameObject.SetActive(!playing);
            if (gameplayCamera != null) gameplayCamera.gameObject.SetActive(playing);

            if (state == GameState.Title && titleAndTutorialCamera != null && titleViewpoint != null)
            {
                titleAndTutorialCamera.transform.SetPositionAndRotation(
                    titleViewpoint.position,
                    titleViewpoint.rotation);
            }
        }
    }
}
