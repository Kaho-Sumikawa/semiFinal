using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SemiFinal
{
    /// <summary>
    /// 入力担当。連打(Mash)と発動(Activate)を別ボタンにして暴発を防ぐ。
    /// 連打＝スペースキー / 発動＝Enterキーのみ(unityroomはカーソル非表示運用のため、クリックは使わない)。
    /// このプロジェクトは Active Input Handling が「Input System Package (New)」のため
    /// 新Input Systemの Keyboard.current を直接読む(Input Actionsアセット不要)。
    /// </summary>
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private bool inputEnabled = false;

        /// <summary>連打キーが押された瞬間に発火。</summary>
        public event Action OnMash;
        /// <summary>発動キーが押された瞬間に発火。</summary>
        public event Action OnActivate;

        private void Update()
        {
            if (!inputEnabled) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                OnMash?.Invoke();
            }

            if (keyboard.enterKey.wasPressedThisFrame)
            {
                OnActivate?.Invoke();
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }
    }
}
