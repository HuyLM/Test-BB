using System;
using System.Collections.Generic;
using UnityEngine;

namespace PopJet
{
    public enum SolutionStepKind { Poke, Launch }

    /// <summary>Một thao tác của người chơi trong lời giải đã ghi.</summary>
    [Serializable]
    public struct SolutionStep
    {
        public SolutionStepKind kind;
        [Tooltip("Số bước vật lý đã chạy từ lúc vào màn tới thao tác này. Phát lại đúng bước này thì vật lý ra y hệt.")]
        public int tick;
        [Tooltip("Thời điểm trên path (giây thế giới, tính từ thao tác đầu tiên).")]
        public float time;
        [Tooltip("Poke: hướng bóng bay (ngược hướng kim), đúng giá trị đã đưa vào lực đẩy. Launch: hướng bắn.")]
        public Vector2 direction;
        [Tooltip("Chỉ dùng cho Launch: vận tốc bóng lúc bắn ra khỏi pháo giấy.")]
        public Vector2 velocity;
    }

    /// <summary>
    /// Lời giải designer ghi lại (GDD §8.3 điều 5, §15.7), lưu trong LevelDefinition:
    /// - pokes: số cú chọc, dùng làm Par.
    /// - path: tâm bóng (x, y) và bán kính (z) mỗi sampleInterval giây thế giới, từ thao tác đầu tiên tới khi về đích.
    ///   Bóng ma Gợi ý chỉ phát lại các vị trí này, không mô phỏng lại vật lý (vật lý có thể lệch nhẹ giữa các máy).
    /// - steps: các thao tác kèm số bước vật lý, để test phát lại kiểm tra màn còn giải được sau khi chỉnh màn hay chỉnh số.
    /// Ghi bằng SolutionRecorder (menu Tools > PopJet > Solutions).
    /// </summary>
    [Serializable]
    public class LevelSolution
    {
        [Tooltip("Khoảng cách giữa hai mẫu vị trí (giây thế giới). GDD §15.7: 0.05.")]
        public float sampleInterval = 0.05f;
        [Tooltip("Fixed Timestep lúc ghi. Đổi Fixed Timestep thì phải ghi lại mọi lời giải.")]
        public float fixedDeltaTime = 0.02f;
        public List<Vector3> path = new List<Vector3>();
        public List<SolutionStep> steps = new List<SolutionStep>();
        [Tooltip("Số cú chọc (không tính bắn pháo giấy).")]
        public int pokes;
        [Tooltip("Tổng số bước vật lý từ lúc vào màn tới khi về đích.")]
        public int totalTicks;
        [Tooltip("Lúc ghi (yyyy-MM-dd HH:mm).")]
        public string recordedAt;

        public bool IsValid => steps != null && steps.Count > 0 && path != null && path.Count > 0;

        /// <summary>Thời điểm của mẫu cuối cùng (giây, tính từ thao tác đầu tiên).</summary>
        public float Duration => path == null || path.Count == 0 ? 0f : (path.Count - 1) * sampleInterval;

        /// <summary>Tâm bóng (x, y) và bán kính (z) tại thời điểm time, nội suy giữa hai mẫu.</summary>
        public Vector3 Sample(float time)
        {
            if (path == null || path.Count == 0) return Vector3.zero;
            float f = Mathf.Max(0f, time) / Mathf.Max(1e-4f, sampleInterval);
            int i = Mathf.Min(Mathf.FloorToInt(f), path.Count - 1);
            if (i >= path.Count - 1) return path[path.Count - 1];
            return Vector3.Lerp(path[i], path[i + 1], f - i);
        }

        /// <summary>Chỉ số mẫu gần vị trí pos nhất.</summary>
        public int NearestSample(Vector2 pos)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < path.Count; i++)
            {
                float d = ((Vector2)path[i] - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Thao tác nên gợi ý khi bóng đang ở pos (GDD §7.2: mặc định chỉ diễn cú kế tiếp): thao tác đầu tiên kể từ
        /// điểm gần nhất trên đường đã ghi. Ở điểm xuất phát thì ra thao tác đầu tiên của lời giải.
        /// </summary>
        public int HintStep(Vector2 pos)
        {
            if (!IsValid) return -1;
            float t = NearestSample(pos) * sampleInterval;
            for (int i = 0; i < steps.Count; i++)
                if (steps[i].time >= t - sampleInterval * 0.5f) return i;
            return steps.Count - 1;
        }

        /// <summary>Thời điểm kết thúc đoạn bay của thao tác step: lúc thao tác kế tiếp, hoặc lúc về đích.</summary>
        public float SegmentEnd(int step) => step + 1 < steps.Count ? steps[step + 1].time : Duration;

        public LevelSolution Clone()
        {
            var c = (LevelSolution)MemberwiseClone();
            c.path = new List<Vector3>(path);
            c.steps = new List<SolutionStep>(steps);
            return c;
        }
    }
}
