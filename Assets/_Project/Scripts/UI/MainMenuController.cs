using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiftArena.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string fightSceneName = "MVP1_Arena";
        public void OnStartClicked()
        {
            SceneManager.LoadScene(fightSceneName);
        }

        public void OnQuitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}