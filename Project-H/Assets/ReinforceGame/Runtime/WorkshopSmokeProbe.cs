#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ReinforceGame
{
    public sealed class WorkshopSmokeProbe : MonoBehaviour
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        const string Key = "ReinforceGame.Save.v1";
        string backup;
        bool hadSave;
        readonly List<string> passed = new List<string>();
        WorkshopGame game;
        ReinforceSettings settings;
        float[] successBackup, destructionBackup;
        object Call(string method, params object[] args) => typeof(WorkshopGame).GetMethod(method, Flags).Invoke(game, args);
        T Get<T>(string name) => (T)typeof(WorkshopGame).GetField(name, Flags).GetValue(game);
        void Set(string name, object value) => typeof(WorkshopGame).GetField(name, Flags).SetValue(game, value);
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            passed.Add(name);
        }
        void Start() { StartCoroutine(Guard()); }
        IEnumerator Guard()
        {
            var run = Run();
            while (true)
            {
                object next;
                try { if (!run.MoveNext()) break; next = run.Current; }
                catch (Exception exception)
                {
                    Restore();
                    File.WriteAllText("Assets/ReinforceGame/Validation/smoke-failure.txt", exception.ToString());
                    Debug.LogException(exception); EditorApplication.Exit(1); yield break;
                }
                yield return next;
            }
            Restore();
            File.WriteAllText("Assets/ReinforceGame/Validation/smoke-pass.txt", string.Join("\n", passed));
            EditorApplication.Exit(0);
        }
        IEnumerator Run()
        {
            yield return null;
            game = FindFirstObjectByType<WorkshopGame>();
            Check(game != null, "Bootstrap instantiated the workshop from settings");
            settings = Get<ReinforceSettings>("settings");
            successBackup = settings.successRates.ToArray(); destructionBackup = settings.destructionRates.ToArray();
            hadSave = PlayerPrefs.HasKey(Key); backup = PlayerPrefs.GetString(Key);
            // Start the probe from a clean round without altering an existing player save.
            foreach (var item in Get<List<WorkshopItem>>("inventory").ToArray()) Call("Remove", item);
            Set("round", 1); Set("revenue", 0); Call("Refill");
            Check(Get<List<WorkshopItem>>("inventory").Count == 6, "Six free products spawned at fixed supply pivots");
            var itemA = Get<List<WorkshopItem>>("inventory")[0];
            Call("Pickup", itemA);
            Check(itemA.transform.parent == game.rig.itemGrip, "Selected product grips to the Engineer left hand");
            var bench = game.GetComponentsInChildren<WorkshopStation>().First(x => x.kind == StationKind.Bench && x.index == 0);
            var take = game.GetComponentsInChildren<WorkshopStation>().First(x => x.kind == StationKind.Bench && x.index == 1);
            var storage = game.GetComponentsInChildren<WorkshopStation>().First(x => x.kind == StationKind.Storage && x.index == 0);
            var sell = game.GetComponentsInChildren<WorkshopStation>().First(x => x.kind == StationKind.Sell);
            var next = game.GetComponentsInChildren<WorkshopStation>().First(x => x.kind == StationKind.NextRound);
            Call("UseStation", bench);
            Check(itemA.transform.parent == game.benchPivot, "Held item places on workbench pivot");
            settings.successRates = Enumerable.Repeat(1f, 8).ToArray(); settings.destructionRates = new float[8];
            game.StartCoroutine((IEnumerator)Call("Reinforce")); yield return new WaitForSeconds(1);
            Check(itemA.level == 1, "A successful strike increases upgrade level");
            settings.successRates = new float[8];
            game.StartCoroutine((IEnumerator)Call("Reinforce")); yield return new WaitForSeconds(1);
            Check(itemA.level == 0, "A downgrade decreases upgrade level");
            game.StartCoroutine((IEnumerator)Call("Reinforce")); yield return new WaitForSeconds(1);
            Check(itemA.level == 0, "Downgrade at zero remains at zero");
            settings.destructionRates = Enumerable.Repeat(1f, 8).ToArray();
            game.StartCoroutine((IEnumerator)Call("Reinforce")); yield return new WaitForSeconds(1);
            Check(Get<WorkshopItem>("bench") == null && Get<List<WorkshopItem>>("inventory").Count == 5, "Destruction removes the product");
            var stored = Get<List<WorkshopItem>>("inventory")[0]; stored.level = 3;
            Call("Pickup", stored); Call("UseStation", storage);
            Check(stored.storageSlot == 0 && stored.transform.parent == game.storagePivots[0], "Storage docks the upgraded item to the selected pivot");
            Call("UseStation", next); Check(Get<int>("round") == 1, "Round cannot advance below the quota");
            var sold = Get<List<WorkshopItem>>("inventory").First(x => x.storageSlot < 0);
            Call("Pickup", sold); Call("UseStation", bench); Call("UseStation", take);
            Check(Get<WorkshopItem>("held") == sold, "TAKE retrieves the workbench item");
            int value = settings.Price(sold.definition, sold.level);
            Call("UseStation", sell); Check(Get<int>("revenue") == value, "Selling credits the upgrade value");
            Set("revenue", settings.Target(1)); Call("UseStation", next);
            Check(Get<int>("round") == 2 && Get<int>("revenue") == 0, "Meeting quota advances and resets round sales");
            Check(stored && stored.level == 3 && stored.storageSlot == 0 && Get<List<WorkshopItem>>("inventory").Count == 7, "Next round preserves storage and refills all six supply items");
            Call("Save");
            foreach (var item in Get<List<WorkshopItem>>("inventory").ToArray()) Call("Remove", item);
            Check((bool)Call("Load") && Get<List<WorkshopItem>>("inventory").Any(x => x.storageSlot == 0 && x.level == 3), "Save/load restores round state and stored upgrades");
            Check((game.rig.view.cullingMask & (1 << 31)) == 0 && (game.rig.view.cullingMask & (1 << 30)) != 0, "Camera shows arms and hammer while hiding the complete body");
            Check(Get<int>("strikes") == 0, "Round attempts have no spending or attempt budget");
            settings.successRates = successBackup; settings.destructionRates = destructionBackup;
            File.WriteAllText("Assets/ReinforceGame/Validation/renderers.txt", string.Join("\n", game.GetComponentsInChildren<Renderer>().Select(x => x.name + " layer=" + x.gameObject.layer + " bounds=" + x.bounds + " material=" + (x.sharedMaterial ? x.sharedMaterial.name : "null"))));
            yield return null;
            Capture("first-person");
            yield return new WaitForSeconds(1);
            game.rig.transform.localRotation = Quaternion.Euler(0, -75, 0);
            Capture("supply-rack");
            yield return new WaitForSeconds(1);
            game.rig.transform.localRotation = Quaternion.Euler(0, 75, 0);
            Capture("storage-rack");
            yield return new WaitForSeconds(1);
        }
        void Capture(string name)
        {
            var canvas = game.GetComponentInChildren<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = game.rig.view; canvas.planeDistance = .3f;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(game.rig.view, request);
            var old = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
            File.WriteAllBytes("Assets/ReinforceGame/Validation/" + name + ".png", pixels.EncodeToPNG());
            RenderTexture.active = old; target.Release(); Destroy(target); Destroy(pixels);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            passed.Add("Rendered " + name + " camera view");
        }
        void Restore()
        {
            if (settings && successBackup != null) { settings.successRates = successBackup; settings.destructionRates = destructionBackup; }
            if (backup != null) { if (hadSave) PlayerPrefs.SetString(Key, backup); else PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
            SessionState.SetBool("ReinforceSmoke", false);
        }
    }
}
#endif
