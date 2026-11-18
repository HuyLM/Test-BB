using Unity.Cinemachine;
using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Giữ chiều ngang nhìn thấy cố định trên mọi tỉ lệ màn hình (GDD §15.4).
    /// - Ở tỉ lệ tham chiếu (9:16) ortho size = referenceOrthoSize, đúng giá trị đã tune trong README.
    /// - Máy dài hơn (9:20) thấy thêm theo chiều dọc thay vì bị hẹp chiều ngang.
    /// - Máy rộng hơn (tablet 3:4) giữ referenceOrthoSize nên thấy rộng hơn, không bị cắt bớt chiều dọc.
    /// Gắn vào object có CinemachineCamera. Chỉ chạy lúc Play, không sửa scene trong Editor.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class CameraAspectFit : MonoBehaviour
    {
        [Tooltip("Ortho size ở tỉ lệ tham chiếu. CM FollowCam dùng 3.5 (README mục 4).")]
        [SerializeField] float referenceOrthoSize = 3.5f;
        [Tooltip("Tỉ lệ rộng/cao tham chiếu: 9:16 = 0.5625.")]
        [SerializeField] float referenceAspect = 9f / 16f;

        CinemachineCamera vcam;
        int lastWidth, lastHeight;

        /// <summary>Chiều ngang (đơn vị world) luôn nhìn thấy được.</summary>
        public float VisibleWidth => 2f * referenceOrthoSize * referenceAspect;

        void Awake()
        {
            vcam = GetComponent<CinemachineCamera>();
            Apply();
        }

        void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) Apply();
        }

        void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            if (lastWidth <= 0 || lastHeight <= 0) return;

            float aspect = (float)lastWidth / lastHeight;
            float size = Mathf.Max(referenceOrthoSize, VisibleWidth / (2f * aspect));
            var lens = vcam.Lens;
            lens.OrthographicSize = size;
            vcam.Lens = lens;
        }
    }
}
