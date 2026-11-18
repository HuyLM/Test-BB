using UnityEngine;

namespace PopJet
{
    /// <summary>Overlay debug nhỏ để tune: trạng thái, khí, số lỗ rò. Xóa khi build thật.</summary>
    public class PopJetDebug : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [SerializeField] int fontSize = 36;

        [Tooltip("Để trống thì tự tìm trong scene.")]
        [SerializeField] LevelManager level;

        GUIStyle style;

        void OnGUI()
        {
            if (balloon == null) return;
            if (level == null) level = FindAnyObjectByType<LevelManager>();
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
                style.normal.textColor = Color.white;
            }
            string time = level != null ? $"\nTime: {level.PlayTime:0.0}s" : "";
            string frozen = Time.timeScale == 0f ? "  (PAUSED)" : "";
            GUI.Label(new Rect(20, 20, 900, 260),
                $"State: {balloon.State}{frozen}\nAir: {balloon.Air01:0.00}\nVents: {balloon.VentCount}  Leaks: {balloon.LeakCount}{time}",
                style);
        }
    }
}
