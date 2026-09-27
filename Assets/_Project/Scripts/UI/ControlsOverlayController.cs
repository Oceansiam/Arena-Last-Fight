using UnityEngine;

namespace RiftArena.UI
{
    public class ControlsOverlayController : MonoBehaviour
    {
        [SerializeField] private GameObject controlsPanel;

        [Tooltip("How far right the panel sits when opened from the Pause menu's Controls button, so it doesn't cover the buttons.")]
        [SerializeField] private float pauseMenuOffsetX = 511f;

        private RectTransform panelRect;
        private float baseY;
        private bool openedFromPauseMenu;
        private bool wasPaused;

        private void Start()
        {
            panelRect = controlsPanel.GetComponent<RectTransform>();
            baseY = panelRect.anchoredPosition.y;
            controlsPanel.SetActive(false);
        }

        private void Update()
        {
            // C key: centered during a fight; off to the side if the pause menu is open.
            if (UnityEngine.Input.GetKeyDown(KeyCode.C))
            {
                bool paused = Time.timeScale == 0f;
                if (controlsPanel.activeSelf) HideControls();
                else Show(paused ? pauseMenuOffsetX : 0f, paused);
            }

            // Whenever the game pauses or unpauses (Esc, Resume button), close the panel,
            // so it never lingers over the fight or overlaps the pause menu.
            bool pausedNow = Time.timeScale == 0f;
            if (pausedNow != wasPaused)
            {
                wasPaused = pausedNow;
                HideControls();
            }
        }

        // Called by the "Controls" button in the Pause menu: shows the panel off to the side.
        public void ToggleControls()
        {
            if (controlsPanel.activeSelf) HideControls();
            else Show(pauseMenuOffsetX, true);
        }

        // Called by a "Back" / "Close" button, if you add one.
        public void HideControls()
        {
            controlsPanel.SetActive(false);
            openedFromPauseMenu = false;
        }

        private void Show(float x, bool fromPauseMenu)
        {
            panelRect.anchoredPosition = new Vector2(x, baseY);
            openedFromPauseMenu = fromPauseMenu;
            controlsPanel.SetActive(true);
        }
    }
}