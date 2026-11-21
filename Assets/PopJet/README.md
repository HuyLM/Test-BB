# Pop Jet - hướng dẫn setup (Unity 2D)

Chạy trên Unity 6 (dùng `linearVelocity`/`linearDamping`, có nhánh `#if` cho bản cũ) và hỗ trợ cả Input System mới lẫn cũ.
Thiết kế game đầy đủ: [Docs/GDD.md](Docs/GDD.md). Giai đoạn 1 của GDD (§16.1) đã có trong code.

## 1. Cài file

Copy cả thư mục `PopJet` vào `Assets/` của project. Không cần package nào thêm ngoài Cinemachine 3, Input System và uGUI (đã có trong project).

## 2. Danh sách script

| Script | Gắn vào | Việc chính |
|---|---|---|
| `BalloonController` | Bóng | Khí, lực đẩy, lỗ chọc, lỗ rò, trạng thái, đếm cú chọc, `Pop(lý do)` |
| `BalloonInput` | Object rỗng `Input` | Giữ - kéo - thả, kim, bỏ qua chạm trên UI, sự kiện `AimStarted`/`AimEnded` |
| `BalloonFeedback` | Bóng | Âm thanh + particle theo sự kiện |
| `Spike` | Gai | Chạm bóng thì tạo thêm lỗ rò |
| `BalloonBounce` | Bóng | Bóp dẹt + rung mềm và tiếng "bộp" khi va tường, sự kiện `Bounced` (độ nảy thật do `PhysicsMaterial2D`) |
| `PatchPickup` | Keo vá | Vá hết lỗ rò, không hồi khí; chỉ dùng khi bóng đang rò |
| `Fan` | Quạt | Đẩy bóng theo trục +X local |
| `ButtonSwitch` | Nút | Mở cửa gán sẵn |
| `Door` | Cửa | Trượt mở/đóng |
| `GoalZone` | Đích | Hoàn thành màn |
| `LevelManager` | Object rỗng | Thất bại mềm (tự chơi lại hoặc chạm để chơi lại ngay), thắng, chuyển màn, phát hiện kẹt |
| `PopJetDebug` | Object rỗng | Overlay state/khí/lỗ rò ở góc dưới trái để tune; chỉ vẽ trong Editor và Development Build |
| **Giai đoạn 1** | | |
| `MobileBootstrap` | (không gắn, tự chạy) | 60 fps, khóa màn dọc, không cho màn hình tự tắt |
| `CameraAspectFit` | `CM FollowCam` | Giữ chiều ngang nhìn thấy cố định trên mọi tỉ lệ màn hình |
| `TrajectoryPreview` | `Input` | Chấm quỹ đạo dự đoán khi ngắm, dừng ở va chạm đầu tiên (bóng mờ) |
| `AirRing` | `Input` | Vòng khí quanh bóng khi ngắm, đoạn nhấp nháy = khí cú này sẽ tiêu |
| `BalloonFace` | Bóng | Mắt, miệng, highlight, biểu cảm (dựng lúc Play) |
| `BalloonString` | Bóng | Dây bóng mềm (verlet), rơi xuống khi bóng vỡ |
| `LeakStreamFX` | Bóng | Vết lỗ, dòng khí phun liên tục và tiếng xì cho từng lỗ rò; băng keo khi vá |
| `Haptics` + `BalloonHaptics` | (static) + Bóng | Rung máy Android/iOS theo sự kiện của bóng |
| `TimeFreezeAudio` | `Music` | Nhạc bị lọc low-pass khi thế giới dừng, tiếng tick/vút |
| `PopJetHud` + `AirBarUI` + `SafeAreaFitter` | `HUD` | Nút Retry, số cú chọc, thanh khí chia ô, lý do thua, "Stuck?" |
| `GameText` | (static) | Mọi chữ trong game (tiếng Anh) gom một chỗ |
| `Plugins/iOS/PopJetHaptics.mm` | (plugin) | Rung trên iOS, chỉ build cho iOS |
| **Giai đoạn 2** | | |
| `LevelDefinition` | Asset (`Levels/Level_*.asset`) | Một màn: mã, tên, thế giới, prefab, Par, dòng gợi ý, cờ tutorial (`TutorialCue`), ghi chú, lời giải đã ghi (`LevelSolution`) |
| `LevelCatalog` | Asset (`Levels/LevelCatalog.asset`) | Danh sách màn theo thứ tự chơi |
| `LevelRoot` | Gốc prefab màn | Điểm xuất phát của bóng (`Spawn`) |
| `LevelLoader` | Object rỗng `LevelLoader` | Tạo prefab màn hiện tại khi scene load, đặt bóng và camera vào điểm xuất phát, `LoadLevel`/`LoadNext` |
| `LevelSession` | Object rỗng `LevelSession` | Một lượt chơi: cú chọc so với Par, nhặt Sao, tính 3 sao + Vương miện khi về đích, gộp thành tích theo màn |
| `StarPickup` | Sao trong prefab màn | Bóng chạm là nhặt (trigger), báo `LevelSession` |
| `TimeController` | (static) | Nơi duy nhất ghi `Time.timeScale`: gom lý do dừng (Aim, Pause, Look, Intro, Menu, Hint); load scene thì xóa hết |
| `GameScreens` | Canvas `HUD` | Dựng lúc chạy (UiKit, TextMeshPro): nút Tạm dừng, ô Sao, nút Xem màn, nút Gợi ý, gợi ý tutorial, bảng mục tiêu đầu màn; màn Tạm dừng (có "Restart Level" khi đang giữ cột mốc), Kết quả, Trang chủ, Bản đồ, Cài đặt |
| `LevelSolution` + `SolutionRecorder` | Data trong `LevelDefinition` + object rỗng `SolutionRecorder` | Ghi lời giải của designer (vị trí bóng mỗi 0.05 giây, thao tác kèm số bước vật lý), lưu vào màn và đặt Par; phát lại thao tác đúng từng bước vật lý cho test và menu Replay |
| `HintGhost` | Object rỗng `HintGhost` | Bóng ma Gợi ý: kim mờ kéo ra rồi đâm, bay theo đường đã ghi tới thao tác kế tiếp (chỉ phát lại vị trí, không mô phỏng vật lý) |
| `TutorialCoach` | Object rỗng `TutorialCoach` | Gợi ý bằng hình theo `TutorialCue`: bàn tay 1-1, ô Sao nhấp nháy 1-6, Par sáng 1-9; nhắc "Release in the middle to cancel" tới khi hủy được một lần |
| `WindLeaves` | Quạt (builder tự gắn) | Lá giấy trôi theo gió, cho thấy vùng và hướng gió |
| `SwitchWire` | Dây trong prefab màn (1-8) | Chấm sáng chạy từ nút tới cửa; bấm nút thì dây sáng rồi mờ |
| `Checkpoint` + `CheckpointState` | Cột mốc trong prefab màn (1-20) | Chạm thì lưu vị trí, khí, số cú chọc (Sao, xu do `LevelSession` lưu thêm); thua hoặc Retry thì chơi lại từ đây; đẩy vật đuổi theo (`PathMover`) tới `chaserProgress` |
| `LevelCameraDirector` | Object rỗng `LevelCameraDirector` | Camera toàn cảnh (CM LookCam tạo lúc chạy) cho Xem màn và camera giới thiệu đầu màn |
| `GameFlow`, `GameSettings` | (static) | Luồng màn hình qua các lần load scene (Trang chủ, giới thiệu); áp cài đặt âm thanh |
| `CoinPickup` | Xu trong prefab màn | Chỉ lần nhặt đầu tính tiền; xu cộng vào ví khi về đích, chơi lại màn đã qua chỉ được 5 xu |
| `PathMover` | Gai di chuyển, máy hút bụi | Kinematic, qua lại hoặc một chiều; tùy chọn chỉ chạy sau cú chọc đầu |
| `Launcher` | Pháo giấy | Giữ bóng (`BalloonController.Hold`), nòng xoay, chạm để bắn (`Launch`), không tính cú chọc |
| `HardHazard` | Lửa nến, miệng máy hút | Chạm là `Pop(reason)` |
| `Cat` | Mèo ngủ | Bóng vào bán kính 1.8 thì dậy, 0.6 giây sau vồ. Luật chạy trong FixedUpdate, hình trong Update |
| `ForceField` | Vùng hút máy hút bụi | Hút về tâm, càng gần càng mạnh; chưa chọc lần đầu thì không hút |
| `PumpStation` | Trạm bơm gắn tường (1-19, 1-20) | Bóng trong vùng thì hồi khí dần (`AddAir`), mỗi trạm hồi tối đa `capacity`; kéo nhẹ bóng về tâm khi không vent; đồng hồ trên trạm vơi theo khí còn lại. Builder: `PumpAt(level, y, onLeftWall)` |
| `SaveSystem` | (static, tự chạy) | Lưu JSON: thành tích từng màn, màn gần nhất, xu, cài đặt, tiến độ tutorial; tự lưu khi qua màn, đổi màn, app xuống nền |

