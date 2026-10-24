using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GardenMerger.Tabletop
{
    [RequireComponent(typeof(Collider2D))]
    public class InfiniteSeedPouch : MonoBehaviour
    {
        [SerializeField] private string _colorId;
        public string ColorId => _colorId;

        public event Action<string, Vector3> OnSeedRequested;

        private bool _isPressedOnPouch;
        private Vector2 _pressStartPosition;
        private const float DragThresholdPixels = 12f;

        public void Initialize(string colorId)
        {
            _colorId = colorId;
        }

        private void OnDisable()
        {
            _isPressedOnPouch = false;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            // 1. Если сейчас перетаскивается любой предмет — мешок полностью игнорирует события
            if (DraggableItem.IsAnyDragging)
            {
                _isPressedOnPouch = false;
                return;
            }

            var camera = Camera.main;
            var pouchCollider = GetComponent<Collider2D>();
            if (camera == null || pouchCollider == null)
            {
                return;
            }

            Vector2 mouseScreenPos = mouse.position.ReadValue();
            Vector3 worldPos = camera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -camera.transform.position.z));

            // Фиксируем нажатие мыши ТОЛЬКО если оно началось на самом мешке
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _isPressedOnPouch = false;

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                if (pouchCollider.OverlapPoint(worldPos))
                {
                    // Если над мешком уже лежит предмет (семечко или цветок) — мешок не нажимается
                    if (!IsOccupiedByDraggable())
                    {
                        _isPressedOnPouch = true;
                        _pressStartPosition = mouseScreenPos;
                    }
                }
            }

            // Если мышь сдвинулась дальше порога — это перетаскивание или жест мыши, отменяем клик по мешку
            if (_isPressedOnPouch && mouse.leftButton.isPressed)
            {
                if (Vector2.Distance(mouseScreenPos, _pressStartPosition) > DragThresholdPixels)
                {
                    _isPressedOnPouch = false;
                }
            }

            // Семечко создается ТОЛЬКО при полноценном преднамеренном клике (нажали и отпустили на мешке без перетаскивания)
            if (_isPressedOnPouch && mouse.leftButton.wasReleasedThisFrame)
            {
                _isPressedOnPouch = false;

                if (pouchCollider.OverlapPoint(worldPos) && !IsOccupiedByDraggable())
                {
                    Vector3 spawnPos = transform.position;
                    spawnPos.z = -0.5f;
                    OnSeedRequested?.Invoke(_colorId, spawnPos);
                }
            }
        }

        private bool IsOccupiedByDraggable()
        {
            var pouchCollider = GetComponent<Collider2D>();
            if (pouchCollider == null) return false;

            Bounds bounds = pouchCollider.bounds;
            bounds.Expand(0.2f);

            var draggables = FindObjectsByType<DraggableItem>(FindObjectsInactive.Exclude);
            for (int i = 0; i < draggables.Length; i++)
            {
                var item = draggables[i];
                if (item == null || item.IsDragging) continue;

                var itemCol = item.GetComponent<Collider2D>();
                if (itemCol != null && bounds.Intersects(itemCol.bounds))
                {
                    return true;
                }
                else if (Vector2.Distance(transform.position, item.transform.position) < 0.75f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
