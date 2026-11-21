using UnityEngine;

namespace ReinforceGame
{
    public sealed class WorkshopItem : MonoBehaviour
    {
        public ItemDefinition definition;
        public int level;
        public int storageSlot = -1;
        public Transform visual;
        public void Dock(Transform pivot, Vector3 euler, Vector3 offset = default)
        {
            transform.SetParent(pivot, false);
            transform.localPosition = offset;
            transform.localRotation = Quaternion.Euler(euler);
        }
    }

}
