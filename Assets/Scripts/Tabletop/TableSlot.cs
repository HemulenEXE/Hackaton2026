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
        private const int MachineItemSortingOrder = 1;

        [SerializeField] private SlotType _slotType;
        [SerializeField] private float _snapRadius = 1.5f;
        private DraggableItem _currentItem;
        private Bounds _snapAreaBounds;
        private bool _hasSnapArea;

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

        public void SetSnapArea(Bounds bounds)
        {
            _snapAreaBounds = bounds;
            _hasSnapArea = true;
        }

        public bool IsWithinSnapArea(Vector2 position)
        {
            if (_hasSnapArea)
            {
                return position.x >= _snapAreaBounds.min.x &&
                       position.x <= _snapAreaBounds.max.x &&
                       position.y >= _snapAreaBounds.min.y &&
                       position.y <= _snapAreaBounds.max.y;
            }

            return Vector2.Distance(position, transform.position) <= _snapRadius;
        }

        public void AssignItem(DraggableItem item)
        {
            _currentItem = item;
            if (item != null)
            {
                item.transform.position = transform.position;
                item.SetCurrentSlot(this);

                if (IsMachineSlot())
                {
                    SpriteRenderer[] renderers = item.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (SpriteRenderer renderer in renderers)
                    {
                        renderer.sortingOrder = MachineItemSortingOrder;
                    }
                }
            }
        }

        public void ClearItem()
        {
            _currentItem = null;
        }

        private bool IsMachineSlot()
        {
            return _slotType == SlotType.IncubatorSeedInput ||
                   _slotType == SlotType.IncubatorFlowerOutput ||
                   _slotType == SlotType.CrossBreederParentA ||
                   _slotType == SlotType.CrossBreederParentB;
        }
    }
}
