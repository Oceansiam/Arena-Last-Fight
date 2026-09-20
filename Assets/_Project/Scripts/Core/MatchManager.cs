using System.Collections;
using RiftArena.Character;
using RiftArena.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RiftArena.Core
{
    /// <summary>
    /// Small end-of-match glue: watches both HealthComponents, and when either hits
    /// zero, freezes both characters' input, shows a centered "PLAYER X WINS" label,
    /// then reloads the current scene after a short delay to reset for the next bout.
    /// No round timer in MVP1 - explicitly out of scope.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        [SerializeField] private HealthComponent playerOneHealth;
        [SerializeField] private HealthComponent playerTwoHealth;
        [SerializeField] private Fighter playerOneFighter;
        [SerializeField] private Fighter playerTwoFighter;
        [SerializeField] private Text winnerText;
        [SerializeField] private float restartDelaySeconds = 2f;

        private bool matchOver;

        public void Configure(HealthComponent p1Health, HealthComponent p2Health,
            Fighter p1Fighter, Fighter p2Fighter, Text winnerLabel)
        {
            playerOneHealth = p1Health;
            playerTwoHealth = p2Health;
            playerOneFighter = p1Fighter;
            playerTwoFighter = p2Fighter;
            winnerText = winnerLabel;
        }

        private void OnEnable()
        {
            if (playerOneHealth != null) playerOneHealth.OnDeath += HandlePlayerOneDeath;
            if (playerTwoHealth != null) playerTwoHealth.OnDeath += HandlePlayerTwoDeath;
        }

        private void OnDisable()
        {
            if (playerOneHealth != null) playerOneHealth.OnDeath -= HandlePlayerOneDeath;
            if (playerTwoHealth != null) playerTwoHealth.OnDeath -= HandlePlayerTwoDeath;
        }

        private void HandlePlayerOneDeath() => EndMatch("PLAYER 2 WINS");
        private void HandlePlayerTwoDeath() => EndMatch("PLAYER 1 WINS");

        private void EndMatch(string message)
        {
            if (matchOver) return;
            matchOver = true;

            if (playerOneFighter != null) playerOneFighter.SetFrozen(true);
            if (playerTwoFighter != null) playerTwoFighter.SetFrozen(true);

            if (winnerText != null)
            {
                winnerText.text = message;
                winnerText.gameObject.SetActive(true);
            }

            StartCoroutine(ReloadAfterDelay());
        }

        private IEnumerator ReloadAfterDelay()
        {
            yield return new WaitForSeconds(restartDelaySeconds);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
