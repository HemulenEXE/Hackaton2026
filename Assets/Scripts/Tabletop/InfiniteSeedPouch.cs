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

        public void Initialize(string colorId)
        {
            _colorId = colorId;
        }

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

            var pouchCollider = GetComponent<Collider2D>();
            var camera = Camera.main;
            if (pouchCollider == null || camera == null)
            {
                return;
            }

            Vector3 screenPosition = mouse.position.ReadValue();
            screenPosition.z = -camera.transform.position.z;
            if (!pouchCollider.OverlapPoint(camera.ScreenToWorldPoint(screenPosition)))
            {
                return;
            }

            Vector3 spawnPos = transform.position;
            spawnPos.z = -0.5f;
            OnSeedRequested?.Invoke(_colorId, spawnPos);
        }
    }
}
