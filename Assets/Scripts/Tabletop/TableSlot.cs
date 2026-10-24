using UnityEngine;

namespace GardenMerger.Tabletop
{
    public enum SlotType
    {
        IncubatorSeedInput,
        IncubatorFlowerOutput,
        CrossBreederParentA,
        CrossBreederParentB,
        CrossBreederSeedOutput,
        OrderSubmissionTray
    }

    public class TableSlot : MonoBehaviour
    {
        [SerializeField] private SlotType _slotType;
        [SerializeField] private float _snapRadius = 1.0f;
        private DraggableItem _currentItem;

        public SlotType SlotType => _slotType;
        public float SnapRadius => _snapRadius;
        public DraggableItem CurrentItem => _currentItem;
        public bool IsOccupied => _currentItem != null;

        public void Initialize(SlotType slotType, float snapRadius)
        {
            _slotType = slotType;
            _snapRadius = snapRadius;
        }

        public bool CanAcceptItem(DraggableItem item)
        {
            if (IsOccupied) return false;

            switch (_slotType)
            {
                case SlotType.IncubatorSeedInput:
                    return item is SeedItem;
                case SlotType.CrossBreederParentA:
                case SlotType.CrossBreederParentB:
                case SlotType.OrderSubmissionTray:
                    return item is FlowerPot;
                case SlotType.IncubatorFlowerOutput:
                case SlotType.CrossBreederSeedOutput:
                    return false; // Dispensed only by machines
                default:
                    return false;
            }
        }

        public void AssignItem(DraggableItem item)
        {
            _currentItem = item;
            if (item != null)
            {
                item.transform.position = transform.position;
                item.SetCurrentSlot(this);
            }
        }

        public void ClearItem()
        {
            _currentItem = null;
        }
    }
}
