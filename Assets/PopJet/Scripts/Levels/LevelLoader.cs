using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PopJet
{
    /// <summary>
    /// Một scene gameplay duy nhất cho mọi màn (GDD §15.7). Khi scene load, tạo prefab của màn hiện tại
    /// từ LevelCatalog và đặt bóng vào điểm xuất phát.
    /// Chơi lại hoặc sang màn mới = load lại scene gameplay (chỉ số màn giữ trong biến static), cách này
    /// reset sạch mọi trạng thái của bóng, HUD và vật cản mà không phải tự reset từng thứ.
    /// Chạy trước mọi script khác để bóng đã ở đúng chỗ khi các Awake khác chạy.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] LevelCatalog catalog;
        [SerializeField] BalloonController balloon;
        [SerializeField] CinemachineCamera vcam;
        [Tooltip("≥ 0: bấm Play là vào thẳng màn này (để test). -1: bắt đầu từ màn đầu tiên.")]
        [SerializeField] int startLevelOverride = -1;

        static int currentIndex = -1;

        // Giữ đúng khi tắt Domain Reload trong Enter Play Mode Options.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { currentIndex = -1; }

        public static LevelLoader Current { get; private set; }
        public LevelCatalog Catalog => catalog;
        public LevelDefinition Level { get; private set; }
        public LevelRoot Root { get; private set; }
        public int Index => currentIndex;
        public bool HasNext => catalog != null && currentIndex + 1 < catalog.Count;

        void Awake()
        {
            Current = this;
            if (catalog == null || catalog.Count == 0)
            {
                Debug.LogError("[PopJet] LevelLoader chưa có LevelCatalog hoặc catalog rỗng.");
                return;
            }
            // Lần đầu vào scene: màn override (để test), nếu không thì chơi tiếp màn gần nhất trong file save.
            if (currentIndex < 0) currentIndex = startLevelOverride >= 0 ? startLevelOverride : SaveSystem.Data.lastLevelIndex;
            currentIndex = Mathf.Clamp(currentIndex, 0, catalog.Count - 1);
            SaveSystem.SetLastLevel(currentIndex);

            Level = catalog[currentIndex];
            if (Level == null || Level.Prefab == null)
            {
                Debug.LogError($"[PopJet] Màn số {currentIndex} trong catalog thiếu LevelDefinition hoặc prefab.");
                return;
            }
            Root = Instantiate(Level.Prefab);
            Root.name = Level.Prefab.name;
            PlaceBalloon(Root.SpawnPoint.position);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void PlaceBalloon(Vector3 p)
        {
            p.z = 0f;
            if (balloon != null)
            {
                balloon.transform.position = p;
                var rb = balloon.GetComponent<Rigidbody2D>();
                if (rb != null) rb.position = p;
            }
            if (vcam != null)
            {
                // Camera nhảy thẳng tới bóng, không lướt từ vị trí cũ trong scene.
                vcam.transform.position = new Vector3(p.x, p.y, vcam.transform.position.z);
                vcam.PreviousStateIsValid = false;
            }
        }

        /// <summary>Mở màn theo thứ tự trong catalog (load lại scene gameplay).</summary>
        public void LoadLevel(int index)
        {
            if (catalog == null || catalog.Count == 0) return;
            currentIndex = Mathf.Clamp(index, 0, catalog.Count - 1);
            TimeController.ResetAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadNext()
        {
            if (HasNext) LoadLevel(currentIndex + 1);
        }
    }
}
