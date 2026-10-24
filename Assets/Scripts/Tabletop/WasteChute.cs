using System;
using UnityEngine;

namespace GardenMerger.Tabletop
{
    [RequireComponent(typeof(Collider2D))]
    public class WasteChute : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _dustParticles;

        public event Action<DraggableItem> OnItemDisposed;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var item = collision.GetComponent<DraggableItem>();
            if (item != null && !item.IsDragging)
            {
                DisposeItem(item);
            }
        }

        public void CheckDispose(DraggableItem item)
        {
            if (item == null) return;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(item.transform.position))
            {
                DisposeItem(item);
            }
        }

        private void DisposeItem(DraggableItem item)
        {
            if (item.CurrentSlot != null)
            {
                item.CurrentSlot.ClearItem();
            }

            if (_dustParticles != null)
            {
                _dustParticles.Play();
            }

            OnItemDisposed?.Invoke(item);
            Destroy(item.gameObject);
        }
    }
}
