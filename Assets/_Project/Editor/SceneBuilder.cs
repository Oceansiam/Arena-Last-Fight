using System.IO;
using RiftArena.Camera;
using RiftArena.Character;
using RiftArena.Combat;
using RiftArena.Core;
using RiftArena.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RiftArena.EditorTools
{
    /// <summary>
    /// Editor-only automation that builds the MVP1 grey-box arena scene from scratch:
    /// ground, walls, light, two wired-up Fighter capsules, camera, UI, and MatchManager.
    /// Run via Window > Rift Arena > Build MVP1 Arena Scene, or headlessly via
    /// `-executeMethod RiftArena.EditorTools.SceneBuilder.BuildScene`.
    /// </summary>
    public static class SceneBuilder
    {
        private const string SceneDir = "Assets/_Project/Scenes/Arenas";
        private const string ScenePath = SceneDir + "/MVP1_Arena.unity";
        private const string MoveDataDir = "Assets/_Project/ScriptableObjects/MoveData";

        [MenuItem("Rift Arena/Build MVP1 Arena Scene")]
        public static void BuildScene()
        {
            EnsureFolder(MoveDataDir);
            EnsureFolder(SceneDir);

            MoveData lightPunch = CreateOrLoadMoveData(
                "LightPunch", "light_punch", damage: 4, knockback: 3f,
                startup: 4, active: 2, recovery: 7,
                description: "Fast jab. Low damage, small knockback.");

            MoveData heavyPunch = CreateOrLoadMoveData(
                "HeavyPunch", "heavy_punch", damage: 10, knockback: 8f,
                startup: 13, active: 4, recovery: 21,
                description: "Slow, hard-hitting punch. High damage, large knockback.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildGround();
            BuildWalls();
            BuildLight();

            Fighter p1Fighter = BuildFighter(
                name: "PlayerOne", isPlayerOne: true, spawnX: -3f,
                lightPunch: lightPunch, heavyPunch: heavyPunch);

            Fighter p2Fighter = BuildFighter(
                name: "PlayerTwo", isPlayerOne: false, spawnX: 3f,
                lightPunch: lightPunch, heavyPunch: heavyPunch);

            WireOpponents(p1Fighter, p2Fighter);

            GameObject cameraGO = BuildCamera(p1Fighter.transform, p2Fighter.transform);

            BuildUiAndMatchManager(p1Fighter, p2Fighter);

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RegisterSceneInBuildSettings(ScenePath);

            Debug.Log($"[SceneBuilder] Arena scene build complete. Saved={saved} Path={ScenePath}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static MoveData CreateOrLoadMoveData(string assetName, string id, int damage, float knockback,
            int startup, int active, int recovery, string description)
        {
            string path = $"{MoveDataDir}/{assetName}.asset";
            MoveData existing = AssetDatabase.LoadAssetAtPath<MoveData>(path);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<MoveData>();
                AssetDatabase.CreateAsset(existing, path);
            }

            existing.id = id;
            existing.damage = damage;
            existing.knockback = knockback;
            existing.startupFrames = startup;
            existing.activeFrames = active;
            existing.recoveryFrames = recovery;
            existing.description = description;

            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(4f, 1f, 4f); // 40x40 units
        }

        private static void BuildWalls()
        {
            CreateWall("LeftWall", new Vector3(-9f, 2f, 0f));
            CreateWall("RightWall", new Vector3(9f, 2f, 0f));
        }

        private static void CreateWall(string name, Vector3 position)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = new Vector3(1f, 4f, 10f);

            // Purely a visual bound for the play space - no collision for MVP1.
            var collider = wall.GetComponent<BoxCollider>();
            if (collider != null) Object.DestroyImmediate(collider);
        }

        private static void BuildLight()
        {
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static Fighter BuildFighter(string name, bool isPlayerOne, float spawnX, MoveData lightPunch, MoveData heavyPunch)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.position = new Vector3(spawnX, 1f, 0f);

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            // Rotation lock only - X/Z ground movement is free and clamped by Fighter's
            // own arena-bounds check instead of a physics-level Z lock.
            rb.constraints = RigidbodyConstraints.FreezeRotationX
                              | RigidbodyConstraints.FreezeRotationY
                              | RigidbodyConstraints.FreezeRotationZ;

            HealthComponent health = go.AddComponent<HealthComponent>();

            HurtboxComponent hurtbox = go.AddComponent<HurtboxComponent>();
            BoxCollider hurtboxCollider = go.GetComponent<BoxCollider>();
            hurtboxCollider.isTrigger = true;
            hurtboxCollider.center = new Vector3(0f, 0f, 0f);
            hurtboxCollider.size = new Vector3(1.1f, 2f, 1.1f);

            GameObject hitboxGO = new GameObject("Hitbox");
            hitboxGO.transform.SetParent(go.transform, false);
            hitboxGO.transform.localPosition = new Vector3(0.9f, 0f, 0f);
            BoxCollider hitboxCollider = hitboxGO.AddComponent<BoxCollider>();
            hitboxCollider.isTrigger = true;
            hitboxCollider.size = new Vector3(0.7f, 0.6f, 0.7f);
            HitboxComponent hitbox = hitboxGO.AddComponent<HitboxComponent>();

            Fighter fighter = go.AddComponent<Fighter>();

            var so = new SerializedObject(fighter);
            so.FindProperty("isPlayerOne").boolValue = isPlayerOne;
            so.FindProperty("moveSpeed").floatValue = 5f;
            so.FindProperty("jumpVelocity").floatValue = 8f;
            so.FindProperty("minX").floatValue = -8f;
            so.FindProperty("maxX").floatValue = 8f;
            so.FindProperty("minZ").floatValue = -4f;
            so.FindProperty("maxZ").floatValue = 4f;
            so.FindProperty("lightPunchMove").objectReferenceValue = lightPunch;
            so.FindProperty("heavyPunchMove").objectReferenceValue = heavyPunch;
            so.FindProperty("hitbox").objectReferenceValue = hitbox;
            so.FindProperty("hurtbox").objectReferenceValue = hurtbox;
            so.FindProperty("health").objectReferenceValue = health;
            so.ApplyModifiedPropertiesWithoutUndo();

            return fighter;
        }

        private static void WireOpponents(Fighter p1, Fighter p2)
        {
            HurtboxComponent p1Hurtbox = p1.GetComponent<HurtboxComponent>();
            HurtboxComponent p2Hurtbox = p2.GetComponent<HurtboxComponent>();

            p1.SetOpponent(p2.transform);
            p2.SetOpponent(p1.transform);

            p1.SetTargetHurtbox(p2Hurtbox);
            p2.SetTargetHurtbox(p1Hurtbox);

            EditorUtility.SetDirty(p1);
            EditorUtility.SetDirty(p2);
        }

        private static GameObject BuildCamera(Transform p1, Transform p2)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            UnityEngine.Camera cam = camGO.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camGO.AddComponent<AudioListener>();

            CombatCamera combatCamera = camGO.AddComponent<CombatCamera>();
            combatCamera.SetPlayers(p1, p2);
            camGO.transform.position = new Vector3(0f, 2.6f, -6f);
            camGO.transform.LookAt(new Vector3(0f, 2.6f, 0f));

            EditorUtility.SetDirty(combatCamera);
            return camGO;
        }

        private static void BuildUiAndMatchManager(Fighter p1Fighter, Fighter p2Fighter)
        {
            // IMPORTANT: must be created with typeof(RectTransform) explicitly. Creating a
            // plain GameObject and letting AddComponent<Canvas> auto-swap its Transform for
            // a RectTransform leaves m_LocalScale at (0,0,0), which invisibly collapses
            // every UI child in the hierarchy - this was a real bug caught in review.
            GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }

            Slider p1Slider = BuildHealthSlider(canvasGO.transform, "PlayerOneHealthSlider",
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                anchoredPos: new Vector2(160f, -40f), fillColor: new Color(0.2f, 0.7f, 1f));
            HealthBarUI p1Bar = p1Slider.GetComponent<HealthBarUI>();
            p1Bar.SetTarget(p1Fighter.Health);
            // HealthComponent.Awake() (which sets CurrentHealth = maxHealth) hasn't
            // necessarily run yet at edit-authoring time, so force the visible/serialized
            // starting value here too - purely a cleaner authored-scene preview, runtime
            // already self-corrects via HealthBarUI.OnEnable() after Awake order.
            p1Slider.value = 100f;

            Slider p2Slider = BuildHealthSlider(canvasGO.transform, "PlayerTwoHealthSlider",
                anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
                anchoredPos: new Vector2(-160f, -40f), fillColor: new Color(1f, 0.3f, 0.3f));
            HealthBarUI p2Bar = p2Slider.GetComponent<HealthBarUI>();
            p2Bar.SetTarget(p2Fighter.Health);
            p2Slider.value = 100f;

            Text winnerText = BuildWinnerText(canvasGO.transform);

            GameObject matchManagerGO = new GameObject("MatchManager");
            MatchManager matchManager = matchManagerGO.AddComponent<MatchManager>();
            matchManager.Configure(p1Fighter.Health, p2Fighter.Health, p1Fighter, p2Fighter, winnerText);
            EditorUtility.SetDirty(matchManager);
        }

        private static Slider BuildHealthSlider(Transform canvasParent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Color fillColor)
        {
            GameObject sliderGO = new GameObject(name, typeof(RectTransform));
            sliderGO.transform.SetParent(canvasParent, false);
            RectTransform rootRect = sliderGO.GetComponent<RectTransform>();
            rootRect.anchorMin = anchorMin;
            rootRect.anchorMax = anchorMax;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = anchoredPos;
            rootRect.sizeDelta = new Vector2(280f, 30f);

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;

            GameObject background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(sliderGO.transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.6f);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGO.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(4f, 4f);
            fillAreaRect.offsetMax = new Vector2(-4f, -4f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
            slider.direction = Slider.Direction.LeftToRight;

            sliderGO.AddComponent<HealthBarUI>();

            return slider;
        }

        private static Text BuildWinnerText(Transform canvasParent)
        {
            GameObject textGO = new GameObject("WinnerText", typeof(RectTransform));
            textGO.transform.SetParent(canvasParent, false);
            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 120f);

            Text text = textGO.AddComponent<Text>();
            text.text = "PLAYER 1 WINS";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            textGO.SetActive(false);

            return text;
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            EditorBuildSettings.scenes = scenes;
        }
    }
}
