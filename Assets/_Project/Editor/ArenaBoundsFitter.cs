using RiftArena.Character;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RiftArena.EditorTools
{
    /// <summary>
    /// Editor-only tool that measures the ring model's actual footprint and writes it
    /// into every Fighter's Arena Bounds fields, so the bounds never have to be
    /// hand-tuned by eyeballing numbers against the gizmo. Run via
    /// Window > Rift Arena > Fit Arena Bounds to Ring Model with the arena scene open
    /// and the ring model already placed.
    /// </summary>
    public static class ArenaBoundsFitter
    {
        // Shrinks the fitted rectangle inward from the ring's outer mesh bounds so
        // characters stop at the ropes/posts instead of clipping through them.
        private const float InwardMargin = 0.6f;

        [MenuItem("Rift Arena/Fit Arena Bounds to Ring Model")]
        public static void FitBounds()
        {
            GameObject ring = GameObject.Find("boxing");
            if (ring == null)
            {
                Debug.LogError("[ArenaBoundsFitter] No GameObject named 'boxing' found in the open scene. " +
                               "Make sure the ring model is in the Hierarchy (and named 'boxing') before running this.");
                return;
            }

            Renderer[] renderers = ring.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError("[ArenaBoundsFitter] 'boxing' has no Renderers under it - can't measure its size.");
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float minX = bounds.min.x + InwardMargin;
            float maxX = bounds.max.x - InwardMargin;
            float minZ = bounds.min.z + InwardMargin;
            float maxZ = bounds.max.z - InwardMargin;

            if (minX >= maxX || minZ >= maxZ)
            {
                Debug.LogError($"[ArenaBoundsFitter] Ring bounds are too small for the {InwardMargin} margin " +
                                $"(measured {bounds.size.x:F2} x {bounds.size.z:F2}). Lower InwardMargin and retry.");
                return;
            }

            Fighter[] fighters = Object.FindObjectsByType<Fighter>(FindObjectsInactive.Exclude);
            if (fighters.Length == 0)
            {
                Debug.LogError("[ArenaBoundsFitter] No Fighter components found in the scene.");
                return;
            }

            foreach (Fighter fighter in fighters)
            {
                var so = new SerializedObject(fighter);
                so.FindProperty("minX").floatValue = minX;
                so.FindProperty("maxX").floatValue = maxX;
                so.FindProperty("minZ").floatValue = minZ;
                so.FindProperty("maxZ").floatValue = maxZ;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(fighter);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[ArenaBoundsFitter] Ring measured as {bounds.size.x:F2} x {bounds.size.z:F2}. " +
                      $"Applied bounds X[{minX:F2}, {maxX:F2}] Z[{minZ:F2}, {maxZ:F2}] to {fighters.Length} Fighter(s). " +
                      "Save the scene (Cmd+S) to keep this.");
        }
    }
}
