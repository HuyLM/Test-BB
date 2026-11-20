using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Lỗ rò nhìn thấy được (GDD §4.3): mỗi lỗ rò có một vết lỗ trên mặt bóng, một dòng khí phun liên tục
    /// (mạnh theo lực còn lại của lỗ) và tiếng "xì" loop. Lỗ tự đóng thì vết mờ dần; vá bằng keo thì
    /// thay bằng miếng băng keo chéo, giữ tới hết màn.
    /// Mỗi frame đọc danh sách jet từ BalloonController (GetJets), không cần thêm sự kiện.
    /// Tiếng xì tạm dừng khi thế giới dừng (lúc ngắm lỗ rò cũng không mất khí).
    /// </summary>
    [RequireComponent(typeof(BalloonController), typeof(CircleCollider2D))]
    public class LeakStreamFX : MonoBehaviour
    {
        [Tooltip("Particle loop phát theo +Y local, simulation space World.")]
        [SerializeField] ParticleSystem streamPrefab;
        [SerializeField] Sprite holeSprite;
        [SerializeField] Sprite tapeSprite;
        [SerializeField] Material spriteMaterial;
        [SerializeField] AudioClip hissLoop;
        [SerializeField, Range(0f, 1f)] float hissVolume = 0.25f;
        [Tooltip("Số hạt mỗi giây khi lỗ rò vừa mở (giảm dần theo lực).")]
        [SerializeField] float maxRate = 40f;
        [SerializeField] Color holeColor = new Color(0.17f, 0.17f, 0.23f);
        [SerializeField] Color tapeColor = new Color(0.957f, 0.835f, 0.553f); // #F4D58D
        [SerializeField] int sortingOrder = 12;
        [Tooltip("Kích thước vết lỗ (local của bóng): x theo tiếp tuyến, y theo bán kính. Dẹt theo bán kính " +
                 "vì lỗ nằm sát mép mặt cầu nhìn nghiêng. Starting value.")]
        [SerializeField] Vector2 holeSize = new Vector2(0.1f, 0.065f);
        [Tooltip("Thụt vết lỗ/băng keo vào trong mép bóng (đơn vị local) để không lòi ra ngoài viền. " +
                 "Dòng khí vẫn phun từ đúng mép. Starting value.")]
        [SerializeField] float markInset = 0.07f;

        class Hole
        {
            public Transform root;
            public ParticleSystem stream;
            public AudioSource audio;
            public SpriteRenderer mark;
            public bool patched;
            public bool paused;
        }

        BalloonController balloon;
        CircleCollider2D circle;
        readonly Dictionary<int, Hole> holes = new Dictionary<int, Hole>();
        readonly List<JetInfo> jets = new List<JetInfo>();
        readonly HashSet<int> seen = new HashSet<int>();
        readonly List<int> ended = new List<int>();

        void Awake()
        {
            balloon = GetComponent<BalloonController>();
            circle = GetComponent<CircleCollider2D>();
        }

        void OnEnable()
        {
            balloon.PatchedUp += OnPatched;
            balloon.BecameEmpty += OnEmpty;
        }

        void OnDisable()
        {
            balloon.PatchedUp -= OnPatched;
            balloon.BecameEmpty -= OnEmpty;
        }

        // PatchAll xóa jet rồi mới gọi PatchedUp, trước LateUpdate: đánh dấu để lúc kết thúc thì dán băng keo.
        void OnPatched() { foreach (var h in holes.Values) h.patched = true; }

        void OnEmpty()
        {
            foreach (var h in holes.Values) StopStream(h);
            holes.Clear();
        }

        void LateUpdate()
        {
            balloon.GetJets(jets);
            seen.Clear();
            bool frozen = Time.timeScale == 0f;

            foreach (var j in jets)
            {
                if (!j.isLeak) continue;
                seen.Add(j.id);
                if (!holes.TryGetValue(j.id, out var h))
                {
                    h = Create(j);
                    holes[j.id] = h;
                }
                float k = j.Strength01;
                if (h.stream != null)
                {
                    var em = h.stream.emission;
                    em.rateOverTime = maxRate * k;
                }
                if (h.audio != null)
                {
                    h.audio.volume = hissVolume * k;
                    if (frozen && !h.paused) { h.audio.Pause(); h.paused = true; }
                    else if (!frozen && h.paused) { h.audio.UnPause(); h.paused = false; }
                }
            }

            ended.Clear();
            foreach (var id in holes.Keys) if (!seen.Contains(id)) ended.Add(id);
            foreach (var id in ended)
            {
                End(holes[id]);
                holes.Remove(id);
            }
        }

        Hole Create(JetInfo leak)
        {
            Vector2 outward = leak.Outward.normalized;
            var h = new Hole();
            var go = new GameObject("Leak_" + leak.id);
            h.root = go.transform;
            h.root.SetParent(transform, false);
            h.root.localPosition = circle.offset + outward * circle.radius;
            h.root.localRotation = Quaternion.FromToRotation(Vector3.up, outward); // gốc bóng không xoay

            h.mark = NewSprite("Hole", h.root, holeSprite, holeColor, sortingOrder);
            h.mark.transform.localPosition = new Vector3(0f, -markInset, 0f); // root: +Y = hướng ra ngoài
            h.mark.transform.localScale = new Vector3(holeSize.x, holeSize.y, 1f);

            if (streamPrefab != null)
            {
                h.stream = Instantiate(streamPrefab, h.root, false);
                h.stream.Play();
            }
            if (hissLoop != null)
            {
                h.audio = go.AddComponent<AudioSource>();
                h.audio.clip = hissLoop;
                h.audio.loop = true;
                h.audio.playOnAwake = false;
                h.audio.spatialBlend = 0f;
                h.audio.volume = hissVolume;
                h.audio.Play();
            }
            return h;
        }

        void End(Hole h)
        {
            StopStream(h);
            if (h.root == null) return;
            if (h.patched)
            {
                // Băng keo chéo, giữ tới hết màn.
                if (h.mark != null) Destroy(h.mark.gameObject);
                for (int i = 0; i < 2; i++)
                {
                    var tape = NewSprite("Tape", h.root, tapeSprite != null ? tapeSprite : holeSprite, tapeColor, sortingOrder + 1);
                    tape.transform.localPosition = new Vector3(0f, -markInset, 0f);
                    tape.transform.localScale = new Vector3(0.24f, 0.07f, 1f);
                    tape.transform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 35f : -35f);
                }
            }
            else StartCoroutine(FadeOut(h.root, h.mark));
        }

        void StopStream(Hole h)
        {
            if (h.stream != null)
            {
                // Tách ra để hạt đã phun bay nốt rồi tự hủy.
                h.stream.transform.SetParent(null, true);
                h.stream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(h.stream.gameObject, 1.5f);
                h.stream = null;
            }
            if (h.audio != null)
            {
                h.audio.Stop();
                Destroy(h.audio);
                h.audio = null;
            }
        }

        IEnumerator FadeOut(Transform root, SpriteRenderer mark)
        {
            float t = 0f;
            Color c = mark != null ? mark.color : Color.clear;
            while (t < 0.4f && mark != null)
            {
                t += Time.deltaTime;
                mark.color = new Color(c.r, c.g, c.b, c.a * (1f - t / 0.4f));
                yield return null;
            }
            if (root != null) Destroy(root.gameObject);
        }

        SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;
            return sr;
        }
    }
}
