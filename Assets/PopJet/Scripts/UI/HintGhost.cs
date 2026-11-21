using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Bóng ma Gợi ý (GDD §3.5, §7.2): diễn lại một thao tác của lời giải designer đã ghi, rồi bay theo đường đã ghi
    /// tới thao tác kế tiếp. Chỉ phát lại vị trí đã ghi (LevelSolution.path), không mô phỏng vật lý.
    /// Cú chọc: cây kim mờ kéo ra phía ngón tay rồi đâm vào bóng. Pháo giấy: chấm chỉ hướng bắn nhấp nháy.
    /// Chạy bằng unscaled time vì thế giới dừng trong lúc xem (TimeReason.Hint, GameScreens lo phần đó).
    /// </summary>
    public class HintGhost : MonoBehaviour
    {
        [SerializeField] Sprite bodySprite;
        [Tooltip("Sprite cây kim, đầu nhọn hướng +Y (như kim thật).")]
        [SerializeField] Sprite needleSprite;
        [SerializeField] Sprite dotSprite;
        [SerializeField] Material material;
        [SerializeField] Color bodyColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] Color needleColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] Color trailColor = new Color(1f, 1f, 1f, 0.5f);
        [Tooltip("Tốc độ phát lại so với lúc ghi. Starting value.")]
        [SerializeField] float playbackSpeed = 1.25f;
        [Tooltip("Bay tối đa bao lâu sau thao tác (giây thật), đoạn dài hơn thì cắt.")]
        [SerializeField] float maxFlightSeconds = 3.5f;
        [Tooltip("Kim kéo ra cách mặt bóng bao xa (đơn vị world).")]
        [SerializeField] float dragDistance = 0.9f;
        [SerializeField] int sortingOrder = 26;

        const int TrailDots = 40;

        SpriteRenderer body, needle;
        readonly List<SpriteRenderer> trail = new List<SpriteRenderer>();
        readonly List<float> trailBorn = new List<float>();

        public bool IsPlaying { get; private set; }
        /// <summary>Object bóng ma, để camera đi theo trong lúc diễn.</summary>
        public Transform Body => body != null ? body.transform : transform;

        void Awake()
        {
            body = NewRenderer("GhostBody", transform, bodySprite, bodyColor, sortingOrder);
            needle = NewRenderer("GhostNeedle", transform, needleSprite != null ? needleSprite : bodySprite, needleColor, sortingOrder + 1);
            needle.transform.localScale = new Vector3(0.22f, 0.7f, 1f); // cỡ kim thật (PopJetSceneBuilder)
            for (int i = 0; i < TrailDots; i++)
            {
                var d = NewRenderer("Trail_" + i, transform, dotSprite != null ? dotSprite : bodySprite, trailColor, sortingOrder - 1);
                d.transform.localScale = Vector3.one * 0.12f;
                trail.Add(d);
                trailBorn.Add(-99f);
            }
        }

        SpriteRenderer NewRenderer(string name, Transform parent, Sprite sprite, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = c;
            sr.sortingOrder = order;
            if (material != null) sr.sharedMaterial = material;
            sr.enabled = false;
            return sr;
        }

        /// <summary>
        /// Diễn thao tác stepIndex của lời giải. skip() trả true thì kết thúc sớm (người chơi chạm màn hình).
        /// </summary>
        public IEnumerator Play(LevelSolution solution, int stepIndex, Func<bool> skip)
        {
            if (solution == null || !solution.IsValid) yield break;
            stepIndex = Mathf.Clamp(stepIndex, 0, solution.steps.Count - 1);
            IsPlaying = true;
            var step = solution.steps[stepIndex];
            Vector3 start = solution.Sample(step.time);
            float radius = Mathf.Max(0.1f, start.z);
            PlaceBody(start, 0f);
            body.enabled = true;

            // 1. Hiện dần, chờ camera lia tới.
            float t = 0f;
            while (t < 0.45f)
            {
                if (skip()) { Hide(); yield break; }
                t += Time.unscaledDeltaTime;
                PlaceBody(start, Mathf.Clamp01(t / 0.3f));
                yield return null;
            }

            // 2. Diễn thao tác.
            if (step.kind == SolutionStepKind.Poke)
            {
                // Ngón tay (và kim) ở phía ngược hướng bay.
                Vector2 finger = -step.direction.normalized;
                needle.enabled = true;
                needle.transform.up = -finger; // đầu kim chĩa vào bóng
                t = 0f;
                const float drag = 0.6f, hold = 0.3f, stab = 0.1f;
                while (t < drag + hold + stab)
                {
                    if (skip()) { Hide(); yield break; }
                    t += Time.unscaledDeltaTime;
                    float out01 = t < drag ? Mathf.SmoothStep(0f, 1f, t / drag)
                                : t < drag + hold ? 1f
                                : 1f - Mathf.Clamp01((t - drag - hold) / stab);
                    float dist = radius + 0.15f + dragDistance * out01;
                    needle.transform.position = (Vector2)start + finger * dist;
                    var c = needleColor;
                    c.a *= Mathf.Clamp01(t / 0.15f);
                    needle.color = c;
                    yield return null;
                }
                needle.enabled = false;
            }
            else
            {
                // Pháo giấy: chấm nhấp nháy theo hướng bắn.
                t = 0f;
                while (t < 0.9f)
                {
                    if (skip()) { Hide(); yield break; }
                    t += Time.unscaledDeltaTime;
                    for (int i = 0; i < 4; i++)
                    {
                        var d = trail[i];
                        d.enabled = true;
                        d.transform.position = (Vector2)start + step.direction.normalized * (radius + 0.35f + i * 0.35f);
                        var c = trailColor;
                        c.a = 0.25f + 0.6f * Mathf.Abs(Mathf.Sin((t * 3f - i * 0.25f) * Mathf.PI));
                        d.color = c;
                    }
                    yield return null;
                }
                for (int i = 0; i < 4; i++) trail[i].enabled = false;
            }

            // 3. Bay theo đường đã ghi tới thao tác kế tiếp (hoặc tới đích).
            float from = step.time;
            float to = Mathf.Min(solution.SegmentEnd(stepIndex), from + maxFlightSeconds * playbackSpeed);
            float pathT = from, nextDot = from;
            int dotIndex = 0;
            while (pathT < to)
            {
                if (skip()) break;
                pathT = Mathf.Min(to, pathT + Time.unscaledDeltaTime * playbackSpeed);
                var p = solution.Sample(pathT);
                PlaceBody(p, 1f);
                if (pathT >= nextDot)
                {
                    var d = trail[dotIndex % TrailDots];
                    d.enabled = true;
                    d.transform.position = (Vector2)p;
                    trailBorn[dotIndex % TrailDots] = Time.unscaledTime;
                    dotIndex++;
                    nextDot += 0.1f;
                }
                FadeTrail();
                yield return null;
            }

            // 4. Mờ dần.
            var last = solution.Sample(pathT);
            t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                PlaceBody(last, 1f - t / 0.35f);
                FadeTrail(1f - t / 0.35f);
                yield return null;
            }
            Hide();
        }

        void PlaceBody(Vector3 sample, float alpha)
        {
            body.transform.position = new Vector3(sample.x, sample.y, 0f);
            body.transform.localScale = Vector3.one * Mathf.Max(0.1f, sample.z) * 2f;
            var c = bodyColor;
            c.a *= Mathf.Clamp01(alpha);
            body.color = c;
        }

        void FadeTrail(float mul = 1f)
        {
            for (int i = 0; i < TrailDots; i++)
            {
                if (!trail[i].enabled) continue;
                float age = Time.unscaledTime - trailBorn[i];
                var c = trailColor;
                c.a *= Mathf.Clamp01(1f - age / 1.6f) * mul;
                trail[i].color = c;
            }
        }

        void Hide()
        {
            body.enabled = false;
            needle.enabled = false;
            foreach (var d in trail) d.enabled = false;
            IsPlaying = false;
        }

        void OnDisable()
        {
            if (body != null) Hide();
        }
    }
}
