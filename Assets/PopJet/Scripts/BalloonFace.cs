using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Mặt bóng (GDD §4.1): hai mắt, đốm sáng, miệng và vệt highlight, tự dựng lúc Play bằng sprite đơn giản
    /// (sau này thay bằng art AI theo §9.6).
    /// - Mắt nhìn theo ngón tay khi ngắm, nhìn về gai gần nhất, còn lại nhìn theo hướng bay.
    /// - Biểu cảm: chờ (chớp mắt), ngắm (nheo một mắt), "ui!" khi chọc hoặc va mạnh, hoảng khi có lỗ rò
    ///   hoặc ở gần gai, mệt khi còn chưa tới 1 cú chọc, vui khi về đích, buồn khi xẹp.
    /// - BalloonBounce xoay Visual theo pháp tuyến va chạm, nên mặt được xoay bù để luôn thẳng đứng
    ///   (vẫn bị bóp dẹt theo cú va).
    /// Chạy bằng unscaled time: thế giới dừng khi ngắm nhưng mắt vẫn phải nhìn theo ngón tay.
    /// </summary>
    [DefaultExecutionOrder(100)] // LateUpdate chạy sau BalloonBounce để xoay bù đúng frame
    [RequireComponent(typeof(BalloonController))]
    public class BalloonFace : MonoBehaviour
    {
        [Tooltip("Object con chứa sprite thân bóng (cùng object BalloonBounce bóp dẹt).")]
        [SerializeField] Transform visual;
        [SerializeField] BalloonInput input;
        [SerializeField] Sprite circleSprite;
        [Tooltip("Sprite nét cong hình chữ U cho miệng cười.")]
        [SerializeField] Sprite smileSprite;
        [SerializeField] Sprite squareSprite;
        [SerializeField] Material material;
        [SerializeField] Color inkColor = new Color(0.17f, 0.17f, 0.23f); // #2B2B3A
        [Tooltip("Gai trong khoảng này (tính từ mặt bóng) thì mắt nhìn về phía nó và bóng sợ hãi.")]
        [SerializeField] float hazardRange = 1.5f;
        [SerializeField] int sortingOrder = 11;
        [Header("Art AI (tuỳ chọn)")]
        [Tooltip("12 mặt vẽ sẵn (Art/World1/Faces, thứ tự Face_00 … Face_11). Có đủ 12 thì dùng thay cho mặt dựng bằng hình tròn.")]
        [SerializeField] Sprite[] faceSprites;
        [Tooltip("Cạnh ảnh mặt so với đường kính bóng (ảnh cắt rộng 1.3 lần để chứa Zz và vạch sốc).")]
        [SerializeField] float faceArtScale = 1.3f;

        enum Mood { Normal, Aim, Ouch, Scared, Tired, Happy, Sad }

        static readonly Vector2 EyeL = new Vector2(-0.13f, 0.05f);
        static readonly Vector2 EyeR = new Vector2(0.13f, 0.05f);
        static readonly Vector2 EyeSize = new Vector2(0.11f, 0.15f);
        const float LookAmount = 0.035f;

        BalloonController balloon;
        BalloonBounce bounce;
        Transform face;
        SpriteRenderer eyeL, eyeR, shineL, shineR, smile, flat, mouthO;
        SpriteRenderer faceArt;
        bool ouchDizzy;
        float winkUntil, nextWink;

        // Chỉ số trong faceSprites (cùng thứ tự với ô trên tấm Balloon_Faces).
        const int FNeutral = 0, FHappy = 1, FLaugh = 2, FScared = 3, FDizzy = 4, FSleepy = 5,
                  FDetermined = 6, FSurprised = 7, FSad = 8, FWink = 9, FWorried = 10, FExcited = 11;
        bool UseArt => faceSprites != null && faceSprites.Length >= 12;
        Vector2 look;
        float ouchUntil, nextBlink, blinkUntil, nextScan;
        bool nearHazard;
        Vector2 hazardDir;
        readonly Collider2D[] overlap = new Collider2D[16];

        void Awake()
        {
            balloon = GetComponent<BalloonController>();
            bounce = GetComponent<BalloonBounce>();
            if (visual == null) visual = transform.Find("Visual");
            Build();
        }

        void OnEnable()
        {
            balloon.VentStarted += OnVent;
            if (bounce != null) bounce.Bounced += OnBounce;
        }

        void OnDisable()
        {
            balloon.VentStarted -= OnVent;
            if (bounce != null) bounce.Bounced -= OnBounce;
        }

        void OnVent(Vector2 hole, Vector2 dir) { ouchUntil = Time.unscaledTime + 0.3f; ouchDizzy = false; }
        void OnBounce(float strength)
        {
            if (strength < 0.8f) return;
            ouchUntil = Time.unscaledTime + (UseArt ? 0.5f : 0.35f);
            ouchDizzy = true;
        }

        void LateUpdate()
        {
            if (face == null) return;
            if (visual != null) face.localRotation = Quaternion.Inverse(visual.localRotation);

            float now = Time.unscaledTime;
            ScanHazards(now);
            Mood mood = PickMood(now);
            look = Vector2.Lerp(look, LookTarget(mood), 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            Apply(mood, now);
        }

        Mood PickMood(float now)
        {
            var state = balloon.State;
            if (state == BalloonState.Goal) return Mood.Happy;
            if (state == BalloonState.Empty) return Mood.Sad;
            if (now < ouchUntil) return Mood.Ouch;
            if (balloon.HasLeaks) return Mood.Scared;
            if (input != null && input.IsAiming) return Mood.Aim;
            if (nearHazard) return Mood.Scared;
            if (balloon.Started && balloon.IsLastPoke) return Mood.Tired;
            return Mood.Normal;
        }

        Vector2 LookTarget(Mood mood)
        {
            if (mood == Mood.Aim)
            {
                Vector2 d = input.PointerWorld - balloon.Center;
                return d.sqrMagnitude > 1e-4f ? d.normalized * LookAmount : Vector2.zero;
            }
            if (nearHazard) return hazardDir * LookAmount;
            Vector2 v = balloon.Velocity;
            return v.magnitude > 0.3f ? v.normalized * (LookAmount * 0.8f) : Vector2.zero;
        }

        void ScanHazards(float now)
        {
            if (now < nextScan) return;
            nextScan = now + 0.1f;
            nearHazard = false;
            if (!balloon.CanAim) return;

            var filter = new ContactFilter2D();
            filter.useTriggers = true;
            Vector2 c = balloon.Center;
            int n = Physics2D.OverlapCircle(c, balloon.Radius + hazardRange, filter, overlap);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var col = overlap[i];
                if (col == null || col.GetComponentInParent<Spike>() == null) continue;
                Vector2 to = col.ClosestPoint(c) - c;
                float d = to.sqrMagnitude;
                if (d < best && d > 1e-6f)
                {
                    best = d;
                    hazardDir = to.normalized;
                    nearHazard = true;
                }
            }
        }

        void Apply(Mood mood, float now)
        {
            if (faceArt != null) { ApplyArt(mood, now); return; }
            Vector2 scaleL = Vector2.one, scaleR = Vector2.one;
            float lift = 0f;
            int mouth = 0; // 0 cười, 1 ngang, 2 mếu, 3 chữ O, 4 cười to

            switch (mood)
            {
                case Mood.Aim: scaleR.y = 0.3f; mouth = 1; break;
                case Mood.Ouch: scaleL.y = scaleR.y = 0.2f; mouth = 3; break;
                case Mood.Scared: scaleL = scaleR = new Vector2(1.2f, 1.2f); mouth = 3; break;
                case Mood.Tired: scaleL.y = scaleR.y = 0.5f; lift = -0.02f; mouth = 2; break;
                case Mood.Happy: scaleL.y = scaleR.y = 0.35f; lift = 0.02f; mouth = 4; break;
                case Mood.Sad: scaleL.y = scaleR.y = 0.6f; mouth = 2; break;
            }

            // Chớp mắt ngẫu nhiên khi bình thường, mệt, hoặc đang ngắm.
            if (mood == Mood.Normal || mood == Mood.Tired || mood == Mood.Aim)
            {
                if (now >= nextBlink)
                {
                    blinkUntil = now + 0.12f;
                    nextBlink = now + Random.Range(2.5f, 5f);
                }
                if (now < blinkUntil) { scaleL.y *= 0.12f; scaleR.y *= 0.12f; }
            }

            Vector2 offset = look + new Vector2(0f, lift);
            PlaceEye(eyeL, shineL, EyeL + offset, scaleL);
            PlaceEye(eyeR, shineR, EyeR + offset, scaleR);

            smile.enabled = mouth == 0 || mouth == 2 || mouth == 4;
            flat.enabled = mouth == 1;
            mouthO.enabled = mouth == 3;
            if (mouth == 0) SetLocal(smile.transform, new Vector2(0f, -0.14f), new Vector2(0.2f, 0.2f));
            else if (mouth == 2) SetLocal(smile.transform, new Vector2(0f, -0.17f), new Vector2(0.18f, -0.16f));
            else if (mouth == 4) SetLocal(smile.transform, new Vector2(0f, -0.13f), new Vector2(0.28f, 0.32f));
        }

        /// <summary>Mặt vẽ sẵn: chọn ô theo biểu cảm, cả mặt dịch theo hướng nhìn; lúc rảnh thỉnh thoảng nháy mắt.</summary>
        void ApplyArt(Mood mood, float now)
        {
            int idx;
            switch (mood)
            {
                case Mood.Aim: idx = FDetermined; break;
                case Mood.Ouch: idx = ouchDizzy ? FDizzy : FSurprised; break;
                case Mood.Scared: idx = balloon.HasLeaks ? FScared : FWorried; break;
                case Mood.Tired: idx = FSleepy; break;
                case Mood.Happy: idx = Mathf.Repeat(now, 0.6f) < 0.3f ? FExcited : FLaugh; break;
                case Mood.Sad: idx = FSad; break;
                default: idx = balloon.Started ? FHappy : FNeutral; break;
            }
            if (mood == Mood.Normal)
            {
                if (now >= nextWink)
                {
                    winkUntil = now + 0.35f;
                    nextWink = now + Random.Range(4f, 8f);
                }
                if (now < winkUntil) idx = FWink;
            }
            faceArt.sprite = faceSprites[idx];
            float lift = mood == Mood.Tired ? -0.02f : 0f;
            faceArt.transform.localPosition = new Vector3(look.x, look.y + lift, 0f);
        }

        void PlaceEye(SpriteRenderer eye, SpriteRenderer shine, Vector2 pos, Vector2 scale)
        {
            SetLocal(eye.transform, pos, Vector2.Scale(EyeSize, scale));
            shine.enabled = scale.y > 0.5f;
            SetLocal(shine.transform, pos + new Vector2(-0.022f, 0.035f * scale.y), new Vector2(0.035f, 0.035f));
        }

        static void SetLocal(Transform t, Vector2 pos, Vector2 scale)
        {
            t.localPosition = new Vector3(pos.x, pos.y, 0f);
            t.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        void Build()
        {
            if (visual == null || circleSprite == null) return;
            face = new GameObject("Face").transform;
            face.SetParent(visual, false);

            if (UseArt)
            {
                faceArt = Part("FaceArt", faceSprites[FNeutral], Color.white, sortingOrder + 1);
                float w = faceSprites[FNeutral].bounds.size.x;
                if (w > 1e-4f) faceArt.transform.localScale = Vector3.one * (faceArtScale / w);
                nextWink = Time.unscaledTime + Random.Range(2f, 5f);
                return; // thân bóng vẽ sẵn đã có vệt sáng
            }

            var highlight = Part("Highlight", circleSprite, new Color(1f, 1f, 1f, 0.55f), sortingOrder);
            SetLocal(highlight.transform, new Vector2(-0.2f, 0.22f), new Vector2(0.22f, 0.11f));
            highlight.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);

            eyeL = Part("EyeL", circleSprite, inkColor, sortingOrder + 1);
            eyeR = Part("EyeR", circleSprite, inkColor, sortingOrder + 1);
            shineL = Part("ShineL", circleSprite, Color.white, sortingOrder + 2);
            shineR = Part("ShineR", circleSprite, Color.white, sortingOrder + 2);
            smile = Part("MouthSmile", smileSprite != null ? smileSprite : circleSprite, inkColor, sortingOrder + 1);
            flat = Part("MouthFlat", squareSprite != null ? squareSprite : circleSprite, inkColor, sortingOrder + 1);
            SetLocal(flat.transform, new Vector2(0f, -0.14f), new Vector2(0.12f, 0.024f));
            mouthO = Part("MouthO", circleSprite, inkColor, sortingOrder + 1);
            SetLocal(mouthO.transform, new Vector2(0f, -0.15f), new Vector2(0.07f, 0.09f));
            nextBlink = Time.unscaledTime + Random.Range(1f, 3f);
        }

        SpriteRenderer Part(string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(face, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (material != null) sr.sharedMaterial = material;
            return sr;
        }
    }
}
