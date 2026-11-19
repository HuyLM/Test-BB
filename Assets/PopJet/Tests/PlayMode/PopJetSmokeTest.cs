using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Smoke test tạm thời: chạy scene mẫu và kích hoạt các sự kiện chính của Giai đoạn 1.
// Asmdef không tham chiếu được Assembly-CSharp nên gọi component bằng reflection.
public class PopJetSmokeTest
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    static Component Comp(string go, string type)
    {
        var o = GameObject.Find(go);
        Assert.IsNotNull(o, "Không thấy object " + go);
        var c = o.GetComponent(type);
        Assert.IsNotNull(c, "Không thấy component " + type);
        return c;
    }

    static object Call(Component c, string method, params object[] args) => c.GetType().GetMethod(method, All).Invoke(c, args);
    static object Get(Component c, string prop) => c.GetType().GetProperty(prop, All).GetValue(c);
    static void SetField(Component c, string f, object v) => c.GetType().GetField(f, All).SetValue(c, v);

    // Nhãn HUD là TextMeshPro: asmdef không tham chiếu TMP nên đọc thuộc tính text/color bằng reflection.
    static Component Tmp(string path)
    {
        var go = GameObject.Find(path);
        Assert.IsNotNull(go, "Không thấy " + path);
        foreach (var c in go.GetComponents<Component>())
            if (c.GetType().Name == "TextMeshProUGUI") return c;
        Assert.Fail("Không có TextMeshProUGUI trên " + path);
        return null;
    }
    static string LabelText(string path) => (string)Get(Tmp(path), "text");
    static Color LabelColor(string path) => (Color)Get(Tmp(path), "color");

    static System.Type RuntimeType(string name) => System.Type.GetType("PopJet." + name + ", Assembly-CSharp");
    static Component Screens => Comp("HUD", "GameScreens");

    static readonly string SavePath = System.IO.Path.Combine(Application.temporaryCachePath, "popjet_test_save.json");
    static System.Type SaveType => System.Type.GetType("PopJet.SaveSystem, Assembly-CSharp");
    static object Save(string method, params object[] args) => SaveType.GetMethod(method).Invoke(null, args);

    // Mỗi test dùng file save riêng, trống: không đụng vào save thật của người chơi.
    [SetUp]
    public void UseTempSave()
    {
        foreach (var f in new[] { SavePath, SavePath + ".bak", SavePath + ".tmp" })
            if (System.IO.File.Exists(f)) System.IO.File.Delete(f);
        Save("UseFile", SavePath);
        // Tắt camera giới thiệu đầu màn: nó dừng thế giới nên WaitForSeconds sẽ không bao giờ xong.
        RuntimeType("GameFlow").GetField("IntroEnabled").SetValue(null, false);
    }

    [TearDown]
    public void RestoreFlow()
    {
        RuntimeType("GameFlow").GetField("IntroEnabled").SetValue(null, true);
        RuntimeType("TimeController").GetMethod("ResetAll").Invoke(null, null);
    }

    static object NewResult(string id, bool star, bool underPar, bool crown)
    {
        var r = System.Activator.CreateInstance(System.Type.GetType("PopJet.LevelResult, Assembly-CSharp"));
        var t = r.GetType();
        t.GetField("levelId").SetValue(r, id);
        t.GetField("goal").SetValue(r, true);
        t.GetField("star").SetValue(r, star);
        t.GetField("underPar").SetValue(r, underPar);
        t.GetField("crown").SetValue(r, crown);
        t.GetField("pokes").SetValue(r, 2);
        return r;
    }

    [Test]
    public void SaveSystem_PersistsMergesAndRecoversFromCorruptFile()
    {
        Save("MergeResult", NewResult("t-1", true, false, false));
        Save("MergeResult", NewResult("t-1", false, true, false)); // lượt sau: từng sao gộp riêng
        Save("SetLastLevel", 1);

        Save("UseFile", SavePath); // bỏ bộ nhớ, đọc lại từ đĩa
        var rec = Save("GetRecord", "t-1");
        Assert.IsNotNull(rec, "Thành tích phải còn sau khi đọc lại file");
        Assert.AreEqual(3, Prop(rec, "Stars"));
        Assert.AreEqual(2, Field(rec, "bestPokes"));
        var data = SaveType.GetProperty("Data").GetValue(null);
        Assert.AreEqual(1, Field(data, "lastLevelIndex"));

        // File chính hỏng: đọc bản .bak (bản trước lần lưu cuối, vẫn có t-1).
        System.IO.File.WriteAllText(SavePath, "{ hỏng");
        Save("UseFile", SavePath);
        Assert.IsNotNull(Save("GetRecord", "t-1"), "Phải khôi phục từ .bak");

        // Hỏng cả hai: bắt đầu lại trống, không ném lỗi.
        System.IO.File.WriteAllText(SavePath + ".bak", "xyz");
        Save("UseFile", SavePath);
        Assert.IsNull(Save("GetRecord", "t-1"));
        Assert.AreEqual(0, Field(SaveType.GetProperty("Data").GetValue(null), "lastLevelIndex"));
    }

    [UnityTest]
    public IEnumerator SaveSystem_ResumesLastLevelAndUnlocks()
    {
        Save("SetLastLevel", 1);
        // Giả lập mở game lần đầu: LevelLoader chưa có màn nào trong bộ nhớ.
        System.Type.GetType("PopJet.LevelLoader, Assembly-CSharp")
            .GetField("currentIndex", All | BindingFlags.Static).SetValue(null, -1);
        SceneManager.LoadScene(0);
        yield return null;
        yield return null;
        Assert.IsNotNull(GameObject.Find("Level_1-2"), "Phải chơi tiếp màn gần nhất trong file save");

        var catalog = Get(Comp("LevelLoader", "LevelLoader"), "Catalog");
        var isUnlocked = catalog.GetType().GetMethod("IsUnlocked");
        Assert.IsFalse((bool)isUnlocked.Invoke(catalog, new object[] { 1 }), "1-2 chưa mở khi 1-1 chưa về đích");

        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 0);
        yield return null;
        yield return null;
        Call(Comp("Balloon", "BalloonController"), "ReachGoal");
        yield return null;
        Assert.IsTrue((bool)isUnlocked.Invoke(catalog, new object[] { 1 }), "Về đích 1-1 thì mở 1-2");
        StringAssert.Contains("\"1-1\"", System.IO.File.ReadAllText(SavePath), "Về đích phải ghi file ngay");

        yield return new WaitForSeconds(1.6f);
        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 0);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator SampleScene_Phase1_RunsWithoutErrors()
    {
        SceneManager.LoadScene(0);
        yield return null;
        yield return null;

        var balloon = Comp("Balloon", "BalloonController");
        var input = Comp("Input", "BalloonInput");
        Assert.IsNotNull(GameObject.Find("HUD/SafeArea/RetryButton"), "HUD");
        Assert.IsNotNull(balloon.transform.Find("Visual/Face/EyeL"), "Mặt bóng");
        Assert.IsNotNull(balloon.transform.Find("String"), "Dây bóng");
        Assert.AreEqual(60, Application.targetFrameRate, "60 fps");
        Assert.IsNotNull(GameObject.Find("Level_1-1"), "LevelLoader phải tạo prefab màn đầu tiên");
        var spawn = GameObject.Find("Level_1-1/Spawn");
        Assert.Less(Vector2.Distance(spawn.transform.position, balloon.transform.position), 0.01f, "Bóng ở điểm xuất phát");
        yield return new WaitForSeconds(0.3f);

        // Giả lập đang giữ tay ngắm, kim chĩa xuống => bóng sẽ bay lên.
        // Batchmode không có ngón tay giữ màn hình nên tắt BalloonInput, nếu không nó tự hủy ngắm ở frame sau.
        ((Behaviour)input).enabled = false;
        SetField(input, "aiming", true);
        SetField(input, "cancelNow", false);
        SetField(input, "needleDir", Vector2.down);
        Call(balloon, "SetAiming", true);
        yield return null;
        yield return null;
        var dot = input.transform.Find("Dot_0");
        Assert.IsNotNull(dot, "Quỹ đạo dự đoán");
        Assert.IsTrue(dot.GetComponent<SpriteRenderer>().enabled, "Chấm quỹ đạo phải hiện");
        Assert.IsNotNull(input.transform.Find("Fill"), "Vòng khí");
        Assert.IsTrue(input.transform.Find("Fill").GetComponent<LineRenderer>().enabled, "Vòng khí phải hiện");
        var airBar = GameObject.Find("HUD/SafeArea/AirBar");
        Assert.Greater(airBar.transform.childCount, 4, "Thanh khí chia ô");
        SetField(input, "aiming", false);
        Call(balloon, "SetAiming", false);
        ((Behaviour)input).enabled = true;

        // Chọc ngang để không bay vào đích ở phía trên của màn 1-1.
        Assert.IsTrue((bool)Call(balloon, "TryVent", Vector2.right));
        Assert.AreEqual(1, Get(balloon, "PokeCount"));
        yield return new WaitForSeconds(0.6f);

        Call(balloon, "AddLeak", (Vector2)balloon.transform.position + Vector2.right);
        yield return new WaitForSeconds(0.3f);
        bool hasLeak = false;
        foreach (Transform t in balloon.transform) if (t.name.StartsWith("Leak_")) hasLeak = true;
        Assert.IsTrue(hasLeak, "Dòng khí rò");
        Call(balloon, "PatchAll");
        yield return new WaitForSeconds(0.3f);

        var reasonType = balloon.GetType().Assembly.GetType("PopJet.FailReason");
        Call(balloon, "Pop", System.Enum.Parse(reasonType, "Burned"));
        yield return null;
        var fail = GameObject.Find("HUD/SafeArea/FailLabel");
        Assert.IsNotNull(fail, "Chữ lý do thua phải hiện");
        Assert.AreEqual("Burned!", LabelText("HUD/SafeArea/FailLabel"));

        yield return new WaitForSeconds(1.8f); // tự chơi lại sau 1.2 giây
        var balloon2 = Comp("Balloon", "BalloonController");
        Assert.AreNotSame(balloon, balloon2, "Scene phải được load lại");
        Assert.AreEqual(0, Get(balloon2, "PokeCount"));
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsNotNull(GameObject.Find("Level_1-1"), "Chơi lại vẫn ở màn 1-1");
    }

    static object Prop(object o, string name) => o.GetType().GetProperty(name, All).GetValue(o);
    static object Field(object o, string name) => o.GetType().GetField(name, All).GetValue(o);

    [UnityTest]
    public IEnumerator LevelSession_StarsParAndCrown()
    {
        SceneManager.LoadScene(0);
        yield return null;
        yield return null;
        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 7); // 1-8 có Sao, Par 4
        yield return null;
        yield return null;

        var session = Comp("LevelSession", "LevelSession");
        Assert.AreEqual(4, Get(session, "Par"));
        Assert.IsTrue((bool)Get(session, "LevelHasStar"));
        Assert.AreEqual("Pokes 0 / Par 4", LabelText("HUD/SafeArea/PokeLabel"));

        // Đưa bóng chạm Sao thật (trigger), không gọi thẳng CollectStar.
        var star = GameObject.Find("Level_1-8/Star");
        var balloon = Comp("Balloon", "BalloonController");
        balloon.GetComponent<Rigidbody2D>().position = star.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsTrue((bool)Get(session, "StarCollectedThisRun"), "Chạm Sao phải nhặt được");
        Assert.AreEqual("Star!", LabelText("HUD/SafeArea/FailLabel"));

        Call(balloon, "TryVent", Vector2.left);
        yield return null;
        Assert.AreEqual("Pokes 1 / Par 4", LabelText("HUD/SafeArea/PokeLabel"));
        Call(balloon, "ReachGoal");
        yield return null;
        var r = Get(session, "LastResult");
        Assert.AreEqual(3, Prop(r, "Stars"), "Về đích + Sao + dưới Par");
        Assert.IsTrue((bool)Field(r, "crown"));
        Assert.AreEqual("3 Stars + Crown!", LabelText("HUD/SafeArea/FailLabel"));

        // 1-1: Par 1, không có Sao (tự đạt). Chọc 2 lần => mất sao Par, không có Vương miện.
        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 0);
        yield return null;
        yield return null;
        session = Comp("LevelSession", "LevelSession");
        balloon = Comp("Balloon", "BalloonController");
        Assert.IsFalse((bool)Get(session, "LevelHasStar"));
        Call(balloon, "TryVent", Vector2.right);
        Call(balloon, "TryVent", Vector2.left);
        yield return null;
        Assert.AreEqual(new Color(1f, 0.541f, 0f), LabelColor("HUD/SafeArea/PokeLabel"), "Vượt Par thì chữ chuyển cam");
        Call(balloon, "ReachGoal");
        yield return null;
        r = Get(session, "LastResult");
        Assert.AreEqual(2, Prop(r, "Stars"));
        Assert.IsFalse((bool)Field(r, "crown"));

        // Thành tích gộp theo màn: 1-2 vẫn giữ Vương miện.
        var sessionType = session.GetType();
        var rec = sessionType.GetMethod("GetRecord").Invoke(null, new object[] { "1-8" });
        Assert.IsTrue((bool)Field(rec, "crown"));

        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 0);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator LevelLoader_ResultScreenNextAdvances()
    {
        SceneManager.LoadScene(0);
        yield return null;
        yield return null;

        var loader = Comp("LevelLoader", "LevelLoader");
        Call(loader, "LoadLevel", 0);
        yield return null;
        yield return null;
        Assert.IsNotNull(GameObject.Find("Level_1-1"));
        var catalog = Get(loader, "Catalog");
        Assert.AreEqual(20, Prop(catalog, "Count"), "Thế giới 1 có 20 màn");

        // Về đích: hiện màn Kết quả, không tự sang màn kế.
        Call(Comp("Balloon", "BalloonController"), "ReachGoal");
        yield return new WaitForSecondsRealtime(1.2f);
        Assert.AreEqual("Result", Get(Screens, "CurrentScreen"), "Về đích phải hiện màn Kết quả");
        Assert.IsNotNull(GameObject.Find("Level_1-1"), "Không tự sang màn kế");
        Assert.AreEqual(1f, Time.timeScale, "Màn Kết quả không dừng thế giới");
        Call(Screens, "Next");
        yield return null;
        yield return null;
        Assert.IsNotNull(GameObject.Find("Level_1-2"), "Next phải sang màn 1-2");
        Assert.IsNull(GameObject.Find("Level_1-1"));
        var spawn = GameObject.Find("Level_1-2/Spawn");
        Assert.Less(Vector2.Distance(spawn.transform.position, Comp("Balloon", "BalloonController").transform.position), 0.01f);

        // Màn cuối: Next mở Bản đồ thay vì sang màn khác.
        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 19);
        yield return null;
        yield return null;
        Assert.IsNotNull(GameObject.Find("Level_1-20"));
        Call(Comp("Balloon", "BalloonController"), "ReachGoal");
        yield return new WaitForSecondsRealtime(1.2f);
        Call(Screens, "Next");
        yield return null;
        Assert.AreEqual("Map", Get(Screens, "CurrentScreen"));
        Assert.IsNotNull(GameObject.Find("HUD/Screens/Map/Viewport/Content/Grid1/Level_1-1"), "Bản đồ có nút màn 1-1");
        Assert.IsNotNull(GameObject.Find("Level_1-20"));

        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 0); // trả về màn đầu cho test khác
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator PauseLookAndIntro_UseTimeController()
    {
        SceneManager.LoadScene(0);
        yield return null;
        yield return null;
        var screens = Screens;
        Assert.IsNotNull(GameObject.Find("HUD/SafeArea/PauseButton"), "Nút Tạm dừng");
        Assert.IsNotNull(GameObject.Find("HUD/SafeArea/LookButton"), "Nút Xem màn");

        Call(screens, "Pause");
        yield return null;
        Assert.AreEqual(0f, Time.timeScale, "Tạm dừng dừng thế giới");
        Assert.AreEqual("Pause", Get(screens, "CurrentScreen"));
        // Nhả ngắm trong lúc đang tạm dừng không được làm thế giới chạy lại.
        var tc = RuntimeType("TimeController");
        var reasonType = RuntimeType("TimeReason");
        tc.GetMethod("Set").Invoke(null, new object[] { System.Enum.Parse(reasonType, "Aim"), false });
        Assert.AreEqual(0f, Time.timeScale);
        Call(screens, "OpenSettings");
        yield return null;
        Assert.AreEqual(0f, Time.timeScale, "Cài đặt mở từ Tạm dừng vẫn dừng thế giới");
        Call(screens, "Resume");
        yield return null;
        Assert.AreEqual(1f, Time.timeScale);

        var director = Comp("LevelCameraDirector", "LevelCameraDirector");
        Call(director, "SetLook", true);
        Assert.AreEqual(0f, Time.timeScale, "Xem màn dừng thế giới");
        Assert.IsTrue((bool)tc.GetProperty("IsBlockingInput").GetValue(null), "Đang Xem màn thì không được ngắm");
        Call(director, "SetLook", false);
        Assert.AreEqual(1f, Time.timeScale);

        // Camera giới thiệu: dừng thế giới khi vào màn mới, tự hết sau khoảng 2.2 giây.
        RuntimeType("GameFlow").GetField("IntroEnabled").SetValue(null, true);
        RuntimeType("GameFlow").GetMethod("ForgetIntro").Invoke(null, null);
        SceneManager.LoadScene(0);
        yield return null;
        yield return null;
        Assert.AreEqual(0f, Time.timeScale, "Giới thiệu đầu màn dừng thế giới");
        Assert.IsNotNull(GameObject.Find("HUD/SafeArea/Objectives"), "Bảng mục tiêu đầu màn");
        yield return new WaitForSecondsRealtime(2.6f);
        Assert.AreEqual(1f, Time.timeScale, "Hết giới thiệu thì thế giới chạy lại");
        // Chơi lại cùng màn thì bỏ qua giới thiệu.
        Call(Comp("LevelManager", "LevelManager"), "Restart");
        yield return null;
        yield return null;
        Assert.AreEqual(1f, Time.timeScale, "Chơi lại không chạy lại giới thiệu");
    }

    [UnityTest]
    public IEnumerator HomeScreen_PlayContinuesAndSettingsPersist()
    {
        RuntimeType("GameFlow").GetMethod("RequestHome").Invoke(null, null);
        SceneManager.LoadScene(0);
        yield return null;
        yield return null;
        Assert.AreEqual("Home", Get(Screens, "CurrentScreen"));
        Assert.AreEqual(0f, Time.timeScale, "Trang chủ dừng thế giới");

        RuntimeType("GameSettings").GetMethod("SetMusic").Invoke(null, new object[] { false });
        Save("Save");
        Save("UseFile", SavePath);
        var settings = Field(SaveType.GetProperty("Data").GetValue(null), "settings");
        Assert.AreEqual(0f, Field(settings, "musicVolume"), "Cài đặt phải lưu ra file");

        Call(Screens, "PlayFromHome");
        yield return null;
        yield return null;
        Assert.IsNull(Get(Screens, "CurrentScreen"));
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsNotNull(GameObject.Find("Level_1-1"), "Save trống: Chơi vào màn 1-1");
    }

    [UnityTest]
    public IEnumerator World1Objects_CoinsLauncherAndHazards()
    {
        SceneManager.LoadScene(0);
        yield return null;
        yield return null;

        // Xu: chạm vào thì nhặt, về đích thì cộng ví. Chơi lại chỉ được 5 xu (GDD §12.2).
        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 2); // 1-3 có xu
        yield return null;
        yield return null;
        var balloon = Comp("Balloon", "BalloonController");
        var session = Comp("LevelSession", "LevelSession");
        balloon.GetComponent<Rigidbody2D>().position = GameObject.Find("Level_1-3/Coin_0").transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.AreEqual(1, Get(session, "CoinsThisRun"), "Chạm xu phải nhặt được");
        Call(balloon, "ReachGoal");
        yield return null;
        Assert.AreEqual(1, Field(SaveType.GetProperty("Data").GetValue(null), "coins"), "Lần đầu: 1 xu mới");
        Call(Comp("LevelManager", "LevelManager"), "Restart");
        yield return null;
        yield return null;
        Call(Comp("Balloon", "BalloonController"), "ReachGoal");
        yield return null;
        Assert.AreEqual(6, Field(SaveType.GetProperty("Data").GetValue(null), "coins"), "Chơi lại: +5 xu");

        // Pháo giấy: bóng chui vào thì bị giữ, bắn ra không tính cú chọc.
        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 10); // 1-11
        yield return null;
        yield return null;
        balloon = Comp("Balloon", "BalloonController");
        var launcher = Comp("Level_1-11/Launcher", "Launcher");
        balloon.GetComponent<Rigidbody2D>().position = launcher.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsTrue((bool)Get(balloon, "IsHeld"), "Pháo phải giữ bóng");
        Assert.IsFalse((bool)Get(balloon, "CanAim"), "Đang trong pháo thì không ngắm được");
        Call(launcher, "Fire");
        yield return new WaitForFixedUpdate();
        Assert.IsFalse((bool)Get(balloon, "IsHeld"));
        Assert.Greater(((Vector2)Get(balloon, "Velocity")).magnitude, 3f, "Pháo bắn bóng đi");
        Assert.AreEqual(0, Get(balloon, "PokeCount"), "Bắn pháo không tính cú chọc");

        // Nến (nhóm D): chạm lửa là nổ ngay, lý do Burned.
        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 14); // 1-15
        yield return null;
        yield return null;
        balloon = Comp("Balloon", "BalloonController");
        balloon.GetComponent<Rigidbody2D>().position = GameObject.Find("Level_1-15/Candle/Flame").transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.AreEqual("Burned", Get(balloon, "LastFailReason").ToString());
        yield return new WaitForSeconds(1.6f); // để tự chơi lại xong

        // Màn trùm: máy hút bụi đứng yên tới cú chọc đầu.
        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 19); // 1-20
        yield return null;
        yield return null;
        var vacuum = GameObject.Find("Level_1-20/Vacuum").transform;
        float y0 = vacuum.position.y;
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(y0, vacuum.position.y, 1e-3f, "Mở màn an toàn: máy hút chưa chạy");
        Call(Comp("Balloon", "BalloonController"), "TryVent", Vector2.up);
        yield return new WaitForSeconds(0.5f);
        Assert.Greater(vacuum.position.y, y0 + 0.1f, "Chọc rồi thì máy hút đuổi theo");

        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 0);
        yield return null;
        yield return null;
    }
}
