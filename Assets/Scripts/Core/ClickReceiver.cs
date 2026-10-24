using System;
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

            var hitCollider = GetComponent<Collider2D>();
            var camera = Camera.main;
            if (hitCollider == null || camera == null)
            {
                return;
            }

            Vector3 screenPosition = mouse.position.ReadValue();
            screenPosition.z = -camera.transform.position.z;
            if (hitCollider.OverlapPoint(camera.ScreenToWorldPoint(screenPosition)))
            {
                OnClick?.Invoke();
            }
        }
    }
}
