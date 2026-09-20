using RiftArena.Character;
using RiftArena.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RiftArena.EditorTools
{
    /// <summary>
    /// Headless play-mode smoke test: opens the MVP1 arena scene, enters Play Mode,
    /// waits a few physics frames, then asserts a handful of basic facts (both
    /// HealthComponents exist and start at 100 HP, both characters sit on the fighting
    /// plane's Z=0). Exits the whole Editor process with code 0 on pass, 1 on failure,
    /// so it can be driven by `-executeMethod ... -logFile ...` from CI/Bash without a
    /// human watching.
    ///
    /// Domain reload is disabled for the transition into Play Mode purely so this
    /// static class's EditorApplication.update subscription survives the transition
    /// (a domain reload wipes static state, which would silently stop the test) -
    /// the setting is restored to its previous value before the process exits.
    /// </summary>
    public static class PlayModeSmokeTest
    {
        private const string ScenePath = "Assets/_Project/Scenes/Arenas/MVP1_Arena.unity";
        private const int FramesToWait = 30;

        private static int frameCount;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;

        public static void Run()
        {
            previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            frameCount = 0;
            EditorApplication.update += OnUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            {
                return;
            }

            frameCount++;

            if (frameCount < FramesToWait)
            {
                return;
            }

            EditorApplication.update -= OnUpdate;

            bool pass = RunAssertions();

            EditorApplication.isPlaying = false;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;

            Debug.Log(pass ? "[SmokeTest] RESULT: PASS" : "[SmokeTest] RESULT: FAIL");
            EditorApplication.Exit(pass ? 0 : 1);
        }

        private static bool RunAssertions()
        {
            bool ok = true;

            GameObject p1GO = GameObject.Find("PlayerOne");
            GameObject p2GO = GameObject.Find("PlayerTwo");

            if (p1GO == null || p2GO == null)
            {
                Debug.LogError("[SmokeTest] Could not find PlayerOne and/or PlayerTwo GameObjects.");
                return false;
            }

            Fighter fighter1 = p1GO.GetComponent<Fighter>();
            Fighter fighter2 = p2GO.GetComponent<Fighter>();
            if (fighter1 == null || fighter2 == null)
            {
                Debug.LogError("[SmokeTest] Missing Fighter component on one or both characters.");
                ok = false;
            }

            HealthComponent h1 = p1GO.GetComponent<HealthComponent>();
            HealthComponent h2 = p2GO.GetComponent<HealthComponent>();
            if (h1 == null || h2 == null)
            {
                Debug.LogError("[SmokeTest] Missing HealthComponent on one or both characters.");
                return false;
            }

            if (h1.MaxHealth != 100 || h2.MaxHealth != 100)
            {
                Debug.LogError($"[SmokeTest] MaxHealth expected 100/100, got {h1.MaxHealth}/{h2.MaxHealth}.");
                ok = false;
            }

            if (h1.CurrentHealth != 100 || h2.CurrentHealth != 100)
            {
                Debug.LogError($"[SmokeTest] CurrentHealth expected 100/100 after {FramesToWait} frames of no input, got {h1.CurrentHealth}/{h2.CurrentHealth}.");
                ok = false;
            }

            Rigidbody rb1 = p1GO.GetComponent<Rigidbody>();
            Rigidbody rb2 = p2GO.GetComponent<Rigidbody>();
            RigidbodyConstraints expected = RigidbodyConstraints.FreezeRotationX
                                             | RigidbodyConstraints.FreezeRotationY
                                             | RigidbodyConstraints.FreezeRotationZ;
            if (rb1 == null || rb2 == null || rb1.constraints != expected || rb2.constraints != expected)
            {
                Debug.LogError("[SmokeTest] Rigidbody constraints do not match the expected rotation lock.");
                ok = false;
            }

            GameObject matchManagerGO = GameObject.Find("MatchManager");
            if (matchManagerGO == null || matchManagerGO.GetComponent<RiftArena.Core.MatchManager>() == null)
            {
                Debug.LogError("[SmokeTest] MatchManager not found in scene.");
                ok = false;
            }

            if (ok)
            {
                Debug.Log($"[SmokeTest] After {FramesToWait} FixedUpdate frames with no input: " +
                          $"p1 pos={p1GO.transform.position} hp={h1.CurrentHealth}, " +
                          $"p2 pos={p2GO.transform.position} hp={h2.CurrentHealth}.");
            }

            return ok;
        }
    }
}
