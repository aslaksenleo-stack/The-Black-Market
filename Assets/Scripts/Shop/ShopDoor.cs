using System.Collections;
using BlackMarket.Audio;
using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>The entrance door. Swings outward when a visitor arrives or leaves, ringing the shop bell.</summary>
    [DisallowMultipleComponent]
    public sealed class ShopDoor : MonoBehaviour
    {
        [SerializeField] private float openAngle = -100f;
        [SerializeField] private float swingSeconds = 0.6f;

        private Coroutine swing;
        private Quaternion closed;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            closed = transform.localRotation;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            SoundKit.Play(Sound.DoorBell, 0.7f);
            SwingTo(closed * Quaternion.Euler(0f, openAngle, 0f));
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            SwingTo(closed);
        }

        private void SwingTo(Quaternion target)
        {
            if (swing != null) StopCoroutine(swing);
            swing = StartCoroutine(Tween.RotateLocal(transform, target, swingSeconds));
        }

        /// <summary>Opens, waits, and closes again (for visitors passing through).</summary>
        public IEnumerator OpenFor(float seconds)
        {
            Open();
            yield return new WaitForSeconds(seconds);
            Close();
        }
    }
}
