using UnityEngine;

namespace PopJet
{
    /// <summary>Đích: bóng chạm vào là hoàn thành màn. Collider phải là trigger.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalZone : MonoBehaviour
    {
        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D other)
        {
            var balloon = other.GetComponentInParent<BalloonController>();
            if (balloon != null) balloon.ReachGoal();
        }
    }
}
