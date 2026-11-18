using System.Collections;
using UnityEngine;

namespace PopJet
{
    /// <summary>Cửa: trượt đi một đoạn khi mở. Collider tắt ngay khi bắt đầu mở để bóng không bị kẹt.</summary>
    public class Door : MonoBehaviour
    {
        [SerializeField] Vector2 openOffset = new Vector2(0f, 2.5f);
        [SerializeField] float duration = 0.5f;

        Collider2D[] colliders;
        Vector3 closedPos;
        Vector3 openPos;
        Coroutine routine;

        void Awake()
        {
            colliders = GetComponentsInChildren<Collider2D>();
            closedPos = transform.localPosition;
            openPos = closedPos + (Vector3)openOffset;
        }

        public void Open() { MoveTo(openPos, false); }
        public void Close() { MoveTo(closedPos, true); }

        void MoveTo(Vector3 target, bool enableCollidersAtEnd)
        {
            if (!enableCollidersAtEnd) SetColliders(false);
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Slide(target, enableCollidersAtEnd));
        }

        IEnumerator Slide(Vector3 target, bool enableCollidersAtEnd)
        {
            Vector3 from = transform.localPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(from, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            transform.localPosition = target;
            if (enableCollidersAtEnd) SetColliders(true);
            routine = null;
        }

        void SetColliders(bool value)
        {
            foreach (var c in colliders) c.enabled = value;
        }
    }
}
