using UnityEngine;

namespace ReinforceGame
{
    public enum StationKind { Bench, Supply, Storage, Sell, NextRound }
    public sealed class WorkshopStation : MonoBehaviour
    {
        public StationKind kind;
        public int index;
        public Transform pivot;
    }
}
