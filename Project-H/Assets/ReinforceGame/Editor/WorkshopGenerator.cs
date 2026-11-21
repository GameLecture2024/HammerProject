using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ReinforceGame.Editor
{
    // All scene, prefab, material and settings creation happens in the Editor.
    [InitializeOnLoad]
    public static class WorkshopGenerator
    {
        const string Root = "Assets/ReinforceGame";
        const string Pack = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)";
        const string SettingsPath = Root + "/Resources/ReinforceSettings.asset";
        const string WorkshopPath = Root + "/Prefabs/Workshop.prefab";
        const string ScenePath = Root + "/Scenes/Bootstrap.unity";
        static readonly string[] Products = { "sword_1handed", "axe_1handed", "shield_round", "staff", "dagger", "engineer_Wrench" };
        static Material wall, wood, metal, brass, teal;

        static WorkshopGenerator()
        {
            // Interactive projects use the explicit menu; do not mutate a user's unsaved scene on import.
        }
        static void GenerateIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += GenerateIfMissing; return; }
            if (!File.Exists(SettingsPath)) Generate();
        }

        [MenuItem("Reinforce Game/Generate Workshop")]
        public static void Generate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before generating.");
            Ensure(Root + "/Art"); Ensure(Root + "/Characters"); Ensure(Root + "/Products");
            Ensure(Root + "/Materials"); Ensure(Root + "/Resources"); Ensure(Root + "/Prefabs"); Ensure(Root + "/Scenes"); Ensure(Root + "/Validation");
            MoveSelection();
            var previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path))
                throw new InvalidOperationException("Save the current scene before generating, or open the generated Bootstrap scene.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode && string.IsNullOrEmpty(previous.path) ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            wall = Material("Charcoal concrete", new Color(.095f, .12f, .14f), .1f);
            wood = Material("Workbench oak", new Color(.22f, .105f, .045f), .05f);
            metal = Material("Black steel", new Color(.085f, .105f, .12f), .65f);
            brass = Material("Warm brass", new Color(.6f, .32f, .085f), .7f);
            teal = Material("Storage enamel", new Color(.04f, .24f, .25f), .35f);
            var settings = AssetDatabase.LoadAssetAtPath<ReinforceSettings>(SettingsPath);
            if (!settings) { settings = ScriptableObject.CreateInstance<ReinforceSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            settings.items = Products.Select((name, i) => new ItemDefinition
            {
                id = name, title = new[] { "Iron sword", "Forged axe", "Round shield", "Arcane staff", "Steel dagger", "Engineer wrench" }[i],
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Products/" + name + ".prefab"),
                basePrice = new[] { 35, 40, 32, 50, 25, 38 }[i], size = i == 3 ? .8f : .62f,
                displayEuler = new Vector3(0, 180, 0), gripEuler = new Vector3(0, 180, 0), benchEuler = new Vector3(90, 0, 0)
            }).ToArray();
            try
            {
                var room = new GameObject("Workshop");
                var game = room.AddComponent<WorkshopGame>();
                BuildRoom(room.transform);
                game.rig = BuildPlayer(room.transform);
                game.benchPivot = Pivot("Item mount - workbench", room.transform, new Vector3(0, 1.19f, 1.4f));
                var bench = Box("Workbench oak slab", room.transform, new Vector3(0, .94f, 1.5f), new Vector3(2.6f, .18f, 1.2f), wood);
                Station(bench, StationKind.Bench, game.benchPivot);
                for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Box("Bench steel leg", room.transform, new Vector3(x * 1.05f, .44f, 1.5f + z * .43f), new Vector3(.12f, .88f, .12f), metal);
                var plate = Box("REINFORCE", room.transform, new Vector3(0, 1.055f, 1.4f), new Vector3(1.1f, .05f, .65f), metal);
                Station(plate, StationKind.Bench, game.benchPivot);
                var take = Box("TAKE item", room.transform, new Vector3(-.95f, 1.09f, 1.04f), new Vector3(.42f, .12f, .28f), brass);
                Station(take, StationKind.Bench, game.benchPivot, 1);
                Label("TAKE", room.transform, new Vector3(-.95f, 1.18f, 1.02f), Quaternion.Euler(60, 0, 0), .045f, Color.white);
                var sell = Box("SELL tray", room.transform, new Vector3(1.0f, 1.07f, 1.45f), new Vector3(.48f, .08f, .55f), teal);
                Station(sell, StationKind.Sell, sell.transform);
                Label("SELL", room.transform, new Vector3(1f, 1.13f, 1.43f), Quaternion.Euler(60, 0, 0), .05f, new Color(.5f, 1, .85f));
                Label("REINFORCE", room.transform, new Vector3(0, 1.09f, 1.8f), Quaternion.Euler(65, 0, 0), .045f, new Color(1, .8f, .4f));
                game.supplyPivots = BuildRack(room.transform, false, 6);
                game.storagePivots = BuildRack(room.transform, true, 6);
                var next = Box("NEXT ROUND switch", room.transform, new Vector3(0, 1.65f, 2.82f), new Vector3(1.25f, .4f, .1f), teal);
                Station(next, StationKind.NextRound, next.transform);
                Label("NEXT ROUND", room.transform, new Vector3(0, 1.65f, 2.75f), Quaternion.identity, .065f, Color.white);
                Label("THE LAST FORGE", room.transform, new Vector3(0, 2.55f, 2.82f), Quaternion.identity, .11f, new Color(.8f, .65f, .4f));
                Label("MEET THE QUOTA. KEEP WHAT YOU DARE.", room.transform, new Vector3(0, 2.29f, 2.82f), Quaternion.identity, .032f, new Color(.6f, .65f, .65f));
                game.resultLight = Light("Strike flash", room.transform, new Vector3(0, 1.8f, 1.4f), Color.white, 0, 2, LightType.Point);
                BuildHUD(game, room.transform);
                settings.workshop = PrefabUtility.SaveAsPrefabAsset(room, WorkshopPath);
                UnityEngine.Object.DestroyImmediate(room);
                var boot = new GameObject("Bootstrap").AddComponent<Bootstrap>();
                var preview = (GameObject)PrefabUtility.InstantiatePrefab(settings.workshop);
                preview.name = "Editor Preview - replaced by Bootstrap at runtime";
                var serialized = new SerializedObject(boot);
                serialized.FindProperty("settings").objectReferenceValue = settings;
                serialized.FindProperty("editorPreview").objectReferenceValue = preview; serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
                File.WriteAllText(Root + "/Validation/generation.txt", "Generated with Unity Editor " + Application.unityVersion + "\n" + DateTime.UtcNow.ToString("O") + "\nScene: " + ScenePath + "\nProducts: " + settings.items.Length);
                Debug.Log("ReinforceGame generated. Open Assets/ReinforceGame/Scenes/Bootstrap.unity.");
            }
            finally
            {
                if (SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            Validate();
        }

        static void MoveSelection()
        {
            Move("Assets/KayKit/Art/Model/UpgradeHammer.fbx", Root + "/Art/UpgradeHammer.fbx");
            Move("Assets/KayKit/Art/Model/UpgradeHammer.png", Root + "/Art/UpgradeHammer.png");
            Move(Pack + "/Prefabs/Characters/Engineer.prefab", Root + "/Characters/Engineer.prefab");
            foreach (var name in Products) Move(Pack + "/Prefabs/Accessories/" + name + ".prefab", Root + "/Products/" + name + ".prefab");
            string[] selected = new[] { Root + "/Characters/Engineer.prefab", Root + "/Art/UpgradeHammer.fbx" }.Concat(Products.Select(x => Root + "/Products/" + x + ".prefab")).ToArray();
            foreach (string dependency in AssetDatabase.GetDependencies(selected, true))
            {
                if (!dependency.StartsWith("Assets/KayKit/", StringComparison.Ordinal) || AssetDatabase.IsValidFolder(dependency)) continue;
                string destination = Root + "/Art/Dependencies/" + dependency.Substring("Assets/KayKit/".Length);
                Ensure(Path.GetDirectoryName(destination).Replace('\\', '/'));
                Move(dependency, destination);
            }
        }
        static void Move(string from, string to)
        {
            if (!AssetDatabase.LoadMainAssetAtPath(from))
            {
                if (!AssetDatabase.LoadMainAssetAtPath(to)) throw new FileNotFoundException("Required asset missing: " + from);
                return;
            }
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
        }
        static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        static Material Material(string name, Color color, float metallic)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = color; material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", .3f); EditorUtility.SetDirty(material); return material;
        }
        static Transform Pivot(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform;
        }
        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static void Station(GameObject go, StationKind kind, Transform pivot, int index = 0)
        {
            var station = go.AddComponent<WorkshopStation>(); station.kind = kind; station.pivot = pivot; station.index = index;
        }
        static void Label(string text, Transform parent, Vector3 position, Quaternion rotation, float size, Color color)
        {
            var go = Pivot(text, parent, position); go.localRotation = rotation;
            var label = go.gameObject.AddComponent<TextMesh>(); label.text = text; label.characterSize = size; label.fontSize = 48;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = color;
        }
        static Light Light(string name, Transform parent, Vector3 position, Color color, float intensity, float range, LightType type)
        {
            var light = Pivot(name, parent, position).gameObject.AddComponent<Light>(); light.type = type; light.color = color;
            light.intensity = intensity; light.range = range; light.shadows = LightShadows.Soft; return light;
        }
        static void BuildRoom(Transform root)
        {
            Box("Sealed floor", root, new Vector3(0, -.1f, 0), new Vector3(6, .2f, 6), wall);
            Box("Sealed ceiling", root, new Vector3(0, 3.3f, 0), new Vector3(6, .2f, 6), wall);
            for (int i = -1; i <= 1; i += 2)
            {
                Box("Solid wall - no exit", root, new Vector3(i * 3, 1.6f, 0), new Vector3(.2f, 3.2f, 6), wall);
                Box("Solid wall - no exit", root, new Vector3(0, 1.6f, i * 3), new Vector3(6, 3.2f, .2f), wall);
            }
            for (int i = -2; i <= 2; i++)
                Box("Floor seam", root, new Vector3(i, .004f, 0), new Vector3(.012f, .008f, 6), metal);
            var lamp = Light("Workbench pendant", root, new Vector3(0, 2.85f, 1.2f), new Color(1, .73f, .42f), 9, 6, LightType.Spot);
            lamp.transform.rotation = Quaternion.Euler(90, 0, 0); lamp.spotAngle = 76;
            Box("Pendant housing", root, new Vector3(0, 2.93f, 1.2f), new Vector3(.65f, .12f, .35f), brass);
            Box("Pendant cable", root, new Vector3(0, 3.1f, 1.2f), new Vector3(.025f, .35f, .025f), metal);
            Light("Supply warm pool", root, new Vector3(-1.9f, 2.3f, .3f), new Color(1, .62f, .28f), 3, 3.2f, LightType.Point);
            Light("Storage cool pool", root, new Vector3(1.9f, 2.3f, .3f), new Color(.25f, .7f, 1), 3, 3.2f, LightType.Point);
            Light("Soft visibility fill", root, new Vector3(0, 2.2f, -.7f), new Color(.5f, .6f, .7f), .8f, 4, LightType.Point);
        }
        static Transform[] BuildRack(Transform root, bool storage, int count)
        {
            float side = storage ? 1 : -1;
            var rack = Pivot(storage ? "STORAGE - persistent mounts" : "SUPPLY - refills each round", root, new Vector3(side * 2.25f, 0, .35f));
            rack.localRotation = Quaternion.Euler(0, side * 90, 0);
            Box("Rack backing", rack, new Vector3(0, 1.5f, .2f), new Vector3(2.4f, 2.25f, .14f), storage ? teal : wood);
            var pivots = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                var position = new Vector3((i % 3 - 1) * .73f, i < 3 ? 1.95f : 1.05f, 0);
                pivots[i] = Pivot("Fixed mount " + (i + 1), rack, position);
                var hook = Box("Mount " + (i + 1), rack, position + new Vector3(0, -.3f, .07f), new Vector3(.62f, .08f, .32f), metal);
                if (storage) Station(hook, StationKind.Storage, pivots[i], i);
                // Empty storage slots have a broad click target behind the mount.
                if (storage)
                {
                    var target = Box("Storage slot " + (i + 1), rack, position + new Vector3(0, 0, .09f), new Vector3(.67f, .73f, .04f), teal);
                    Station(target, StationKind.Storage, pivots[i], i);
                }
            }
            Label(storage ? "KEEP / NEXT ROUND" : "SUPPLY / FREE", rack, new Vector3(0, 2.76f, .06f), Quaternion.identity, .065f, storage ? new Color(.45f, .9f, 1) : new Color(1, .8f, .45f));
            return pivots;
        }
        static FirstPersonRig BuildPlayer(Transform root)
        {
            var look = Pivot("Player look pivot", root, new Vector3(0, 1.6f, -.45f));
            var rig = look.gameObject.AddComponent<FirstPersonRig>();
            var camera = Pivot("First person camera", look, Vector3.zero).gameObject.AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.nearClipPlane = .12f; camera.farClipPlane = 30; camera.fieldOfView = 68;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.015f, .02f, .025f);
            camera.cullingMask = ~(1 << 31); camera.gameObject.AddComponent<AudioListener>(); rig.view = camera;
            var character = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Characters/Engineer.prefab"));
            character.transform.SetParent(look, false); character.transform.localPosition = new Vector3(0, -1.6f, 0);
            foreach (var animator in character.GetComponentsInChildren<Animator>()) animator.enabled = false;
            foreach (var renderer in character.GetComponentsInChildren<Renderer>())
            {
                renderer.gameObject.layer = renderer.name.Contains("Arm") ? 30 : 31;
                if (renderer.name.Contains("Arm")) renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (renderer is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = true;
                if (renderer.name == "Engineer_ArmLeft") rig.leftArm = renderer;
            }
            Transform Find(string name) => character.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            rig.rightUpper = Find("upperarm.r"); rig.rightLower = Find("lowerarm.r"); rig.rightHand = Find("hand.r");
            rig.leftUpper = Find("upperarm.l"); rig.leftLower = Find("lowerarm.l"); rig.leftHand = Find("hand.l");
            if (!rig.rightHand || !rig.leftHand) throw new InvalidOperationException("Engineer hand bones were not found.");
            FirstPersonRig.Solve(rig.rightUpper, rig.rightLower, rig.rightHand, look.TransformPoint(new Vector3(.32f, -.32f, .58f)), Vector3.down + Vector3.right);
            FirstPersonRig.Solve(rig.leftUpper, rig.leftLower, rig.leftHand, look.TransformPoint(new Vector3(-.32f, -.35f, .65f)), Vector3.down - Vector3.right);
            rig.hammerGrip = Pivot("Hammer grip socket", rig.rightHand, Vector3.zero);
            rig.itemGrip = Pivot("Item grip socket", rig.leftHand, Vector3.zero);
            var hammer = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/UpgradeHammer.fbx"));
            hammer.transform.SetParent(rig.hammerGrip, false);
            var bounds = new Bounds(); bool first = true;
            foreach (var renderer in hammer.GetComponentsInChildren<Renderer>()) { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
            float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            hammer.transform.localScale *= .4f / Mathf.Max(longest, .001f);
            hammer.transform.localPosition = new Vector3(0, .18f, .03f);
            foreach (var renderer in hammer.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.Off;
            foreach (var t in hammer.GetComponentsInChildren<Transform>()) t.gameObject.layer = 30;
            foreach (var collider in character.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var collider in hammer.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            return rig;
        }
        static void BuildHUD(WorkshopGame game, Transform root)
        {
            var canvasGo = new GameObject("Workshop HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(root, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            Text Text(string name, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(canvasGo.transform, false);
                var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
                var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.alignment = alignment;
                text.color = new Color(.94f, .91f, .8f); text.raycastTarget = false;
                var outline = go.AddComponent<Outline>(); outline.effectColor = new Color(0, 0, 0, .9f); outline.effectDistance = new Vector2(1, -1); return text;
            }
            game.statusText = Text("Quota", new Vector2(0, 1), new Vector2(32, -25), new Vector2(1100, 90), 25, TextAnchor.UpperLeft);
            game.itemText = Text("Item and risk", new Vector2(0, 0), new Vector2(32, 105), new Vector2(1450, 85), 24, TextAnchor.LowerLeft);
            game.helpText = Text("Controls", new Vector2(0, 0), new Vector2(32, 24), new Vector2(1450, 65), 19, TextAnchor.LowerLeft);
            game.messageText = Text("Outcome", new Vector2(.5f, 1), new Vector2(0, -130), new Vector2(1450, 60), 23, TextAnchor.MiddleCenter);
        }

        [MenuItem("Reinforce Game/Open Bootstrap")]
        public static void OpenBootstrap() => EditorSceneManager.OpenScene(ScenePath);

        [MenuItem("Reinforce Game/Reset Saved Progress")]
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey("ReinforceGame.Save.v1"); PlayerPrefs.Save();
            Debug.Log("ReinforceGame saved progress cleared. Restart Play mode for a fresh game.");
        }

        [MenuItem("Reinforce Game/Validate Generated Assets")]
        public static void Validate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ReinforceSettings>(SettingsPath);
            if (!settings || !settings.workshop || settings.items.Any(x => !x.prefab)) throw new InvalidOperationException("Incomplete settings or item references.");
            if (settings.successRates.Length == 0 || settings.destructionRates.Length == 0 || settings.priceMultipliers.Length < 2) throw new InvalidOperationException("Missing probability or price data.");
            var game = settings.workshop.GetComponent<WorkshopGame>();
            if (!game || !game.rig || !game.rig.view || !game.rig.hammerGrip || !game.rig.itemGrip || !game.benchPivot || game.supplyPivots.Length != 6 || game.storagePivots.Length != 6)
                throw new InvalidOperationException("Incomplete workshop references.");
            if ((game.rig.view.cullingMask & (1 << 31)) != 0) throw new InvalidOperationException("Body is visible to first-person camera.");
            string[] outside = AssetDatabase.GetDependencies(WorkshopPath, true).Where(x => x.StartsWith("Assets/") && !x.StartsWith(Root + "/")).ToArray();
            if (outside.Length > 0) throw new InvalidOperationException("Dependencies outside ReinforceGame: " + string.Join(", ", outside));
            File.WriteAllText(Root + "/Validation/validation.txt", "PASS: prefab references, player rig, 6 supply mounts, 6 storage mounts, camera body culling, self-contained asset dependencies.\n" + DateTime.UtcNow.ToString("O"));
            Debug.Log("ReinforceGame validation passed.");
        }
    }
}
