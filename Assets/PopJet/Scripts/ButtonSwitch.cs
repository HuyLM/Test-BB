using UnityEngine;
using UnityEngine.Events;

namespace PopJet
{
    /// <summary>
    /// Nút bấm: bóng chạm vào thì mở các cửa được gán (và gọi UnityEvent).
    /// Collider phải là trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ButtonSwitch : MonoBehaviour
    {
        [SerializeField] Door[] doors;
        [Tooltip("true = bấm một lần là giữ nguyên trạng thái. false = chỉ mở khi bóng đang đè lên nút.")]
        [SerializeField] bool latch = true;
        [SerializeField] UnityEvent onPressed;
        [SerializeField] UnityEvent onReleased;

        bool pressed;

        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (pressed || other.GetComponentInParent<BalloonController>() == null) return;
            pressed = true;
            foreach (var d in doors) if (d != null) d.Open();
            onPressed?.Invoke();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (latch || !pressed || other.GetComponentInParent<BalloonController>() == null) return;
            pressed = false;
            foreach (var d in doors) if (d != null) d.Close();
            onReleased?.Invoke();
        }
    }
}
