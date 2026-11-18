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

        Assert.IsTrue((bool)Call(balloon, "TryVent", Vector2.up));
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
    }
}
