using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace RiftArena.CharacterSelect
{
    /// <summary>
    /// Drives the character select screen: cycles the roster, spawns the current
    /// character's model on the stage with its Idle animation playing, and on Confirm
    /// hands the pick to CharacterSelection before loading the battle scene.
    ///
    /// Both players pick on the same screen, one after the other - Player One confirms
    /// first, then Player Two gets their own independent pass over the same roster
    /// (picking the same character as Player One is allowed).
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        [SerializeField] private CharacterRoster roster;
        [SerializeField] private Transform stagePoint;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private string battleSceneName = "MVP1_Arena";

        private int currentIndex;
        private int currentPlayer;
        private GameObject currentInstance;

        private void Start()
        {
            currentIndex = 0;
            currentPlayer = 1;
            UpdatePrompt();
            ShowCharacter(currentIndex);
        }

        private void UpdatePrompt()
        {
            if (promptText != null) promptText.text = "PLAYER " + currentPlayer + " - SELECT YOUR FIGHTER";
        }

        public void Next()
        {
            currentIndex = (currentIndex + 1) % roster.characters.Length;
            ShowCharacter(currentIndex);
        }

        public void Previous()
        {
            currentIndex = (currentIndex - 1 + roster.characters.Length) % roster.characters.Length;
            ShowCharacter(currentIndex);
        }

        private void ShowCharacter(int index)
        {
            if (currentInstance != null) Destroy(currentInstance);

            CharacterDefinition def = roster.characters[index];
            currentInstance = Instantiate(def.modelPrefab, stagePoint.position, stagePoint.rotation, stagePoint);

            Animator animator = currentInstance.GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                if (def.animatorController != null)
                {
                    animator.runtimeAnimatorController = def.animatorController;
                }
                animator.CrossFade("Idle", 0.05f);
            }

            if (nameText != null) nameText.text = def.displayName;
        }

        public void Confirm()
        {
            if (currentPlayer == 1)
            {
                CharacterSelection.PlayerOneCharacterIndex = currentIndex;
                currentPlayer = 2;
                UpdatePrompt();
            }
            else
            {
                CharacterSelection.PlayerTwoCharacterIndex = currentIndex;
                SceneManager.LoadScene(battleSceneName);
            }
        }
    }
}
