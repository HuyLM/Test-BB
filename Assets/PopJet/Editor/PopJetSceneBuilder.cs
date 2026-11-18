using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PopJet.EditorTools
{
    /// <summary>
    /// Dựng scene mẫu Pop Jet đúng như README (mục 3): bóng, input (kim + mũi tên),
    /// gai, keo vá, quạt, nút + cửa, đích, LevelManager, debug overlay.
    /// Tự sinh sprite, particle và âm thanh placeholder để chạy được ngay.
    /// Menu: Tools > PopJet > Build Sample Scene
    /// </summary>
    public static partial class PopJetSceneBuilder
    {
        const string Root = "Assets/PopJet";
        const string ArtDir = Root + "/Art";
        const string SfxDir = Root + "/Audio";
        const string PrefabDir = Root + "/Prefabs";
        const string ScenePath = Root + "/Scenes/PopJet_Sample.unity";
        const string TemplateScene = "Assets/Scenes/SampleScene.unity";

        static Sprite circle, triangle, square, dot;
        static Material spriteMat, particleMat;

        [MenuItem("Tools/PopJet/Build Sample Scene")]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        /// <summary>Dùng cho batchmode: -executeMethod PopJet.EditorTools.PopJetSceneBuilder.BuildFromCommandLine</summary>
        public static void BuildFromCommandLine()
        {
            Build();
        }

        static void Build()
        {
            EnsureDir(ArtDir); EnsureDir(SfxDir); EnsureDir(PrefabDir); EnsureDir(Root + "/Scenes");

            circle = MakeSprite("Circle", 256, (x, y, s) => Disc(x, y, s, 0.5f));
            triangle = MakeSprite("Triangle", 256, TriangleUp);
            square = MakeSprite("Square", 32, (x, y, s) => 1f);
            dot = MakeSprite("SoftDot", 64, (x, y, s) => SoftDot(x, y, s));

            spriteMat = LoadMat("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat")
                        ?? AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            particleMat = LoadMat("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat")
                          ?? spriteMat;

            var ventPuff = MakePuffPrefab("VentPuff", new Color(1f, 1f, 1f, 0.9f), 14, 3.5f);
            var leakPuff = MakePuffPrefab("LeakPuff", new Color(1f, 0.75f, 0.6f, 0.9f), 8, 2f);
            var patchPuff = MakePuffPrefab("PatchPuff", new Color(1f, 0.9f, 0.3f, 1f), 16, 1.5f, 180f);

            var ventClips = new[] { MakeNoiseClip("vent_1", 0.35f, 1), MakeNoiseClip("vent_2", 0.32f, 2), MakeNoiseClip("vent_3", 0.38f, 3) };
            var leakClips = new[] { MakeNoiseClip("leak_1", 0.6f, 4, 0.35f), MakeNoiseClip("leak_2", 0.55f, 5, 0.35f), MakeNoiseClip("leak_3", 0.65f, 6, 0.35f) };
            var patchClips = new[] { MakeToneClip("patch_1", 0.15f, 660f, 990f) };
            var emptyClip = MakeToneClip("empty", 0.7f, 440f, 110f);
            var goalClip = MakeToneClip("goal", 0.5f, 523f, 1046f);

            // Dùng SampleScene làm nền (đã có Main Camera orthographic size 5 + Global Light 2D).
            Scene scene = File.Exists(TemplateScene)
                ? EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.transform.position = new Vector3(0f, 0f, -10f);
                cam.backgroundColor = new Color(0.16f, 0.2f, 0.28f);
            }

            var level = new GameObject("Level").transform;

            // ---- Tường bao (màn dọc 9:16, size 5 => rộng ~5.6) ----
            var wallColor = new Color(0.45f, 0.48f, 0.55f);
            Wall("Wall_Left", new Vector2(-2.95f, 0f), new Vector2(0.3f, 10.6f), wallColor, level);
            Wall("Wall_Right", new Vector2(2.95f, 0f), new Vector2(0.3f, 10.6f), wallColor, level);
            Wall("Wall_Bottom", new Vector2(0f, -5.15f), new Vector2(6.2f, 0.3f), wallColor, level);
            Wall("Wall_Top", new Vector2(0f, 5.15f), new Vector2(6.2f, 0.3f), wallColor, level);
            // Buồng đích phía trên bên phải: vách đứng + cửa làm sàn.
            Wall("Wall_GoalDivider", new Vector2(1.0f, 3.35f), new Vector2(0.25f, 3.4f), wallColor, level);

            // ---- Bóng ----
            var balloonGo = new GameObject("Balloon");
            balloonGo.transform.position = new Vector3(0f, -3.6f, 0f);
            balloonGo.transform.localScale = Vector3.one * 0.9f;
            // Sprite nằm ở object con để BalloonBounce bóp dẹt mà không đụng scale gốc (scale gốc = lượng khí).
            var balloonVisual = new GameObject("Visual");
            balloonVisual.transform.SetParent(balloonGo.transform, false);
            AddSprite(balloonVisual, circle, new Color(0.93f, 0.33f, 0.3f), 10);
            var rb = balloonGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // camera follow mượt, không giật theo FixedUpdate
            var balloonCol = balloonGo.AddComponent<CircleCollider2D>();
            balloonCol.radius = 0.5f;
            balloonCol.sharedMaterial = MakeBouncyMaterial();
            var balloon = balloonGo.AddComponent<BalloonController>();
            var audio = balloonGo.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            var feedback = balloonGo.AddComponent<BalloonFeedback>();
            SetRefs(feedback,
                ("source", audio),
                ("ventPuffPrefab", ventPuff), ("leakPuffPrefab", leakPuff), ("patchPuffPrefab", patchPuff),
                ("emptyClip", emptyClip), ("goalClip", goalClip));
            SetArray(feedback, "ventClips", ventClips);
            SetArray(feedback, "leakClips", leakClips);
            SetArray(feedback, "patchClips", patchClips);

            var bounce = balloonGo.AddComponent<BalloonBounce>();
            SetRefs(bounce, ("visual", balloonVisual.transform), ("source", audio));
            SetArray(bounce, "bounceClips", new Object[]
            {
                MakeToneClip("bounce_1", 0.14f, 240f, 150f),
                MakeToneClip("bounce_2", 0.12f, 270f, 170f),
                MakeToneClip("bounce_3", 0.16f, 210f, 130f)
            });

            // ---- Input: kim + mũi tên hướng bay ----
            var inputGo = new GameObject("Input");
            var needle = new GameObject("Needle");
            needle.transform.localScale = new Vector3(0.22f, 0.7f, 1f);
            AddSprite(needle, triangle, new Color(0.85f, 0.85f, 0.88f), 20); // đầu nhọn hướng +Y

            var arrowGo = new GameObject("FlightArrow");
            var lr = arrowGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.widthMultiplier = 0.08f;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.3f));
            lr.startColor = lr.endColor = new Color(0.11f, 0.62f, 0.46f);
            lr.sharedMaterial = spriteMat;
            lr.sortingOrder = 15;
            lr.numCapVertices = 4;

            var input = inputGo.AddComponent<BalloonInput>();
            SetRefs(input, ("balloon", balloon), ("needle", needle.transform), ("flightArrow", lr));

            // ---- Gai (collider thường = chặn bóng) ----
            var spike = new GameObject("SpikeStrip");
            spike.transform.SetParent(level);
            spike.transform.position = new Vector3(0.4f, 0.2f, 0f);
            var spikeCol = spike.AddComponent<BoxCollider2D>();
            spikeCol.size = new Vector2(1.6f, 0.4f);
            spike.AddComponent<Spike>();
            for (int i = 0; i < 4; i++)
            {
                var t = new GameObject("Tooth_" + i);
                t.transform.SetParent(spike.transform, false);
                t.transform.localPosition = new Vector3(-0.6f + i * 0.4f, 0f, 0f);
                t.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                AddSprite(t, triangle, new Color(0.7f, 0.72f, 0.78f), 5);
                // Răng dưới chĩa xuống
                var b = new GameObject("ToothDown_" + i);
                b.transform.SetParent(spike.transform, false);
                b.transform.localPosition = new Vector3(-0.6f + i * 0.4f, 0f, 0f);
                b.transform.localRotation = Quaternion.Euler(0, 0, 180);
                b.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                AddSprite(b, triangle, new Color(0.7f, 0.72f, 0.78f), 5);
            }

            // ---- Keo vá ----
            var patch = new GameObject("Patch");
            patch.transform.SetParent(level);
            patch.transform.position = new Vector3(2.1f, -1.2f, 0f);
            patch.transform.localScale = Vector3.one * 0.5f;
            AddSprite(patch, circle, new Color(1f, 0.85f, 0.25f), 4);
            patch.AddComponent<CircleCollider2D>().isTrigger = true;
            var patchComp = patch.AddComponent<PatchPickup>();
            SetFloat(patchComp, "airRefill", 0.3f); // gấp 3 mức cũ (10% -> 30%)

            // ---- Quạt: +X local là hướng gió, xoay 90 độ để thổi lên ----
            var fan = new GameObject("Fan");
            fan.transform.SetParent(level);
            fan.transform.position = new Vector3(-1.75f, -0.4f, 0f);
            fan.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            var fanCol = fan.AddComponent<BoxCollider2D>();
            fanCol.size = new Vector2(3.4f, 1.6f);
            fanCol.isTrigger = true;
            fan.AddComponent<Fan>();
            var wind = new GameObject("WindZone");
            wind.transform.SetParent(fan.transform, false);
            wind.transform.localScale = new Vector3(3.4f, 1.6f, 1f);
            AddSprite(wind, square, new Color(0.5f, 0.85f, 1f, 0.15f), 1);
            var fanBody = new GameObject("FanBody");
            fanBody.transform.SetParent(fan.transform, false);
            fanBody.transform.localPosition = new Vector3(-1.7f, 0f, 0f);
            fanBody.transform.localScale = new Vector3(0.25f, 1.6f, 1f);
            AddSprite(fanBody, square, new Color(0.3f, 0.65f, 0.85f), 3);

            // ---- Cửa (sàn buồng đích) ----
            var door = new GameObject("Door");
            door.transform.SetParent(level);
            door.transform.position = new Vector3(1.95f, 1.65f, 0f);
            door.transform.localScale = new Vector3(1.75f, 0.25f, 1f);
            AddSprite(door, square, new Color(0.6f, 0.4f, 0.25f), 2);
            door.AddComponent<BoxCollider2D>();
            var doorComp = door.AddComponent<Door>();
            SetValue(doorComp, "openOffset", new Vector2(1.9f, 0f));

            // ---- Nút ----
            var button = new GameObject("Button");
            button.transform.SetParent(level);
            button.transform.position = new Vector3(-2.65f, 3.6f, 0f);
            button.transform.localScale = new Vector3(0.25f, 0.9f, 1f);
            AddSprite(button, square, new Color(0.9f, 0.25f, 0.45f), 2);
            button.AddComponent<BoxCollider2D>().isTrigger = true;
            var sw = button.AddComponent<ButtonSwitch>();
            SetArray(sw, "doors", new Object[] { doorComp });

            // ---- Đích ----
            var goal = new GameObject("Goal");
            goal.transform.SetParent(level);
            goal.transform.position = new Vector3(1.95f, 3.7f, 0f);
            goal.transform.localScale = Vector3.one * 1.1f;
            AddSprite(goal, circle, new Color(0.3f, 0.85f, 0.45f, 0.6f), 1);
            goal.AddComponent<CircleCollider2D>().isTrigger = true;
            goal.AddComponent<GoalZone>();

            // ---- LevelManager + Debug ----
            var lm = new GameObject("LevelManager").AddComponent<LevelManager>();
            SetRefs(lm, ("balloon", balloon));
            var dbg = new GameObject("PopJetDebug").AddComponent<PopJetDebug>();
            SetRefs(dbg, ("balloon", balloon));

            var vcamBuilt = BuildCinemachine(cam, balloonGo.transform);
            SetupPop(balloonGo, vcamBuilt);
            SetupPhase1(balloon, input, lm, vcamBuilt); // GDD Giai đoạn 1 (PopJetPhase1Builder.cs)

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[PopJet] Đã dựng scene mẫu: " + ScenePath);
        }

        // ---------------- Camera (Cinemachine, không tự code follow) ----------------

        const float FollowOrthoSize = 3.5f; // starting value: thấy ~7/10 chiều cao màn để camera có chỗ đi theo

        static CinemachineCamera BuildCinemachine(Camera cam, Transform target)
        {
            if (cam == null) return null;
            cam.orthographicSize = FollowOrthoSize;
            if (cam.GetComponent<CinemachineBrain>() == null) cam.gameObject.AddComponent<CinemachineBrain>();

            // Cố ý KHÔNG dùng CinemachineConfiner2D: nếu chặn camera ở mép màn thì bóng sát góc
            // sẽ nằm sát mép màn hình, không còn chỗ để kéo kim. Camera luôn giữ bóng gần giữa màn.
            var vcamGo = new GameObject("CM FollowCam");
            vcamGo.transform.position = new Vector3(target.position.x, target.position.y, -10f);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Follow = target;
            var lens = vcam.Lens;
            lens.OrthographicSize = FollowOrthoSize;
            lens.NearClipPlane = 0.3f;
            lens.FarClipPlane = 1000f;
            vcam.Lens = lens;

            var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.4f, 0.4f, 0f); // starting value: độ trễ follow
            var comp = composer.Composition;
            comp.DeadZone.Enabled = true;
            comp.DeadZone.Size = new Vector2(0.1f, 0.1f); // bóng rung nhẹ khi nảy không kéo camera theo
            composer.Composition = comp;
            var look = composer.Lookahead;
            look.Enabled = true;
            look.Time = 0.25f;     // nhìn trước hướng bay một chút
            look.Smoothing = 8f;
            look.IgnoreY = false;
            composer.Lookahead = look;
            return vcam;
        }

        // ---------------- Bóng vỡ tung khi hết khí ----------------

        /// <summary>Áp dụng hiệu ứng vỡ cho scene đang mở (không dựng lại scene, giữ nguyên chỉnh tay).</summary>
        [MenuItem("Tools/PopJet/Add Balloon Pop To Open Scene")]
        public static void AddPopToOpenScene()
        {
            var balloon = Object.FindAnyObjectByType<BalloonController>();
            if (balloon == null) { Debug.LogError("[PopJet] Không tìm thấy BalloonController trong scene."); return; }
            LoadSharedAssets();
            SetupPop(balloon.gameObject, Object.FindAnyObjectByType<CinemachineCamera>());
            var scene = balloon.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PopJet] Đã thêm hiệu ứng vỡ bóng vào " + scene.path);
        }

        static void LoadSharedAssets()
        {
            EnsureDir(ArtDir); EnsureDir(SfxDir); EnsureDir(PrefabDir);
            if (square == null) square = MakeSprite("Square", 32, (x, y, s) => 1f);
            if (dot == null) dot = MakeSprite("SoftDot", 64, (x, y, s) => SoftDot(x, y, s));
            if (particleMat == null)
                particleMat = LoadMat("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat")
                              ?? AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        }

        static void SetupPop(GameObject balloonGo, CinemachineCamera vcam)
        {
            var feedback = balloonGo.GetComponent<BalloonFeedback>();
            if (feedback == null) feedback = balloonGo.AddComponent<BalloonFeedback>();

            SetRefs(feedback, ("popBurstPrefab", MakePopBurstPrefab()));
            SetArray(feedback, "popClips", new Object[]
            {
                MakePopClip("pop_1", 11), MakePopClip("pop_2", 12), MakePopClip("pop_3", 13)
            });

            // Rung camera bằng Cinemachine Impulse (không tự code shake).
            var impulse = balloonGo.GetComponent<CinemachineImpulseSource>();
            if (impulse == null) impulse = balloonGo.AddComponent<CinemachineImpulseSource>();
            var def = impulse.ImpulseDefinition;
            def.ImpulseChannel = 1;
            def.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
            def.ImpulseDuration = 0.35f;                 // starting value
            def.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.DefaultVelocity = new Vector3(0.18f, 0.18f, 0f); // starting value: biên độ rung (đơn vị world)
            EditorUtility.SetDirty(impulse);

            if (vcam != null)
            {
                var listener = vcam.GetComponent<CinemachineImpulseListener>();
                if (listener == null) listener = vcam.gameObject.AddComponent<CinemachineImpulseListener>();
                listener.ChannelMask = 1;
                listener.Gain = 1f;
                listener.Use2DDistance = true;
                EditorUtility.SetDirty(listener);
            }

            // Nối onPopped -> impulse.GenerateImpulse() (persistent listener, thấy được trong Inspector).
            var so = new SerializedObject(feedback);
            var calls = so.FindProperty("onPopped.m_PersistentCalls.m_Calls");
            bool exists = false;
            for (int i = 0; i < calls.arraySize; i++)
            {
                var c = calls.GetArrayElementAtIndex(i);
                if (c.FindPropertyRelative("m_Target").objectReferenceValue == impulse &&
                    c.FindPropertyRelative("m_MethodName").stringValue == "GenerateImpulse") exists = true;
            }
            if (!exists)
            {
                int idx = calls.arraySize;
                calls.arraySize++;
                var c = calls.GetArrayElementAtIndex(idx);
                c.FindPropertyRelative("m_Target").objectReferenceValue = impulse;
                c.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = $"{typeof(CinemachineImpulseSource).FullName}, {typeof(CinemachineImpulseSource).Assembly.GetName().Name}";
                c.FindPropertyRelative("m_MethodName").stringValue = "GenerateImpulse";
                c.FindPropertyRelative("m_Mode").intValue = 1; // PersistentListenerMode.Void
                c.FindPropertyRelative("m_CallState").intValue = 2; // RuntimeOnly
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static ParticleSystem MakePopBurstPrefab()
        {
            string path = $"{PrefabDir}/PopBurst.prefab";

            // Material mảnh vỡ: hình chữ nhật đặc (mảnh cao su).
            string shredMatPath = $"{ArtDir}/PopJetShred.mat";
            var shredMat = AssetDatabase.LoadAssetAtPath<Material>(shredMatPath);
            if (shredMat == null && particleMat != null)
            {
                shredMat = new Material(particleMat);
                if (shredMat.HasProperty("_BaseMap")) shredMat.SetTexture("_BaseMap", square.texture);
                AssetDatabase.CreateAsset(shredMat, shredMatPath);
            }
            var puffMat = AssetDatabase.LoadAssetAtPath<Material>($"{ArtDir}/PopJetParticle.mat") ?? particleMat;

            // Gốc: mảnh vỡ cùng màu bóng (BalloonFeedback tô màu lúc nổ), bắn ra mọi hướng rồi rơi.
            var root = new GameObject("PopBurst");
            var shreds = root.AddComponent<ParticleSystem>();
            shreds.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = shreds.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startSizeZ = 1f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.93f, 0.33f, 0.3f);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = shreds.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)28) });
            var shape = shreds.shape;
            shape.shapeType = ParticleSystemShapeType.Circle; // mặt phẳng XY => bắn 360 độ trong 2D
            shape.radius = 0.3f;
            var limit = shreds.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 2.5f; // bắn mạnh rồi chậm lại nhanh, như mảnh cao su
            var rot = shreds.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-12f, 12f);
            var col = shreds.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = root.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = shredMat;
            r.sortingOrder = 31;

            // Con 1: chớp sáng vòng tròn ngay tại chỗ nổ.
            var flash = new GameObject("Flash");
            flash.transform.SetParent(root.transform, false);
            var fps = flash.AddComponent<ParticleSystem>();
            fps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var fm = fps.main;
            fm.duration = 0.1f; fm.loop = false;
            fm.startLifetime = 0.12f;
            fm.startSpeed = 0f;
            fm.startSize = 1.8f;
            fm.startColor = new Color(1f, 1f, 1f, 0.9f);
            fm.simulationSpace = ParticleSystemSimulationSpace.World;
            var fem = fps.emission; fem.rateOverTime = 0f; fem.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            var fshape = fps.shape; fshape.enabled = false;
            var fsize = fps.sizeOverLifetime; fsize.enabled = true;
            fsize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.3f));
            var fcol = fps.colorOverLifetime; fcol.enabled = true;
            var fg = new Gradient();
            fg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                       new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fcol.color = fg;
            var fr = flash.GetComponent<ParticleSystemRenderer>();
            fr.sharedMaterial = puffMat;
            fr.sortingOrder = 32;

            // Con 2: khí thoát ra thành làn bụi trắng.
            var puff = new GameObject("Puff");
            puff.transform.SetParent(root.transform, false);
            var pps = puff.AddComponent<ParticleSystem>();
            pps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var pm = pps.main;
            pm.duration = 0.3f; pm.loop = false;
            pm.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            pm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            pm.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            pm.startColor = new Color(1f, 1f, 1f, 0.7f);
            pm.simulationSpace = ParticleSystemSimulationSpace.World;
            var pem = pps.emission; pem.rateOverTime = 0f; pem.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)18) });
            var pshape = pps.shape; pshape.shapeType = ParticleSystemShapeType.Circle; pshape.radius = 0.25f;
            var pcol = pps.colorOverLifetime; pcol.enabled = true; pcol.color = fg;
            var pr = puff.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = puffMat;
            pr.sortingOrder = 30;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ParticleSystem>();
        }

        static AudioClip MakePopClip(string name, int seed)
        {
            // Tiếng "bụp": nhiễu trắng tấn công cực nhanh, tắt nhanh + tiếng trầm thump.
            var rnd = new System.Random(seed);
            float seconds = 0.22f;
            int n = (int)(seconds * Rate);
            var data = new float[n];
            double phase = 0;
            float thumpHz = 70f + seed % 3 * 12f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float crack = (float)(rnd.NextDouble() * 2 - 1) * Mathf.Exp(-t * 45f);
                phase += 2 * System.Math.PI * Mathf.Lerp(thumpHz * 1.8f, thumpHz, t / seconds) / Rate;
                float thump = (float)System.Math.Sin(phase) * Mathf.Exp(-t * 18f);
                data[i] = Mathf.Clamp(crack * 0.9f + thump * 0.7f, -1f, 1f) * 0.85f;
            }
            return WriteWav(name, data);
        }

        static PhysicsMaterial2D MakeBouncyMaterial()
        {
            string path = $"{ArtDir}/BalloonBouncy.physicsMaterial2D";
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (m == null)
            {
                m = new PhysicsMaterial2D("BalloonBouncy");
                AssetDatabase.CreateAsset(m, path);
            }
            m.bounciness = 0.85f; // starting value: nảy rõ; damping 1.5 của bóng sẽ hãm bớt nên không bật như bóng cao su
            m.friction = 0.05f;  // trơn, không bám tường khi trượt
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------- Helpers: scene ----------------

        static SpriteRenderer AddSprite(GameObject go, Sprite sprite, Color color, int order)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (spriteMat != null) sr.sharedMaterial = spriteMat;
            return sr;
        }

        static void Wall(string name, Vector2 pos, Vector2 size, Color color, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            AddSprite(go, square, color, 0);
            go.AddComponent<BoxCollider2D>();
        }

        static void SetRefs(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var p = so.FindProperty(field);
                if (p == null) { Debug.LogError($"[PopJet] Không tìm thấy field {field} trên {target}"); continue; }
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetValue(Object target, string field, Vector2 v)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).vector2Value = v;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float v)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = v;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddToBuildSettings(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == path)) return;
            list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ---------------- Helpers: assets ----------------

        static void EnsureDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static Material LoadMat(string path) => AssetDatabase.LoadAssetAtPath<Material>(path);

        delegate float Shape(float x, float y, int size);

        static Sprite MakeSprite(string name, int size, Shape alpha)
        {
            string path = $"{ArtDir}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f, size)));
                tex.SetPixels32(px);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = size; // 1 sprite = 1 đơn vị world
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static float Disc(float x, float y, int s, float r)
        {
            float d = new Vector2(x / s - 0.5f, y / s - 0.5f).magnitude;
            return (r - d) * s; // khử răng cưa ~1px
        }

        static float SoftDot(float x, float y, int s)
        {
            float d = new Vector2(x / s - 0.5f, y / s - 0.5f).magnitude * 2f;
            return 1f - Mathf.SmoothStep(0f, 1f, d);
        }

        static float TriangleUp(float x, float y, int s)
        {
            // Đỉnh ở (0.5, 1), đáy từ (0,0) đến (1,0)
            float u = x / s, v = y / s;
            float halfWidth = 0.5f * (1f - v);
            return (halfWidth - Mathf.Abs(u - 0.5f)) * s;
        }

        static ParticleSystem MakePuffPrefab(string name, Color color, int count, float speed, float angle = 22f)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // phát theo +Y local

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = particleMat;
            r.sortingOrder = 30;
            if (particleMat != null && dot != null && particleMat.HasProperty("_BaseMap"))
            {
                // Không sửa material gốc của package: tạo bản riêng có texture chấm mềm.
                string matPath = $"{ArtDir}/PopJetParticle.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (m == null)
                {
                    m = new Material(particleMat);
                    m.SetTexture("_BaseMap", dot.texture);
                    AssetDatabase.CreateAsset(m, matPath);
                }
                r.sharedMaterial = m;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ParticleSystem>();
        }

        // ---- Âm thanh placeholder (WAV 16-bit mono) ----

        const int Rate = 22050;

        static AudioClip MakeNoiseClip(string name, float seconds, int seed, float volume = 0.6f)
        {
            var rnd = new System.Random(seed);
            int n = (int)(seconds * Rate);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float env = Mathf.Min(1f, t * 30f) * (1f - t) * (1f - t);
                float white = (float)(rnd.NextDouble() * 2 - 1);
                lp += (white - lp) * 0.35f; // lọc thấp nhẹ cho tiếng "xì"
                data[i] = lp * env * volume;
            }
            return WriteWav(name, data);
        }

        static AudioClip MakeToneClip(string name, float seconds, float f0, float f1)
        {
            int n = (int)(seconds * Rate);
            var data = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += 2 * System.Math.PI * f / Rate;
                float env = Mathf.Min(1f, t * 40f) * (1f - t);
                data[i] = (float)System.Math.Sin(phase) * env * 0.4f;
            }
            return WriteWav(name, data);
        }

        static AudioClip WriteWav(string name, float[] samples)
        {
            string path = $"{SfxDir}/{name}.wav";
            if (!File.Exists(path))
            {
                using (var fs = new FileStream(path, FileMode.Create))
                using (var w = new BinaryWriter(fs))
                {
                    int dataLen = samples.Length * 2;
                    w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    w.Write(36 + dataLen);
                    w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    w.Write(16); w.Write((short)1); w.Write((short)1);
                    w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
                    w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    w.Write(dataLen);
                    foreach (var s in samples) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
                }
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
