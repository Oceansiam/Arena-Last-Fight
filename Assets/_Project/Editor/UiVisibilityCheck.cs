using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RiftArena.EditorTools
{
    /// <summary>
    /// Headless pixel-level proof that the health bar UI actually renders.
    ///
    /// ScreenCapture.CaptureScreenshotAsTexture() turned out to be unreliable in this
    /// CLI/no-window batchmode environment on this machine: with -nographics it returns
    /// uninitialized garbage pixel data (confirmed visually - pure static noise, not a
    /// render of the scene), and without -nographics it returns null outright (no real
    /// display surface to read from). Neither is a real capture of the app.
    ///
    /// Instead this temporarily reparents the Canvas into Screen Space - Camera mode
    /// pointed at an explicit off-screen RenderTexture and calls Camera.Render() directly
    /// - a pure GPU off-screen render that does not depend on any OS window/display
    /// surface, which is the standard reliable way to get real pixel output in headless
    /// batchmode. The Canvas is restored to its original (Screen Space - Overlay) mode
    /// immediately after, and nothing is saved back to the scene file - this is a
    /// read-only diagnostic.
    /// </summary>
    public static class UiVisibilityCheck
    {
        private const string ScenePath = "Assets/_Project/Scenes/Arenas/MVP1_Arena.unity";
        private const int SettleFrames = 40;
        private const string OutputPath = "Logs/ui_visibility_capture.png";
        private const int CaptureWidth = 640;
        private const int CaptureHeight = 480;

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

            bool pass = CaptureAndCheck();

            EditorApplication.isPlaying = false;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;

            Debug.Log(pass ? "[UiVisibilityCheck] RESULT: PASS" : "[UiVisibilityCheck] RESULT: FAIL");
            EditorApplication.Exit(pass ? 0 : 1);
        }

        private static bool CaptureAndCheck()
        {
            GameObject canvasGO = GameObject.Find("Canvas");
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (canvasGO == null || cam == null)
            {
                Debug.LogError("[UiVisibilityCheck] Could not find Canvas and/or Main Camera.");
                return false;
            }

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            RenderMode originalMode = canvas.renderMode;
            UnityEngine.Camera originalWorldCamera = canvas.worldCamera;
            float originalPlaneDistance = canvas.planeDistance;
            RenderTexture originalCamTarget = cam.targetTexture;
            RenderTexture originalActive = RenderTexture.active;

            RenderTexture rt = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            Texture2D tex = null;
            bool ok;

            try
            {
                // Route the Canvas through the camera's render pass instead of the
                // Overlay compositor, into our own off-screen buffer.
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;

                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                tex = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                tex.Apply();

                ok = AnalyzeAndSave(tex);
            }
            finally
            {
                cam.targetTexture = originalCamTarget;
                RenderTexture.active = originalActive;
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalWorldCamera;
                canvas.planeDistance = originalPlaneDistance;
                rt.Release();
                Object.DestroyImmediate(rt);
            }

            return ok;
        }

        private static bool AnalyzeAndSave(Texture2D tex)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? "Logs");
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(OutputPath, png);
            Debug.Log($"[UiVisibilityCheck] Wrote off-screen capture to {OutputPath} ({tex.width}x{tex.height}, {png.Length} bytes).");

            // Quick global sanity check: if the whole frame is a single flat color (e.g.
            // pure black), the capture itself is broken/degenerate, not evidence of
            // anything about the UI specifically.
            Color corner = tex.GetPixel(2, 2);
            Color farCorner = tex.GetPixel(tex.width - 3, tex.height - 3);
            Color midSky = tex.GetPixel(tex.width / 2, tex.height - 5);
            bool looksDegenerate = corner == farCorner && farCorner == midSky;
            Debug.Log($"[UiVisibilityCheck] Sanity samples: corner={corner} farCorner={farCorner} midSky={midSky} degenerate={looksDegenerate}");
            if (looksDegenerate)
            {
                Debug.LogError("[UiVisibilityCheck] Capture looks degenerate (uniform flat color everywhere) - cannot use it as evidence either way.");
                return false;
            }

            // Player1 health bar: top-left anchor, sizeDelta (280,30), anchoredPosition
            // (160,-40), pivot 0.5. With Screen Space - Camera + CanvasScaler
            // (Scale With Screen Size, reference 1920x1080, MatchWidthOrHeight=0), the
            // canvas is sized in reference units and scaled by screenWidth/1920 to real
            // pixels, same math as Overlay mode.
            float scaleFactor = tex.width / 1920f;
            int barLeft = Mathf.RoundToInt((160f - 140f) * scaleFactor);
            int barRight = Mathf.RoundToInt((160f + 140f) * scaleFactor);
            int barTopFromTop = Mathf.RoundToInt((40f - 15f) * scaleFactor);
            int barBottomFromTop = Mathf.RoundToInt((40f + 15f) * scaleFactor);
            int yTop = tex.height - 1 - barTopFromTop;
            int yBottom = tex.height - 1 - barBottomFromTop;

            int barCenterX = Mathf.Clamp((barLeft + barRight) / 2, 0, tex.width - 1);
            int barCenterY = Mathf.Clamp((yTop + yBottom) / 2, 0, tex.height - 1);
            Color barPixel = tex.GetPixel(barCenterX, barCenterY);

            // Just outside the bar's rect (10px above its top edge in screen space) -
            // should be plain background if the bar has a real, bounded rect.
            int outsideY = Mathf.Clamp(yTop + 10, 0, tex.height - 1);
            Color justOutsidePixel = tex.GetPixel(barCenterX, outsideY);

            Debug.Log($"[UiVisibilityCheck] Bar rect in capture: x[{barLeft},{barRight}] y[{yBottom},{yTop}] (top-left origin).");
            Debug.Log($"[UiVisibilityCheck] Bar center pixel ({barCenterX},{barCenterY}): {barPixel}");
            Debug.Log($"[UiVisibilityCheck] Just-outside pixel ({barCenterX},{outsideY}): {justOutsidePixel}");

            float barLuma = barPixel.r + barPixel.g + barPixel.b;
            float outsideLuma = justOutsidePixel.r + justOutsidePixel.g + justOutsidePixel.b;
            float diff = outsideLuma - barLuma;

            Debug.Log($"[UiVisibilityCheck] barLuma={barLuma:F3} outsideLuma={outsideLuma:F3} diff={diff:F3}");

            if (diff < 0.05f)
            {
                Debug.LogError("[UiVisibilityCheck] FAIL - no meaningful darkening at the health bar's location vs. just outside its rect. The bar does not appear to be rendering.");
                return false;
            }

            Debug.Log("[UiVisibilityCheck] PASS - the health bar's rect reads distinctly darker than the area just outside it, consistent with the semi-transparent black bar actually rendering.");
            return true;
        }
    }
}
