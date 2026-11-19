using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Gắn vào object gốc của prefab màn. Mọi thứ của màn (tường, gai, quạt, keo, cửa, đích...) nằm bên dưới.
    /// Bóng, input, camera, HUD, nhạc nằm sẵn trong scene gameplay, không nằm trong prefab màn.
    /// </summary>
    public class LevelRoot : MonoBehaviour
    {
        [Tooltip("Vị trí bóng lúc bắt đầu màn. Để trống thì dùng gốc của prefab.")]
        [SerializeField] Transform spawnPoint;

        public Transform SpawnPoint => spawnPoint != null ? spawnPoint : transform;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.33f, 0.3f);
            Gizmos.DrawWireSphere(SpawnPoint.position, 0.45f);
        }
    }
}
