using System;
using GardenMerger.Tabletop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GardenMerger.Core
{
    public class ClickReceiver : MonoBehaviour
    {
        public Action OnClick;

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (DraggableItem.IsAnyDragging)
            {
                return;
            }

            var hitCollider = GetComponent<Collider2D>();
            var camera = Camera.main;
            if (hitCollider == null || camera == null)
            {
                return;
            }

            Vector3 screenPosition = mouse.position.ReadValue();
            screenPosition.z = -camera.transform.position.z;
            Vector3 worldPos = camera.ScreenToWorldPoint(screenPosition);

            if (!hitCollider.OverlapPoint(worldPos))
            {
                return;
            }

            var topDraggable = DraggableItem.GetTopDraggableUnderPoint(worldPos);
            if (topDraggable != null)
            {
                return;
            }

            OnClick?.Invoke();
        }
    }
}
