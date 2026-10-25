using System;
using GardenMerger.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GardenMerger.Tabletop
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class DraggableItem : MonoBehaviour
    {
        public static DraggableItem CurrentlyDraggedItem { get; private set; }
        public static bool IsAnyDragging => CurrentlyDraggedItem != null;

        [SerializeField] protected string _colorId;
        protected TableSlot _currentSlot;
        protected bool _isDragging;
        protected Vector3 _dragOffset;
        protected Vector3 _initialScale;
        protected Camera _mainCamera;
        protected Vector2 _deskBounds = new Vector2(8.0f, 4.2f);
        private SpriteRenderer[] _draggedRenderers;
        private int[] _originalSortingOrders;

        public string ColorId => _colorId;
        public TableSlot CurrentSlot => _currentSlot;
        public bool IsDragging => _isDragging;

        public event Action<DraggableItem> OnItemPickedUp;
        public event Action<DraggableItem> OnItemDropped;
        public static event Action<DraggableItem> OnAnyItemPickedUp;
        public static event Action<DraggableItem> OnAnyItemDropped;

        public virtual string GetDragTitle() => string.Empty;
        public virtual string GetDragSubtitle() => string.Empty;
        public virtual Color GetDragColor() => Color.white;
        public virtual float DragTooltipYOffset => 1.3f;

        protected virtual void Awake()
        {
            _mainCamera = Camera.main;
            _initialScale = transform.localScale;
            DragTooltipUI.EnsureInstance();
        }

        protected virtual void OnDisable()
        {
            if (_isDragging)
            {
                EndDrag();
            }
            if (CurrentlyDraggedItem == this)
            {
                CurrentlyDraggedItem = null;
                OnAnyItemDropped?.Invoke(this);
            }
        }

        public void SetCurrentSlot(TableSlot slot)
        {
            _currentSlot = slot;
        }

        public void SetDeskBounds(Vector2 bounds)
        {
            _deskBounds = bounds;
        }

        public static DraggableItem GetTopDraggableUnderPoint(Vector2 worldPoint)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
            DraggableItem topItem = null;
            int highestOrder = int.MinValue;
            float highestZ = float.MinValue;

            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null) continue;

                var item = col.GetComponentInParent<DraggableItem>();
                if (item != null && item.enabled && item.gameObject.activeInHierarchy)
                {
                    var renderer = item.GetComponentInChildren<SpriteRenderer>();
                    int order = renderer != null ? renderer.sortingOrder : 0;
                    float zPriority = -item.transform.position.z;

                    if (topItem == null || order > highestOrder || (order == highestOrder && zPriority > highestZ))
                    {
                        topItem = item;
                        highestOrder = order;
                        highestZ = zPriority;
                    }
                }
            }

            return topItem;
        }

        protected virtual void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (!_isDragging)
            {
                if (IsAnyDragging || !mouse.leftButton.wasPressedThisFrame ||
                    (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
                {
                    return;
                }

                Vector3 mouseWorldPos = GetMouseWorldPosition(mouse.position.ReadValue());
                var topItem = GetTopDraggableUnderPoint(mouseWorldPos);
                if (topItem != this)
                {
                    return;
                }

                BeginDrag(mouse);
            }

            if (_isDragging)
            {
                if (mouse.leftButton.isPressed)
                {
                    UpdateDrag(mouse);
                }
                else
                {
                    EndDrag();
                }
            }
        }

        public void BeginDrag(Mouse mouse)
        {
            if (_isDragging) return;

            CurrentlyDraggedItem = this;
            _isDragging = true;
            if (_mainCamera == null) _mainCamera = Camera.main;

            Vector3 mouseWorldPos = GetMouseWorldPosition(mouse != null ? mouse.position.ReadValue() : Vector2.zero);
            _dragOffset = transform.position - mouseWorldPos;
            transform.localScale = _initialScale * 1.15f;
            RaiseRenderersForDragging();

            if (_currentSlot != null)
            {
                _currentSlot.ClearItem();
                _currentSlot = null;
            }

            OnItemPickedUp?.Invoke(this);
            OnAnyItemPickedUp?.Invoke(this);
        }

        private void UpdateDrag(Mouse mouse)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition(mouse.position.ReadValue());
            Vector3 targetPos = mouseWorldPos + _dragOffset;

            // Clamp inside desk bounds
            targetPos.x = Mathf.Clamp(targetPos.x, -_deskBounds.x, _deskBounds.x);
            targetPos.y = Mathf.Clamp(targetPos.y, -_deskBounds.y, _deskBounds.y);
            targetPos.z = -0.5f; // Lift above table

            transform.position = targetPos;
        }

        private void EndDrag()
        {
            _isDragging = false;
            if (CurrentlyDraggedItem == this)
            {
                CurrentlyDraggedItem = null;
            }

            transform.localScale = _initialScale;
            RestoreRendererSortingOrders();
            Vector3 pos = transform.position;
            pos.z = 0f;
            transform.position = pos;

            // Check snap slots
            TableSlot nearestSlot = FindNearestValidSlot();
            if (nearestSlot != null)
            {
                nearestSlot.AssignItem(this);
            }

            OnItemDropped?.Invoke(this);
            OnAnyItemDropped?.Invoke(this);
        }

        private void RaiseRenderersForDragging()
        {
            _draggedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            _originalSortingOrders = new int[_draggedRenderers.Length];

            int dragSortingOrder = 0;
            for (int i = 0; i < _draggedRenderers.Length; i++)
            {
                _originalSortingOrders[i] = _draggedRenderers[i].sortingOrder;
                dragSortingOrder = Mathf.Max(dragSortingOrder, _originalSortingOrders[i] + 2);
            }

            for (int i = 0; i < _draggedRenderers.Length; i++)
            {
                _draggedRenderers[i].sortingOrder = dragSortingOrder;
            }
        }

        private void RestoreRendererSortingOrders()
        {
            if (_draggedRenderers == null || _originalSortingOrders == null)
            {
                return;
            }

            for (int i = 0; i < _draggedRenderers.Length; i++)
            {
                if (_draggedRenderers[i] != null)
                {
                    _draggedRenderers[i].sortingOrder = _originalSortingOrders[i];
                }
            }

            _draggedRenderers = null;
            _originalSortingOrders = null;
        }

        protected TableSlot FindNearestValidSlot()
        {
            TableSlot[] allSlots = FindObjectsOfType<TableSlot>();
            TableSlot bestSlot = null;
            float minDistance = float.MaxValue;

            foreach (var slot in allSlots)
            {
                if (!slot.CanAcceptItem(this)) continue;

                float dist = Vector2.Distance(transform.position, slot.transform.position);
                if (slot.IsWithinSnapArea(transform.position) && dist < minDistance)
                {
                    minDistance = dist;
                    bestSlot = slot;
                }
            }

            return bestSlot;
        }

        protected Vector3 GetMouseWorldPosition(Vector2 screenPosition)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            Vector3 mouseScreenPosition = screenPosition;
            mouseScreenPosition.z = -_mainCamera.transform.position.z;
            return _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        }
    }
}
