using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ReinforceGame
{
    public sealed class WorkshopGame : MonoBehaviour
    {
        public FirstPersonRig rig;
        public Transform benchPivot;
        public Transform[] supplyPivots, storagePivots;
        public Text statusText, itemText, helpText, messageText;
        public Light resultLight;
        ReinforceSettings settings;
        WorkshopItem held, bench;
        WorkshopItem hovered;
        readonly List<WorkshopItem> inventory = new List<WorkshopItem>();
        InputAction click, reset;
        int round = 1, revenue, strikes;
        bool busy, initialized;
        string message = "LEFT: supply rack  /  CENTER: workbench  /  RIGHT: storage";
        float messageUntil;
        const string SaveKey = "ReinforceGame.Save.v1";

        [Serializable] sealed class SavedItem { public string id; public int level; public string location; public int slot; }
        [Serializable] sealed class SaveState { public int round = 1; public int revenue; public int strikes; public List<SavedItem> items = new List<SavedItem>(); }

        public void Initialize(ReinforceSettings data)
        {
            settings = data;
            click = new InputAction("Interact", InputActionType.Button, "<Mouse>/leftButton");
            reset = new InputAction("Retry", InputActionType.Button, "<Keyboard>/r");
            click.Enable(); reset.Enable();
            initialized = true;
            if (!Load()) Refill();
            RefreshUI();
        }
        void OnDestroy() { click?.Dispose(); reset?.Dispose(); }
        void Update()
        {
            if (!initialized) return;
            rig.carrying = held != null;
            hovered = null;
            if (Mouse.current != null)
            {
                var hoverHits = Physics.RaycastAll(rig.view.ScreenPointToRay(Mouse.current.position.ReadValue()), 8f, ~(1 << 30 | 1 << 31));
                Array.Sort(hoverHits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hoverHits)
                {
                    var candidate = hit.collider.GetComponentInParent<WorkshopItem>();
                    if (candidate && candidate == held) continue;
                    hovered = candidate; break;
                }
            }
            if (!busy && reset.WasPressedThisFrame() && IsBankrupt())
            {
                revenue = 0; strikes = 0; Refill(); Save(); Say("Round retried. The target is unchanged.");
            }
            if (!busy && click.WasPressedThisFrame() && Mouse.current != null && !Mouse.current.rightButton.isPressed)
                Interact();
            RefreshUI();
        }
        void Interact()
        {
            Ray ray = rig.view.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, 8f, ~(1 << 30 | 1 << 31));
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            RaycastHit hit = default;
            bool found = false;
            foreach (var candidate in hits)
            {
                if (candidate.collider.GetComponentInParent<WorkshopItem>() == held && held) continue;
                hit = candidate; found = true; break;
            }
            if (!found) return;
            var item = hit.collider.GetComponentInParent<WorkshopItem>();
            if (item)
            {
                if (held) { Say("Place the held item first."); return; }
                if (item == bench) { StartCoroutine(Reinforce()); return; }
                Pickup(item); return;
            }
            var station = hit.collider.GetComponentInParent<WorkshopStation>();
            if (!station) return;
            UseStation(station);
        }
        void UseStation(WorkshopStation station)
        {
            switch (station.kind)
            {
                case StationKind.Bench:
                    if (station.index == 1)
                    {
                        if (bench && !held) Pickup(bench);
                    }
                    else if (held)
                    {
                        if (bench) { Say("The workbench is occupied."); break; }
                        bench = held; held = null; bench.storageSlot = -1;
                        bench.Dock(benchPivot, bench.definition.benchEuler);
                        Save(); Say("Click REINFORCE to strike. TAKE retrieves the item.");
                    }
                    else if (bench) StartCoroutine(Reinforce());
                    break;
                case StationKind.Storage:
                    if (!held) { Say("Carry an item here to keep it for future rounds."); break; }
                    if (inventory.Exists(x => x != held && x.storageSlot == station.index)) { Say("This storage slot is occupied."); break; }
                    held.storageSlot = station.index;
                    held.Dock(storagePivots[station.index], held.definition.displayEuler);
                    held = null; Save(); Say("Stored. This item will survive the round change."); break;
                case StationKind.Sell:
                    if (!held) { Say("Carry an item to the selling tray."); break; }
                    int price = settings.Price(held.definition, held.level);
                    revenue += price; Remove(held); held = null; Save(); Say($"Sold for {price:N0}. Round sales: {revenue:N0}."); break;
                case StationKind.NextRound:
                    if (revenue < settings.Target(round)) { Say("Reach the sales target before advancing."); break; }
                    if (held || bench) { Say("Sell or store the active item before advancing."); break; }
                    round++; revenue = 0; strikes = 0; Refill(); Save(); Say("New target. Supply refilled; stored items retained."); break;
            }
        }
        void Pickup(WorkshopItem item)
        {
            if (item == bench) bench = null;
            held = item; item.storageSlot = -1;
            item.Dock(rig.itemGrip, item.definition.gripEuler, GripOffset(item));
            Save(); Say("Carry to the workbench, storage rack, or selling tray.");
        }
        Vector3 GripOffset(WorkshopItem item) => item.definition.gripOffset - Quaternion.Euler(item.definition.gripEuler) * item.visual.localPosition;
        IEnumerator Reinforce()
        {
            if (bench.level >= settings.MaxLevel) { Say("Maximum upgrade reached. Sell or store this item."); yield break; }
            busy = true;
            float time = 0;
            while (time < .5f)
            {
                time += Time.deltaTime; rig.strike = Mathf.Sin(Mathf.Clamp01(time / .5f) * Mathf.PI);
                yield return null;
            }
            rig.strike = 0; strikes++;
            float roll = UnityEngine.Random.value;
            float success = settings.Chance(bench.level), destruction = settings.Destruction(bench.level);
            if (roll < success)
            {
                bench.level++; resultLight.color = new Color(.25f, 1, .65f);
                Say($"SUCCESS  +{bench.level}  |  Value {settings.Price(bench.definition, bench.level):N0}");
            }
            else if (roll < success + destruction)
            {
                Remove(bench); bench = null; resultLight.color = new Color(1, .15f, .07f); Say("DESTROYED. Choose another item or retrieve one from storage.");
            }
            else
            {
                bench.level = Mathf.Max(0, bench.level - 1); resultLight.color = new Color(1, .6f, .1f);
                Say($"DOWNGRADE  +{bench.level}  |  Level cannot fall below zero.");
            }
            Save(); resultLight.intensity = 4;
            yield return new WaitForSeconds(.35f);
            resultLight.intensity = 0; busy = false;
        }
        void Remove(WorkshopItem item) { inventory.Remove(item); Destroy(item.gameObject); }
        WorkshopItem Spawn(ItemDefinition definition, Transform pivot, Vector3 rotation)
        {
            var root = new GameObject(definition.title);
            var item = root.AddComponent<WorkshopItem>(); item.definition = definition;
            var visual = Instantiate(definition.prefab, root.transform); item.visual = visual.transform;
            foreach (var collider in visual.GetComponentsInChildren<Collider>()) Destroy(collider);
            var renderers = visual.GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(); bool first = true;
            foreach (var renderer in renderers) { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
            float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float scale = longest > .0001f ? definition.size / longest : 1;
            visual.transform.localScale *= scale;
            visual.transform.localPosition -= bounds.center * scale;
            var box = root.AddComponent<BoxCollider>(); box.size = Vector3.Max(bounds.size * scale, Vector3.one * .12f);
            item.Dock(pivot, rotation); inventory.Add(item); return item;
        }
        void Refill()
        {
            foreach (var item in inventory.ToArray()) if (item.storageSlot < 0) Remove(item);
            held = bench = null;
            for (int i = 0; i < supplyPivots.Length; i++) Spawn(settings.items[i % settings.items.Length], supplyPivots[i], settings.items[i % settings.items.Length].displayEuler);
        }
        bool IsBankrupt() => inventory.Count == 0 && revenue < settings.Target(round);
        void Say(string value) { message = value; messageUntil = Time.unscaledTime + 5; }
        void RefreshUI()
        {
            statusText.text = $"THE LAST FORGE     /     ROUND {round:00}\nSALES  {revenue:N0} / {settings.Target(round):N0}     STRIKES  {strikes}  /  UNLIMITED";
            var item = hovered ? hovered : bench ? bench : held;
            if (item)
            {
                int level = item.level;
                string chance = level >= settings.MaxLevel ? "MAXIMUM LEVEL" : $"UP {settings.Chance(level):P0}    DOWN {1 - settings.Chance(level) - settings.Destruction(level):P0}    BREAK {settings.Destruction(level):P0}";
                string location = item == bench ? "WORKBENCH" : item == held ? "IN HAND" : item.storageSlot >= 0 ? "STORAGE" : "SUPPLY";
                itemText.text = $"{location}   {item.definition.title} +{level}\nVALUE {settings.Price(item.definition, level):N0}     {chance}";
            }
            else itemText.text = "WORKBENCH EMPTY\nChoose an item from the supply rack on your left.";
            helpText.text = "RMB DRAG  Look around     LMB  Select / place / use\nLEFT  Supply     FRONT  Reinforce & sell     RIGHT  Storage";
            if (IsBankrupt()) messageText.text = "NO ITEMS REMAIN / TARGET MISSED     Press R to retry this round.";
            else if (Time.unscaledTime < messageUntil) messageText.text = message;
            else if (revenue >= settings.Target(round)) messageText.text = "TARGET REACHED  /  Store valuable items, then click NEXT ROUND.";
            else messageText.text = "No upgrade cost. Every strike risks downgrade or destruction.";
        }
        void Save()
        {
            var state = new SaveState { round = round, revenue = revenue, strikes = strikes };
            foreach (var item in inventory)
            {
                string location = item == held ? "hand" : item == bench ? "bench" : item.storageSlot >= 0 ? "storage" : "supply";
                int slot = item.storageSlot;
                if (location == "supply") slot = Array.IndexOf(supplyPivots, item.transform.parent);
                state.items.Add(new SavedItem { id = item.definition.id, level = item.level, location = location, slot = slot });
            }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(state)); PlayerPrefs.Save();
        }
        bool Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            SaveState state;
            try { state = JsonUtility.FromJson<SaveState>(PlayerPrefs.GetString(SaveKey)); }
            catch (Exception) { return false; }
            if (state == null || state.round < 1 || state.items == null) return false;
            foreach (var entry in state.items)
                if (Array.Find(settings.items, x => x.id == entry.id) == null ||
                    (entry.location == "storage" && (entry.slot < 0 || entry.slot >= storagePivots.Length)) ||
                    (entry.location == "supply" && (entry.slot < 0 || entry.slot >= supplyPivots.Length))) return false;
            round = state.round; revenue = state.revenue; strikes = state.strikes;
            foreach (var entry in state.items)
            {
                var definition = Array.Find(settings.items, x => x.id == entry.id);
                Transform pivot = entry.location == "hand" ? rig.itemGrip : entry.location == "bench" ? benchPivot : entry.location == "storage" ? storagePivots[entry.slot] : supplyPivots[entry.slot];
                var item = Spawn(definition, pivot, entry.location == "bench" ? definition.benchEuler : entry.location == "hand" ? definition.gripEuler : definition.displayEuler);
                item.level = Mathf.Clamp(entry.level, 0, settings.MaxLevel);
                if (entry.location == "hand") { held = item; item.transform.localPosition = GripOffset(item); }
                if (entry.location == "bench") bench = item;
                if (entry.location == "storage") item.storageSlot = entry.slot;
            }
            return true;
        }
    }
}
