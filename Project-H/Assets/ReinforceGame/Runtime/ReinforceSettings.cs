using System;
using UnityEngine;

namespace ReinforceGame
{
    [CreateAssetMenu(menuName = "Reinforce Game/Settings")]
    public sealed class ReinforceSettings : ScriptableObject
    {
        public GameObject workshop;
        public ItemDefinition[] items;
        [Min(1)] public int firstTarget = 250;
        [Min(1)] public float targetGrowth = 1.45f;
        [Range(0, 1)] public float[] successRates = { .9f, .8f, .7f, .6f, .5f, .4f, .3f, .2f };
        [Range(0, 1)] public float[] destructionRates = { .05f, .08f, .12f, .18f, .25f, .32f, .4f, .5f };
        public float[] priceMultipliers = { 1, 1.8f, 3.2f, 5.5f, 9, 15, 25, 42, 70 };
        public int Target(int round) => Mathf.RoundToInt(firstTarget * Mathf.Pow(targetGrowth, round - 1));
        public float Chance(int level) => successRates[Mathf.Clamp(level, 0, successRates.Length - 1)];
        public float Destruction(int level) => Mathf.Min(1 - Chance(level), destructionRates[Mathf.Clamp(level, 0, destructionRates.Length - 1)]);
        public int Price(ItemDefinition item, int level) => Mathf.RoundToInt(item.basePrice * priceMultipliers[Mathf.Clamp(level, 0, priceMultipliers.Length - 1)]);
        public int MaxLevel => Mathf.Min(successRates.Length, priceMultipliers.Length - 1);
    }

    [Serializable]
    public sealed class ItemDefinition
    {
        public string id;
        public string title;
        public GameObject prefab;
        public int basePrice = 35;
        public Vector3 displayEuler;
        public Vector3 gripEuler;
        public Vector3 gripOffset;
        public Vector3 benchEuler = new Vector3(90, 0, 0);
        public float size = .65f;
    }
}
