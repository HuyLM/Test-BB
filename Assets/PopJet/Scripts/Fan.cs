using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Quạt: đẩy bóng theo hướng +X local (trục đỏ) khi bóng nằm trong vùng trigger.
    /// Xoay object quanh trục Z để đổi hướng gió. Có thể bật/tắt bằng SetOn (ví dụ từ nút bấm).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Fan : MonoBehaviour
    {
        [SerializeField] float force = 4f;   // starting value
        [SerializeField] bool isOn = true;

        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        public bool IsOn => isOn;
        public void SetOn(bool value) { isOn = value; }
        public void Toggle() { isOn = !isOn; }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!isOn) return;
            if (other.GetComponentInParent<BalloonController>() == null) return;
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            rb.AddForce(transform.right * force, ForceMode2D.Force);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.right * 1.5f);
        }
    }
}
