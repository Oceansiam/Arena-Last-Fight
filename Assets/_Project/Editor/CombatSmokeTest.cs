using RiftArena.Character;
using RiftArena.Combat;
using RiftArena.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RiftArena.EditorTools
{
    /// <summary>
    /// Headless end-to-end combat verification. Opens the MVP1 arena scene, enters real
    /// Play Mode, then drives the actual production mechanism (Fighter.AttackState +
    /// HitboxComponent/HurtboxComponent overlap) rather than simulated keypresses, which
    /// are unavailable in batchmode:
    ///   1. Positions the two fighters close enough for Player1's hitbox to reach
    ///      Player2's hurtbox, then forces Player1 into AttackState with the LightPunch
    ///      MoveData via the same public API the Idle/Walk states use
    ///      (fighter.AttackState.SetMove + fighter.Machine.ChangeState).
    ///   2. Waits enough physics frames for startup+active+recovery to elapse and
    ///      asserts Player2's HealthComponent actually dropped by exactly the light
    ///      punch's damage (4).
    ///   3. Directly zeroes Player2's remaining HP via the same sanctioned
    ///      HealthComponent.ApplyDamage API real combat uses, then asserts the immediate
    ///      on-death reaction fired: both fighters frozen and the winner Text active with
    ///      the correct message (does not wait for the ~2s scene reload).
    /// Exits the whole Editor process with code 0 on pass, 1 on failure.
    /// </summary>
    public static class CombatSmokeTest
    {
        private const string ScenePath = "Assets/_Project/Scenes/Arenas/MVP1_Arena.unity";
        private const int SettleFrames = 30;
        private const int PostAttackFrames = 150; // generous margin over the light punch's 13-frame total

        private enum Phase { Settling, Attacking, WaitingForHit, Done }

        private static Phase phase = Phase.Settling;
        private static int frameCount;
        private static int attackFrameCount;
        private static Fighter p1Fighter;
        private static Fighter p2Fighter;
        private static HealthComponent p2Health;
        private static int p2HealthBeforePunch;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;

        public static void Run()
        {
            previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            phase = Phase.Settling;
            frameCount = 0;
            attackFrameCount = 0;

            EditorApplication.update += OnUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            {
                return;
            }

            switch (phase)
            {
                case Phase.Settling:
                    frameCount++;
                    if (frameCount < SettleFrames) return;

                    if (!TrySetupFighters())
                    {
                        Finish(false);
                        return;
                    }

                    // Move the two fighters close enough for Player1's Light Punch
                    // hitbox (local offset 0.9 in front, box half-extent 0.35) to reach
                    // Player2's hurtbox (half-extent 0.55) - comfortable overlap margin.
                    p1Fighter.Body.position = new Vector3(0f, p1Fighter.Body.position.y, 0f);
                    p2Fighter.Body.position = new Vector3(1.2f, p2Fighter.Body.position.y, 0f);
                    p1Fighter.Body.linearVelocity = Vector3.zero;
                    p2Fighter.Body.linearVelocity = Vector3.zero;

                    p2HealthBeforePunch = p2Health.CurrentHealth;

                    // Drive the real production mechanism directly - the exact same
                    // public API IdleState/WalkState use when W is pressed.
                    p1Fighter.AttackState.SetMove(p1Fighter.LightPunchMove);
                    p1Fighter.Machine.ChangeState(p1Fighter.AttackState);

                    phase = Phase.Attacking;
                    attackFrameCount = 0;
                    break;

                case Phase.Attacking:
                    attackFrameCount++;
                    if (attackFrameCount < PostAttackFrames) return;

                    bool hitLanded = VerifyPunchLanded();
                    if (!hitLanded)
                    {
                        Finish(false);
                        return;
                    }

                    bool deathFlowOk = VerifyDeathFlow();
                    Finish(deathFlowOk);
                    break;
            }
        }

        private static bool TrySetupFighters()
        {
            GameObject p1GO = GameObject.Find("PlayerOne");
            GameObject p2GO = GameObject.Find("PlayerTwo");
            if (p1GO == null || p2GO == null)
            {
                Debug.LogError("[CombatSmokeTest] Could not find PlayerOne and/or PlayerTwo.");
                return false;
            }

            p1Fighter = p1GO.GetComponent<Fighter>();
            p2Fighter = p2GO.GetComponent<Fighter>();
            p2Health = p2GO.GetComponent<HealthComponent>();

            if (p1Fighter == null || p2Fighter == null || p2Health == null)
            {
                Debug.LogError("[CombatSmokeTest] Missing Fighter/HealthComponent on one or both characters.");
                return false;
            }

            return true;
        }

        private static bool VerifyPunchLanded()
        {
            int expectedDamage = p1Fighter.LightPunchMove.damage; // 4, per MoveData asset
            int expected = p2HealthBeforePunch - expectedDamage;
            int actual = p2Health.CurrentHealth;

            Debug.Log($"[CombatSmokeTest] Light Punch: Player2 HP before={p2HealthBeforePunch}, after={actual}, expected={expected} (damage={expectedDamage}).");

            if (actual != expected)
            {
                Debug.LogError($"[CombatSmokeTest] FAIL - Light Punch did not apply the expected damage. before={p2HealthBeforePunch} after={actual} expectedDamage={expectedDamage}");
                return false;
            }

            return true;
        }

        private static bool VerifyDeathFlow()
        {
            bool ok = true;

            GameObject matchManagerGO = GameObject.Find("MatchManager");
            MatchManager matchManager = matchManagerGO != null ? matchManagerGO.GetComponent<MatchManager>() : null;
            if (matchManager == null)
            {
                Debug.LogError("[CombatSmokeTest] MatchManager not found.");
                return false;
            }

            GameObject winnerTextGO = GameObject.Find("WinnerText");
            Text winnerText = winnerTextGO != null ? winnerTextGO.GetComponent<Text>() : null;
            if (winnerText == null)
            {
                Debug.LogError("[CombatSmokeTest] WinnerText not found.");
                return false;
            }

            // Sanity: should still be hidden and both fighters still responsive before we
            // finish Player2 off.
            if (winnerTextGO.activeSelf)
            {
                Debug.LogError("[CombatSmokeTest] WinnerText was already active before the KO - test setup invalid.");
                ok = false;
            }

            // Zero out the remainder of Player2's HP via the same sanctioned public API
            // real combat uses (HealthComponent.ApplyDamage is the only legal way to
            // mutate HP). OnDeath fires synchronously inside this call.
            p2Health.ApplyDamage(p2Health.CurrentHealth);

            if (p2Health.CurrentHealth != 0 || !p2Health.IsDead)
            {
                Debug.LogError($"[CombatSmokeTest] Expected Player2 HP=0/IsDead=true after finishing blow, got HP={p2Health.CurrentHealth} IsDead={p2Health.IsDead}.");
                ok = false;
            }

            if (!p1Fighter.IsFrozen || !p2Fighter.IsFrozen)
            {
                Debug.LogError($"[CombatSmokeTest] Expected both fighters frozen after KO. p1.IsFrozen={p1Fighter.IsFrozen} p2.IsFrozen={p2Fighter.IsFrozen}");
                ok = false;
            }

            if (!winnerTextGO.activeSelf)
            {
                Debug.LogError("[CombatSmokeTest] Expected WinnerText to become active immediately on KO.");
                ok = false;
            }
            else if (winnerText.text != "PLAYER 1 WINS")
            {
                Debug.LogError($"[CombatSmokeTest] Expected WinnerText \"PLAYER 1 WINS\", got \"{winnerText.text}\".");
                ok = false;
            }

            if (ok)
            {
                Debug.Log("[CombatSmokeTest] Death/win flow verified: both fighters frozen, WinnerText active with \"PLAYER 1 WINS\".");
            }

            return ok;
        }

        private static void Finish(bool pass)
        {
            EditorApplication.update -= OnUpdate;
            phase = Phase.Done;

            EditorApplication.isPlaying = false;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;

            Debug.Log(pass ? "[CombatSmokeTest] RESULT: PASS" : "[CombatSmokeTest] RESULT: FAIL");
            EditorApplication.Exit(pass ? 0 : 1);
        }
    }
}