## 2b. Màn chơi (Giai đoạn 2, phần 2.1)

- **Một scene gameplay** (`PopJet_Sample.unity`) chứa bóng, input, camera, HUD, nhạc, LevelManager và `LevelLoader`. **Nội dung màn** (tường, gai, quạt, keo, cửa, đích) là prefab trong `Levels/Prefabs/`, gốc có `LevelRoot` và object con `Spawn`.
- Chơi lại và sang màn mới đều load lại scene gameplay; chỉ số màn giữ trong biến static của `LevelLoader`. Cách này reset sạch bóng, HUD, vật cản (khác GDD §15.7 định tạo lại prefab; đổi sau nếu load scene chậm trên máy thật).
- Về đích thì hiện màn **Kết quả** (3 sao phồng lần lượt, cú chọc / Par, xu, Vương miện; nút Next / Retry / Map). Màn cuối: Next mở Bản đồ.
- **Thêm màn mới:** dựng prefab có `LevelRoot` + `Spawn` (đừng đặt bóng trong prefab), tạo `Assets > Create > PopJet > Level Definition`, gán prefab và Par, kéo vào `LevelCatalog`. Test một màn: đặt `Start Level Override` trên `LevelLoader` thành chỉ số của màn đó.
- **3 sao (phần 2.2, GDD §3.4):** ★ Về đích, ★ Nhặt Sao, ★ Số cú chọc ≤ Par. Màn không đặt `StarPickup` thì sao thứ hai tự đạt. Đủ 3 sao trong **cùng một lượt** thì có Vương miện. Thành tích gộp từng sao qua các lượt (`LevelSession.GetRecord(id)`), hiện chỉ giữ trong bộ nhớ, phần 2.3 sẽ lưu ra file. HUD hiện "Pokes 1 / Par 4" (vượt Par thì chuyển cam), "Star!" khi nhặt Sao và số sao khi về đích. Mỗi màn nên có đúng 1 Sao, đặt lệch khỏi đường ngắn nhất.
- **Lưu game (phần 2.3):** file `popjet_save.json` trong `Application.persistentDataPath` (menu `Tools > PopJet > Show Save File` mở thư mục, `Delete Save File` xóa để chơi lại từ đầu). Lưu: thành tích từng màn (từng sao, Vương miện, ít cú chọc nhất), màn chơi gần nhất (mở game là chơi tiếp màn đó), xu, cài đặt (rung, âm lượng nhạc/hiệu ứng, giảm chuyển động). Ghi qua file tạm rồi mới thay file chính, bản trước giữ ở `.bak`; file hỏng thì đọc `.bak`, hỏng nốt thì bắt đầu lại trống. Màn sau mở khi màn trước đã về đích (`LevelCatalog.IsUnlocked`); bản đồ ở phần 2.5 sẽ dùng. Cần lưu thêm gì thì thêm vào `SaveData`, đừng dùng PlayerPrefs riêng.
- **20 màn Thế giới 1 (phần 2.7)** dựng trong `Editor/PopJetLevelBuilder.cs` theo GDD §8.2/§8.4: 1-1..1-10 tutorial (bố cục scene mẫu cũ giờ là 1-8), 1-11 pháo giấy, 1-12 gối bông, 1-15 nến (nhóm D đầu tiên), 1-18 mèo, 1-20 màn trùm máy hút bụi (cao 22, có cột mốc). **Bố cục và Par chưa playtest**, ghi chú lời giải nằm trong `notes` của từng `LevelDefinition`. Builder **ghi đè** 20 prefab `Level_1-*` mỗi lần chạy; màn dựng tay nên đặt tên khác.
- **Ghi lời giải (GDD §8.3 điều 5, §15.7):** chọn `Tools > PopJet > Solutions > Record Best (fewest pokes)`, bấm Play, chơi tới đích (góc trên hiện chữ đỏ `REC`). Về đích thì lời giải lưu vào `LevelDefinition` của màn và **Par = số cú chọc** của lời giải. `Record Best` chỉ lưu khi chưa có hoặc ít cú chọc hơn bản cũ; `Record Always` luôn ghi đè (dùng sau khi sửa bố cục làm lời giải cũ hỏng). Lượt chơi lại từ cột mốc không được lưu. `Report` liệt kê màn nào đã có lời giải; `Replay Current Level` (đang Play) chơi lại màn bằng lời giải đã ghi để xem bằng mắt; `Clear Selected Level Solution` xóa lời giải của LevelDefinition đang chọn. Builder **giữ Par** của màn đã có lời giải, không ghi đè bằng số nháp.
- **Phát lại y hệt:** lời giải lưu số bước vật lý của từng cú chọc và cú bắn pháo; phát lại đúng bước đó thì vật lý ra giống từng bit (test `Solutions_ReplayMatchesRecordedRunExactly`). Muốn giữ được điều này: mọi luật tác động lên bóng chạy trong `FixedUpdate` hoặc callback vật lý, đo thời gian bằng biến cộng trong `FixedUpdate` (không dùng `Time.time`), không di chuyển collider trong `Update` (hình nhún, xoay đặt ở object con như xu và Sao).
- **Gợi ý (GDD §3.5, §7.2):** thua cùng một màn 3 lần thì hiện nút **Hint** (dưới phải) kèm dòng "Need a hint?". Bấm thì thế giới dừng, camera theo bóng ma diễn **thao tác kế tiếp** của lời giải (tính từ điểm gần bóng nhất trên đường đã ghi), chạm để xem xong sớm. Màn chưa có lời giải thì không có nút. Chưa tính giá (xu, quảng cáo là việc Giai đoạn 3).
- **Tutorial bằng hình (GDD §8.4):** 1-1 bàn tay mờ trên bóng, khi giữ thì bàn tay ma kéo xuống; 1-6 ô Sao nhấp nháy tới khi nhặt; 1-7 (và mọi quạt) lá giấy trôi theo gió; 1-8 dây chấm từ nút tới cửa; 1-9 phần Par trên HUD vàng và nhún, dòng "Par 3: finish in 3 pokes or fewer". Mọi màn: kéo kim ra rồi đưa ngón tay về giữa bóng thì hiện "Release in the middle to cancel", hủy được một lần thì thôi (lưu trong `SaveData.tutorial`). Cờ đặt trong `LevelDefinition.tutorial` (builder: tham số `cues` của `L(...)`).
- **Hoàn thiện (task 4–6, 2026-11-21):** mũi tên chỉ đích khi đích ngoài màn hình (`GoalIndicator`); Reduce Motion tắt rung camera, chớp sáng khi nổ và hiệu ứng chuyển màn (thay bằng mờ dần); chuyển màn bằng bóng phình rồi nổ confetti (`ScreenTransition`, bỏ qua khi chơi lại; smoke test tắt qua `ScreenTransition.Enabled`); popup "xì hơi" khi đóng; Trang chủ có bóng chọc thử (`HomeBalloon`); âm thanh §11.2 (`GameAudio`: nút UI, chuông Sao, 3 nốt Đô–Mi–Son ở Kết quả, "cót két" khi khí dưới 1 cú chọc, ép nhạc khi nổ; vá có 3 biến thể; pitch cú chọc tăng khi khí cạn). Font Baloo 2/Nunito chưa đổi.
- **Cột mốc (GDD §6 #9):** chạm thì lưu vị trí cột mốc, khí, số cú chọc, Sao và xu đã nhặt; thua, chạm để chơi lại, hoặc bấm Retry thì chơi lại từ cột mốc, bóng chờ cú chọc đầu như mở màn. Tạm dừng có thêm **Restart Level** để chơi lại từ đầu màn. Đổi màn, về Trang chủ, về đích thì bỏ cột mốc. Lượt chơi lại từ cột mốc không được Vương miện. 1-20: cột mốc trên đường pháo giấy bắn lên tầng 2, chơi lại từ đó thì máy hút bắt đầu ở y = 1 (khoảng 10 giây sau cú chọc đầu mới tới). Builder: `CheckpointAt(level, pos, chaser, chaserProgress)`.
- **HUD và màn hình (phần 2.4, 2.5):** chữ dùng TextMeshPro (builder tự import TMP Essentials; lần đầu nếu báo chưa thấy font thì chạy lại Build Sample Scene). Lần đầu mở game vào thẳng 1-1; Trang chủ chỉ hiện khi mở app sau khi đã qua 1-3. Vào màn mới: camera toàn cảnh + bảng 3 mục tiêu trong khoảng 2 giây (chạm để bỏ qua), chơi lại thì bỏ qua. Mọi dừng thời gian đi qua `TimeController`: nhả ngắm trong lúc Tạm dừng không làm thế giới chạy lại.

## 3. Dựng scene mẫu

**Cách nhanh:** menu `Tools > PopJet > Build Sample Scene` tự dựng toàn bộ scene vào `Assets/PopJet/Scenes/PopJet_Sample.unity` (kèm sprite, particle, âm thanh, nhạc placeholder, HUD và mọi tính năng Giai đoạn 1). Chạy headless khi Editor đã đóng:

```
Unity.exe -batchmode -quit -projectPath "E:/Unity/my/Test-BB" -executeMethod PopJet.EditorTools.PopJetSceneBuilder.BuildFromCommandLine -logFile -
```

**Menu khác:**

| Menu | Việc |
|---|---|
| `Tools > PopJet > Add Phase 1 Features To Open Scene` | Thêm tính năng Giai đoạn 1 vào scene đang mở, không dựng lại (giữ chỉnh tay). Chạy lại không bị nhân đôi |
| `Tools > PopJet > Add Balloon Pop To Open Scene` | Chỉ thêm hiệu ứng vỡ bóng vào scene đang mở |
| `Tools > PopJet > Apply Mobile Player Settings` | Player Settings: Default Orientation = Portrait, tắt xoay ngang |

**Lưu ý khi chạy lại builder:** nó ghi đè `PopJet_Sample.unity`, các prefab trong `Prefabs/` và cài đặt import của sprite sinh tự động (`Circle`, `Square`, `Triangle`, `SoftDot`, `Ring`, `Smile`, `RoundRect`, `Star`). Art thật (kể cả art AI theo GDD §9.6) đặt tên khác, trong thư mục con như `Art/World1/`.

**Art AI Thế giới 1 (`Art/World1/`, builder không ghi đè):** ảnh gốc ở `ArtSource/AI/raw/`. Builder dùng ảnh có sẵn, thiếu file thì giữ placeholder hình học; collider luôn giữ kích thước gameplay.

| Phần | Cách gắn |
|---|---|
| Thân bóng `Balloon_Body` | Thay sprite của `Balloon/Visual`, pivot = tâm hình tròn (đường kính 421 px = 1 đơn vị). Ảnh đã chuẩn hoá về gần trắng để tô màu bóng |
| Mặt bóng `Faces/Face_00…11` | Cắt từ tấm 4×3, chỉ giữ nét mực. `BalloonFace.faceSprites` (đủ 12 ảnh thì thay mặt dựng bằng hình tròn) |
| Kim, nút, cửa | `AddArtWorld` / `DoorArt` (cửa 9-slice, xoay nằm ngang) |
| Tường, kệ | `WallArt`: lát dải bìa `Wall_Tile` dọc cạnh dài (Tiled) |
| Mèo | `Cat.sleepSprite` / `awakeSprite` đổi theo trạng thái |
| Nền lớp giữa, vân giấy | `Room_Mid` trên mặt sàn; `HUD/PaperGrain` (RawImage lặp, dưới mọi UI) |
| Icon HUD `Icons/Icon_*` | Nút Tạm dừng / Xem màn thành nút tròn; Retry / Map / Settings có icon bên trái |

Viền mực `#2B2B3A` (§9.6 điều 3) dùng shader `Shaders/SpriteOutline.shader` (material `Art/SpriteOutline.mat`), dày 3 px ở màn rộng 1080 tính theo pixel màn hình; gắn cho bóng và vật tương tác, không gắn cho nền, tường, mặt. Sprite art luôn import Full Rect để viền không bị cắt. Atlas `Art/World1.spriteatlasv2` (padding 16, tối đa 2048) gom art trừ ảnh lát và nền xa.

**Font:** `Fonts/Baloo2-ExtraBold.ttf` (tiêu đề, chữ từ cỡ 80) và `Fonts/Nunito-ExtraBold.ttf` (còn lại), giấy phép OFL (file `OFL-*.txt` kèm theo). Builder tạo font asset TMP động `Nunito SDF` / `Baloo2 SDF` một lần; xoá file `.asset` để tạo lại.

**Camera (Cinemachine 3, không tự code follow):** Main Camera Orthographic + `CinemachineBrain`, màn dọc 9:16. Object `CM FollowCam` có `CinemachineCamera` (Follow = `Balloon`, Ortho Size 3.5 ở tỉ lệ 9:16) + `CinemachinePositionComposer` (Damping 0.4, Dead Zone 10%, Lookahead 0.25 giây) + `CameraAspectFit`. **Không** dùng `CinemachineConfiner2D`: chặn camera ở mép màn thì bóng sát góc sẽ nằm sát mép màn hình, không còn chỗ kéo kim. Bật `Interpolate` cho `Rigidbody2D` của bóng để camera không giật.

**Bóng (`Balloon`)**
1. Tạo Sprite hình tròn (`Create > 2D > Sprites > Circle`), đặt tên `Balloon`.
2. Add `Rigidbody2D`, `CircleCollider2D`, `BalloonController`. Script tự đặt gravity = 0, damping, khóa xoay.
3. (Tùy chọn) Add `AudioSource` + `BalloonFeedback`, kéo clip và particle vào.
4. Nảy mềm: gán `PhysicsMaterial2D` (Bounciness 0.85, Friction 0.05) vào `CircleCollider2D`. Đưa `SpriteRenderer` xuống object con `Visual`, Add `BalloonBounce` vào bóng rồi kéo `Visual` vào ô `Visual` (không bóp scale gốc vì scale gốc thể hiện lượng khí).
5. Mặt bóng, dây, dòng khí rò, rung: dùng menu `Add Phase 1 Features To Open Scene`. Mặt bóng được dựng lúc Play nên không thấy trong Scene view khi chưa chạy.

**Input**
1. Tạo object rỗng `Input`, Add `BalloonInput`, kéo `Balloon` vào.
2. Tạo Sprite `Needle` (hình tam giác nhỏ, **đầu nhọn hướng lên**), kéo vào ô `Needle`.
3. Ô `Flight Arrow` (mũi tên thẳng) không còn cần: `TrajectoryPreview` thay thế. Menu Giai đoạn 1 tự tắt mũi tên cũ.

**HUD:** Canvas Screen Space Overlay, Canvas Scaler 1080×1920 (Match = 0, theo chiều ngang), mọi thứ nằm trong `SafeArea`. Cần `EventSystem` với `InputSystemUIInputModule` (project chỉ bật Input System mới, `StandaloneInputModule` sẽ báo lỗi). Chữ đang dùng UI Text thường với font có sẵn; Giai đoạn 2 đổi sang TextMeshPro.

**Vật tương tác** (mọi vật cần Collider2D; các vật là trigger script tự bật `isTrigger`)

| Vật | Sprite + Collider | Ghi chú |
|---|---|---|
| Gai | Sprite + `PolygonCollider2D` hoặc `BoxCollider2D` (không trigger = chặn bóng, trigger = bóng xuyên qua) + `Spike` | Nên thử cả hai xem cái nào hợp cảm giác |
| Keo vá | Sprite + `CircleCollider2D` + `PatchPickup` | |
| Quạt | Sprite + `BoxCollider2D` (kích thước = vùng gió) + `Fan` | Xoay quanh trục Z: mũi tên xanh (+X) là hướng gió |
| Nút | Sprite + `Collider2D` + `ButtonSwitch` | Kéo `Door` vào mảng `Doors` |
| Cửa | Sprite + `BoxCollider2D` (không trigger) + `Door` | Chỉnh `Open Offset` |
| Đích | Sprite + `Collider2D` + `GoalZone` | |

**LevelManager:** tạo object rỗng, Add `LevelManager`, kéo `Balloon` vào. Thêm scene vào Build Settings để `Restart` hoạt động.

## 4. Giá trị khởi điểm cần playtest

Tất cả đều là starting value, không có nguồn chuẩn.

| Thông số | Giá trị | Ở đâu | Nếu sai |
|---|---|---|---|
| Lực chọc / thời gian | 4 / 2.25 giây (cũ 6 / 1.5, đổi 2026-11-19: vent lâu hơn, cùng xung lực) | `BalloonController` | Bóng bay không đủ xa: tăng lực trước, không tăng thời gian. Đổi thời gian thì chỉnh lực để giữ lực × thời gian ≈ 9 |
| Trạm bơm: tốc độ / tổng mỗi trạm / lực giữ | 0.25 khí/giây / 0.5 / 4 | `PumpStation` | Bóng trôi khỏi vùng trước khi bơm xong: tăng lực giữ. Màn quá dễ: giảm tổng mỗi trạm |
| Khí mất mỗi lần chọc | 0.12 (~8 cú chọc; cũ 0.18, giảm 2026-11-19 cho dễ hơn) | `BalloonController` | Vẫn hết khí sớm: giảm tiếp xuống 0.10. Quá dễ, Par vô nghĩa: tăng về 0.15 |
| Lực khi khí gần cạn | 0.75 (cũ 0.6) | `BalloonController.minThrustWhenLow` | Cú chọc cuối vẫn quá yếu: tăng lên 0.85 |
| Lỗ rò: lực / thời gian / khí | 1.8 / 3 giây / 0.04 (cũ 0.06) | `BalloonController` | Bóng "tự bay lung tung": giảm lực rò trước. Lỗ rò tiết kiệm khí hơn cú chọc ~1.8 lần; GDD giữ làm kỹ năng "rò chiến thuật" |
| Cooldown giữa 2 lỗ rò | 0.5 giây | `BalloonController` | Trượt dọc gai vẫn bị kẹt: tăng cooldown |
| Số lỗ rò tối đa | 4 | `BalloonController` | |
| Vùng hủy | 40% bán kính bóng | `BalloonInput` | Người mới hủy nhầm: thu nhỏ |
| Lực quạt | 4 | `Fan` | Quạt quá yếu/mạnh so với lực chọc: chỉnh theo tỉ lệ với lực chọc |
| Dừng thế giới khi giữ ngắm | Bật | `BalloonInput` → `Freeze World While Aiming` | Đang giữ chọn hướng kim thì `Time.timeScale = 0` (vật lý, quạt, cửa, particle, camera đều dừng). `LevelManager.PlayTime` dùng unscaled time nên vẫn đếm. Đồng hồ bắt đầu từ lần chọc đầu, dừng khi thắng/thua |
| Hết khí = vỡ tung | Bật | `BalloonController` → `Pop When Empty` | Bóng đứng yên, ẩn đi, `BalloonFeedback` bắn prefab `PopBurst`, phát `Pop Clips`, gọi `On Popped` → rung camera. Tắt thì quay về kiểu xẹp rơi với `Empty Gravity Scale`. `Pop(lý do)` luôn vỡ tung |
| Rung camera khi vỡ | biên độ 0.18, 0.35 giây | `Balloon` → `CinemachineImpulseSource` | Rung chóng mặt: giảm biên độ. Không cảm thấy nổ: tăng lên 0.3 |
| Trọng lực khi đang chơi | 0.05 (rơi ~0.33 đơn vị/giây) | `BalloonController` → `Play Gravity Scale` | Rơi nhanh quá/khó giữ bóng: giảm; không thấy áp lực: tăng 0.02 mỗi lần |
| Độ nảy / ma sát bóng | 0.85 / 0.05 | `BalloonBouncy.physicsMaterial2D` | Bóng "bật như cao su": giảm bounciness. Bóng dính tường: giảm friction |
| Bóp dẹt tối đa / tốc độ dẹt hết cỡ | 0.35 / 2.5 | `BalloonBounce` | Không thấy mềm: tăng `Max Squash`. Rung quá lâu: tăng `Spring Damping` |
| Camera: damping / dead zone / ortho size | 0.4 / 10% / 3.5 ở 9:16 | `CM FollowCam`, `CameraAspectFit` | Camera trễ quá: giảm damping. Không thấy đường bay: tăng `Reference Ortho Size` |
| **Giai đoạn 1** | | | |
| Độ dài quỹ đạo dự đoán | 0.6 giây hoặc tới va chạm đầu | `TrajectoryPreview` → `Preview Seconds` | Người mới đoán sai hướng/quãng bay: 1.0. Quá dễ (★Par > 60%): 0.4 |
| Vòng khí: 1 ô = 1 cú chọc | `Vent Air Cost` | `AirRing`, `AirBarUI` | Người chơi không trả lời được "còn mấy cú": thêm số đếm |
| Tự chơi lại sau khi thua | 1.2 giây, chạm để bỏ qua sau 0.25 giây | `LevelManager` | Trung vị nổ → chạm lại > 3 giây: giảm còn 0.8 |
| Phát hiện kẹt | khí < 1 cú, tốc độ < 0.05, trong 3 giây | `LevelManager` | Người chơi phải chờ lâu khi kẹt: giảm còn 2 giây |
| Chữ lý do thua | hiện 1.2 giây, cao hơn chỗ nổ 150 | `PopJetHud` | |
| Nhạc khi ngắm | low-pass 800 Hz, −4 dB, vào 0.15 / ra 0.2 giây | `TimeFreezeAudio` | Người chơi không nhận ra "thế giới dừng": 500 Hz |
| Rung khi nảy | từ độ mạnh 0.35, cách nhau ≥ 0.08 giây | `BalloonHaptics` | Rung quá nhiều khi lăn dọc tường: tăng ngưỡng |
| FPS | 60 | `MobileBootstrap.TargetFps` | Máy yếu nóng/tụt pin: thử 30 cho menu |

**Tuning vật lý:** `Mass` = 1 và damping 1.5 cho vận tốc tối đa khoảng 4 đơn vị/giây khi xì đủ lực. Nếu bóng nặng hoặc nhẹ quá, chỉnh `Damping` và lực chọc cùng nhau. `TrajectoryPreview` tích phân cùng công thức nên tự khớp khi đổi các số này.

## 5. Playtest nhanh (smoke test)

| Test | Cách thử | Đạt khi |
|---|---|---|
| Người mới | Đưa máy cho người chưa biết luật, không nói gì | Đoán đúng hướng bay ≥ 8/10 lần trong 2 phút |
| Stress | Chạm liên tục, kéo ra ngoài màn, thả giữa chừng, chạm lên nút Retry | Không lỗi, không kẹt state Aiming, chạm lên nút không bắt đầu ngắm |
| Gai | Cho bóng trượt dọc thanh gai | Không sinh quá 4 lỗ cùng lúc, bóng không bị kẹt, mỗi lỗ có dòng khí và tiếng xì |
| Hết khí | Chọc đến khi hết | Bóng nổ, hiện "Out of air!", tự chơi lại sau 1.2 giây (chạm để chơi lại ngay) |
| Vá | Bị gai rồi chạm keo | Lỗ rò dừng ngay, dòng khí tắt, miếng băng keo chéo xuất hiện |
| Quỹ đạo | Giữ trên bóng, kéo quanh | Chấm quỹ đạo dừng ở tường, cú cuối thì chấm và vòng khí chuyển cam |
| Kẹt | Còn < 1 cú chọc, để bóng nằm yên ở góc | Sau 3 giây: "Stuck?" và nút Retry nhấp nháy |
| Màn hình | Game view 9:16, 9:20 và 3:4 | Chiều ngang thấy như nhau ở 9:16 và 9:20; HUD nằm trong safe area |

**Test tự động:** `Tests/PlayMode/PopJetSmokeTest.cs` chạy scene gameplay và kích hoạt ngắm, chọc, rò, vá, nổ, tự chơi lại; test thứ hai kiểm tra LevelLoader (thắng 1-1 thì sang 1-2, màn cuối thì đứng yên); test thứ ba kiểm tra nhặt Sao, Par, 3 sao và Vương miện; hai test lưu game kiểm tra ghi/đọc lại, gộp sao, khôi phục khi file hỏng, chơi tiếp màn gần nhất và mở khóa màn. Các test lời giải: chơi 1-13 (quạt, gai di chuyển, lỗ rò), 1-11 (pháo giấy), 1-18 (mèo) theo kịch bản rồi phát lại, đường bay phải trùng từng mẫu; phát lại mọi lời giải đã ghi trong catalog, màn nào không còn về đích thì báo (màn chưa có lời giải thì bỏ qua và ghi vào log). Thêm test Gợi ý (thua 3 lần thì có nút, bóng ma diễn rồi thế giới chạy lại), tutorial bằng hình và cột mốc 1-20. Mọi test dùng file save tạm riêng, không đụng save thật, và không ghi lời giải vào asset dù đang bật ghi. Chạy trong `Window > General > Test Runner > PlayMode`, hoặc headless (Editor phải đóng):

```
Unity.exe -batchmode -projectPath "E:/Unity/my/Test-BB" -runTests -testPlatform PlayMode -testResults results.xml -logFile -
```

## 6. Lưu ý

- Sprite kim cần đầu nhọn hướng lên (+Y), script xoay sao cho đầu kim chĩa vào bóng.
- Particle khí thoát nên phát theo hướng +Y local.
- `BalloonInput` bỏ qua chạm bắt đầu trên phần tử UI có Raycast Target. Ảnh trang trí trên HUD nên tắt `Raycast Target`.
- Rung Android cần quyền VIBRATE: `Haptics` giữ một tham chiếu `Handheld.Vibrate` để Unity tự thêm quyền. Rung iOS dùng `Plugins/iOS/PopJetHaptics.mm` (builder đặt plugin chỉ cho iOS).
- Mỗi vật tương tác mới nên giới thiệu ở màn riêng trước khi trộn với vật khác (GDD §8.2).
