using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RiftArena.EditorTools
{
    /// <summary>
    /// Crash-safe headless diagnostic (no RenderTexture/Camera.Render() - those proved
    /// unstable in this environment's -nographics batchmode). Opens the MVP1 arena
    /// scene, enters real Play Mode, then queries Unity's own live UI state directly:
    /// CanvasRenderer culling/alpha, computed RectTransform world corners, Canvas scale
    /// factor, and Graphic enabled/color for the Player1 health bar's Background and
    /// Fill images. This tells us definitively whether Unity itself considers these
    /// graphics visible and correctly sized, without depending on any pixel capture.
    /// </summary>
    public static class UiStateDump
    {
        private const string ScenePath = "Assets/_Project/Scenes/Arenas/MVP1_Arena.unity";
        private const int SettleFrames = 40;

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
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;

            frameCount++;
            if (frameCount < SettleFrames) return;

            EditorApplication.update -= OnUpdate;

            bool pass = Dump();

            EditorApplication.isPlaying = false;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;

            Debug.Log(pass ? "[UiStateDump] RESULT: PASS" : "[UiStateDump] RESULT: FAIL");
            EditorApplication.Exit(pass ? 0 : 1);
        }

        private static bool Dump()
        {
            bool ok = true;

            GameObject canvasGO = GameObject.Find("Canvas");
            GameObject sliderGO = GameObject.Find("PlayerOneHealthSlider");
            if (canvasGO == null || sliderGO == null)
            {
                Debug.LogError("[UiStateDump] Canvas or PlayerOneHealthSlider not found.");
                return false;
            }

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            Debug.Log($"[UiStateDump] Canvas: enabled={canvas.enabled} isActiveAndEnabled={canvas.isActiveAndEnabled} renderMode={canvas.renderMode} scaleFactor={canvas.scaleFactor} pixelRect={canvas.pixelRect} rootCanvas={canvas.rootCanvas.name}");

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            Debug.Log($"[UiStateDump] Canvas RectTransform.rect={canvasRect.rect} worldCorners=[{canvasCorners[0]},{canvasCorners[1]},{canvasCorners[2]},{canvasCorners[3]}]");

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            Debug.Log($"[UiStateDump] CanvasScaler: uiScaleMode={scaler.uiScaleMode} referenceResolution={scaler.referenceResolution} matchWidthOrHeight={scaler.matchWidthOrHeight} -> computed scaleFactor on canvas={canvas.scaleFactor}");

            RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
            Vector3[] sliderCorners = new Vector3[4];
            sliderRect.GetWorldCorners(sliderCorners);
            Debug.Log($"[UiStateDump] Slider GO active={sliderGO.activeInHierarchy} RectTransform.rect={sliderRect.rect} worldCorners=[{sliderCorners[0]},{sliderCorners[1]},{sliderCorners[2]},{sliderCorners[3]}]");

            bool sliderDegenerate = sliderRect.rect.width <= 0.01f || sliderRect.rect.height <= 0.01f;
            if (sliderDegenerate)
            {
                Debug.LogError($"[UiStateDump] FAIL - Slider RectTransform.rect is degenerate (near-zero size): {sliderRect.rect}");
                ok = false;
            }

            Transform backgroundT = sliderGO.transform.Find("Background");
            Transform fillAreaT = sliderGO.transform.Find("Fill Area");
            Transform fillT = fillAreaT != null ? fillAreaT.Find("Fill") : null;

            ok &= DumpGraphic("Background", backgroundT);
            // "Fill Area" is intentionally a plain layout container (RectTransform only,
            // no Image) - matches Unity's own default Slider template structure.
            DumpGraphic("Fill Area", fillAreaT, requireGraphic: false);
            ok &= DumpGraphic("Fill", fillT);

            Slider slider = sliderGO.GetComponent<Slider>();
            Debug.Log($"[UiStateDump] Slider component: value={slider.value} minValue={slider.minValue} maxValue={slider.maxValue} fillRect={(slider.fillRect != null ? slider.fillRect.name : "NULL")} targetGraphic={(slider.targetGraphic != null ? slider.targetGraphic.name : "NULL")}");

            return ok;
        }

        private static bool DumpGraphic(string label, Transform t, bool requireGraphic = true)
        {
            if (t == null)
            {
                Debug.LogError($"[UiStateDump] FAIL - child '{label}' not found under PlayerOneHealthSlider.");
                return false;
            }

            bool ok = true;
            GameObject go = t.gameObject;
            RectTransform rt = t.GetComponent<RectTransform>();
            Graphic graphic = t.GetComponent<Graphic>();
            CanvasRenderer cr = t.GetComponent<CanvasRenderer>();

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            string graphicInfo = graphic != null
                ? $"type={graphic.GetType().Name} enabled={graphic.enabled} isActiveAndEnabled={graphic.isActiveAndEnabled} color={graphic.color}"
                : "NO GRAPHIC COMPONENT";

            string crInfo = cr != null
                ? $"cull={cr.cull} GetAlpha={cr.GetAlpha()} GetColor={cr.GetColor()}"
                : "NO CANVASRENDERER";

            Debug.Log($"[UiStateDump] '{label}': GO.activeInHierarchy={go.activeInHierarchy} rect={rt.rect} worldCorners=[{corners[0]},{corners[2]}] {graphicInfo} | {crInfo}");

            if (!go.activeInHierarchy)
            {
                Debug.LogError($"[UiStateDump] FAIL - '{label}' GameObject is not active in hierarchy.");
                ok = false;
            }
            if (requireGraphic && (graphic == null || !graphic.enabled || !graphic.isActiveAndEnabled))
            {
                Debug.LogError($"[UiStateDump] FAIL - '{label}' Graphic is missing/disabled.");
                ok = false;
            }
            if (graphic != null && graphic.color.a <= 0.001f)
            {
                Debug.LogError($"[UiStateDump] FAIL - '{label}' Graphic color alpha is ~0 (invisible): {graphic.color}");
                ok = false;
            }
            if (cr != null && cr.cull)
            {
                Debug.LogError($"[UiStateDump] FAIL - '{label}' CanvasRenderer.cull is true (Unity itself is culling/not drawing it).");
                ok = false;
            }
            if (cr != null && cr.GetAlpha() <= 0.001f)
            {
                Debug.LogError($"[UiStateDump] FAIL - '{label}' CanvasRenderer.GetAlpha() is ~0 (invisible).");
                ok = false;
            }
            if (rt.rect.width <= 0.01f || rt.rect.height <= 0.01f)
            {
                Debug.LogError($"[UiStateDump] FAIL - '{label}' RectTransform.rect is degenerate: {rt.rect}");
                ok = false;
            }

            return ok;
        }
    }
}
