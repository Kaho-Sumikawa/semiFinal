using UnityEngine;

namespace SemiFinal
{
    /// <summary>
    /// GameManagerの状態に応じて、タイトル/ゲーム/リザルトの3パネル(GameObject)を
    /// SetActiveで切り替える。1シーン内で3画面を表現するための最小構成。
    /// </summary>
    public class ScreenFlowController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject titleRoot;
        [SerializeField] private GameObject gameRoot;
        [SerializeField] private GameObject resultRoot;

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
            if (titleRoot != null) titleRoot.SetActive(state == GameState.Title);
            if (gameRoot != null) gameRoot.SetActive(state == GameState.Playing);
            if (resultRoot != null) resultRoot.SetActive(state == GameState.Result);
        }
    }
}
