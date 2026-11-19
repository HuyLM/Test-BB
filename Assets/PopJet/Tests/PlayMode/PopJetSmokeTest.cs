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
        Assert.AreEqual("Burned!", fail.GetComponent<UnityEngine.UI.Text>().text);

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
        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 1); // 1-2 có Sao, Par 4
        yield return null;
        yield return null;

        var session = Comp("LevelSession", "LevelSession");
        Assert.AreEqual(4, Get(session, "Par"));
        Assert.IsTrue((bool)Get(session, "LevelHasStar"));
        var pokeLabel = GameObject.Find("HUD/SafeArea/PokeLabel").GetComponent<UnityEngine.UI.Text>();
        Assert.AreEqual("Pokes 0 / Par 4", pokeLabel.text);

        // Đưa bóng chạm Sao thật (trigger), không gọi thẳng CollectStar.
        var star = GameObject.Find("Level_1-2/Star");
        var balloon = Comp("Balloon", "BalloonController");
        balloon.GetComponent<Rigidbody2D>().position = star.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsTrue((bool)Get(session, "StarCollectedThisRun"), "Chạm Sao phải nhặt được");
        Assert.AreEqual("Star!", GameObject.Find("HUD/SafeArea/FailLabel").GetComponent<UnityEngine.UI.Text>().text);

        Call(balloon, "TryVent", Vector2.left);
        yield return null;
        Assert.AreEqual("Pokes 1 / Par 4", pokeLabel.text);
        Call(balloon, "ReachGoal");
        yield return null;
        var r = Get(session, "LastResult");
        Assert.AreEqual(3, Prop(r, "Stars"), "Về đích + Sao + dưới Par");
        Assert.IsTrue((bool)Field(r, "crown"));
        Assert.AreEqual("3 Stars + Crown!", GameObject.Find("HUD/SafeArea/FailLabel").GetComponent<UnityEngine.UI.Text>().text);

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
        Assert.AreEqual(new Color(1f, 0.541f, 0f), GameObject.Find("HUD/SafeArea/PokeLabel").GetComponent<UnityEngine.UI.Text>().color, "Vượt Par thì chữ chuyển cam");
        Call(balloon, "ReachGoal");
        yield return null;
        r = Get(session, "LastResult");
        Assert.AreEqual(2, Prop(r, "Stars"));
        Assert.IsFalse((bool)Field(r, "crown"));

        // Thành tích gộp theo màn: 1-2 vẫn giữ Vương miện.
        var sessionType = session.GetType();
        var rec = sessionType.GetMethod("GetRecord").Invoke(null, new object[] { "1-2" });
        Assert.IsTrue((bool)Field(rec, "crown"));

        yield return new WaitForSeconds(1.6f); // 1-1 thắng thì tự sang 1-2, để yên cho xong rồi trả về 1-1
        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 0);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator LevelLoader_LoadsNextLevelAndWinAdvances()
    {
        SceneManager.LoadScene(0);
        yield return null;
        yield return null;

        var loader = Comp("LevelLoader", "LevelLoader");
        Call(loader, "LoadLevel", 0);
        yield return null;
        yield return null;
        Assert.IsNotNull(GameObject.Find("Level_1-1"));

        // Về đích ở 1-1 thì tự sang 1-2 sau winDelay.
        var balloon = Comp("Balloon", "BalloonController");
        Call(balloon, "ReachGoal");
        yield return new WaitForSeconds(1.6f);
        Assert.IsNotNull(GameObject.Find("Level_1-2"), "Thắng phải sang màn 1-2");
        Assert.IsNull(GameObject.Find("Level_1-1"));
        var spawn = GameObject.Find("Level_1-2/Spawn");
        Assert.Less(Vector2.Distance(spawn.transform.position, Comp("Balloon", "BalloonController").transform.position), 0.01f);

        // Màn cuối: thắng thì không đi đâu cả.
        Call(Comp("Balloon", "BalloonController"), "ReachGoal");
        yield return new WaitForSeconds(1.6f);
        Assert.IsNotNull(GameObject.Find("Level_1-2"));

        Call(Comp("LevelLoader", "LevelLoader"), "LoadLevel", 0); // trả về màn đầu cho test khác
        yield return null;
        yield return null;
    }
}
