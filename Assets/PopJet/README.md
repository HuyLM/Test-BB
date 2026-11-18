# Pop Jet - hướng dẫn setup (Unity 2D)

Chạy được trên Unity 2021 LTS trở lên (có xử lý riêng cho Unity 6: `linearDamping`) và hỗ trợ cả Input System mới lẫn cũ.

## 1. Cài file

Copy cả thư mục `PopJet` vào `Assets/` của project. Không cần package nào thêm.

## 2. Danh sách script

| Script | Gắn vào | Việc chính |
|---|---|---|
| `BalloonController` | Bóng | Khí, lực đẩy, lỗ chọc, lỗ rò, trạng thái |
| `BalloonInput` | Object rỗng `Input` | Giữ - kéo - thả, kim, mũi tên hướng bay |
| `BalloonFeedback` | Bóng | Âm thanh + particle theo sự kiện |
| `Spike` | Gai | Chạm bóng thì tạo thêm lỗ rò |
| `BalloonBounce` | Bóng | Bóp dẹt + rung mềm và tiếng "bộp" khi va tường (độ nảy thật do `PhysicsMaterial2D`) |
| `PatchPickup` | Keo vá | Vá hết lỗ rò, hồi 30% khí |
| `Fan` | Quạt | Đẩy bóng theo trục +X local |
| `ButtonSwitch` | Nút | Mở cửa gán sẵn |
| `Door` | Cửa | Trượt mở/đóng |
| `GoalZone` | Đích | Hoàn thành màn |
| `LevelManager` | Object rỗng | Thất bại mềm (tự chơi lại), thắng, chuyển màn |
| `PopJetDebug` | Object rỗng | Overlay state/khí/lỗ rò để tune (xóa khi build thật) |

## 3. Dựng scene mẫu (khoảng 10 phút)

**Cách nhanh:** menu `Tools > PopJet > Build Sample Scene` tự dựng toàn bộ scene bên dưới vào `Assets/PopJet/Scenes/PopJet_Sample.unity` (kèm sprite, particle, âm thanh placeholder).

**Camera (Cinemachine 3, không tự code follow):** Main Camera Orthographic + `CinemachineBrain`, màn dọc 9:16 (Game view đặt Portrait). Object `CM FollowCam` có `CinemachineCamera` (Follow = `Balloon`, Ortho Size 3.5) + `CinemachinePositionComposer` (Damping 0.4, Dead Zone 10%, Lookahead 0.25 giây). **Không** dùng `CinemachineConfiner2D`: chặn camera ở mép màn thì bóng sát góc sẽ nằm sát mép màn hình, không còn chỗ kéo kim. Bật `Interpolate` cho `Rigidbody2D` của bóng để camera không giật.

**Bóng (`Balloon`)**
1. Tạo Sprite hình tròn (`Create > 2D > Sprites > Circle`), đặt tên `Balloon`.
2. Add `Rigidbody2D`, `CircleCollider2D`, `BalloonController`. Script tự đặt gravity = 0, damping, khóa xoay.
3. (Tùy chọn) Add `AudioSource` + `BalloonFeedback`, kéo clip và particle vào.
4. Nảy mềm: gán `PhysicsMaterial2D` (Bounciness 0.85, Friction 0.05) vào `CircleCollider2D`. Đưa `SpriteRenderer` xuống object con `Visual`, Add `BalloonBounce` vào bóng rồi kéo `Visual` vào ô `Visual` (không bóp scale gốc vì scale gốc thể hiện lượng khí).

**Input**
1. Tạo object rỗng `Input`, Add `BalloonInput`, kéo `Balloon` vào.
2. Tạo Sprite `Needle` (hình tam giác nhỏ, **đầu nhọn hướng lên**), kéo vào ô `Needle`.
3. Tạo object `FlightArrow` với `LineRenderer` (Width 0.08, Use World Space bật), kéo vào ô `Flight Arrow`.

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
| Lực chọc / thời gian | 6 / 1.5 giây | `BalloonController` | Bóng bay không đủ xa: tăng lực trước, không tăng thời gian |
| Khí mất mỗi lần chọc | 0.18 | `BalloonController` | Người mới hết khí ở 5 màn đầu: giảm xuống 0.10-0.15 |
| Lỗ rò: lực / thời gian / khí | 1.8 / 3 giây / 0.06 | `BalloonController` | Bóng "tự bay lung tung": giảm lực rò trước |
| Cooldown giữa 2 lỗ rò | 0.5 giây | `BalloonController` | Trượt dọc gai vẫn bị kẹt: tăng cooldown |
| Số lỗ rò tối đa | 4 | `BalloonController` | |
| Vùng hủy | 40% bán kính bóng | `BalloonInput` | Người mới hủy nhầm: thu nhỏ |
| Lực quạt | 4 | `Fan` | Quạt quá yếu/mạnh so với lực chọc: chỉnh theo tỉ lệ với lực chọc |
| Dừng thế giới khi giữ ngắm | Bật | `BalloonInput` → `Freeze World While Aiming` | Đang giữ chọn hướng kim thì `Time.timeScale = 0` (vật lý, quạt, cửa, particle, camera đều dừng). `LevelManager.PlayTime` dùng unscaled time nên vẫn đếm. Đồng hồ bắt đầu từ lần chọc đầu, dừng khi thắng/thua |
| Hết khí = vỡ tung | Bật | `BalloonController` → `Pop When Empty` | Bóng đứng yên, ẩn đi, `BalloonFeedback` bắn prefab `PopBurst` (mảnh vỡ cùng màu bóng + chớp sáng + khí trắng), phát `Pop Clips`, gọi `On Popped` → `CinemachineImpulseSource.GenerateImpulse` rung camera (listener trên `CM FollowCam`). Tắt thì quay về kiểu xẹp rơi với `Empty Gravity Scale` |
| Rung camera khi vỡ | biên độ 0.18, 0.35 giây | `Balloon` → `CinemachineImpulseSource` (`Default Velocity`, `Impulse Duration`) | Rung chóng mặt: giảm biên độ. Không cảm thấy nổ: tăng lên 0.3 |
| Trọng lực khi đang chơi | 0.05 (rơi ~0.33 đơn vị/giây) | `BalloonController` → `Play Gravity Scale` | Bóng đứng im trước lần chọc đầu (state `Ready`), sau đó rơi chậm. Rơi nhanh quá/khó giữ bóng: giảm; không thấy áp lực: tăng 0.02 mỗi lần |
| Khí hồi khi ăn keo | 0.3 (gấp 3 mức cũ 0.1) | `PatchPickup` | Màn quá dễ vì keo hồi quá nhiều: giảm về 0.2 |
| Độ nảy / ma sát bóng | 0.85 / 0.05 | `BalloonBouncy.physicsMaterial2D` | Bóng "bật như cao su": giảm bounciness. Bóng dính tường: giảm friction |
| Bóp dẹt tối đa / tốc độ dẹt hết cỡ | 0.35 / 2.5 | `BalloonBounce` | Không thấy mềm: tăng `Max Squash`. Rung quá lâu: tăng `Spring Damping` |
| Camera: damping / dead zone / ortho size | 0.4 / 10% / 3.5 | `CM FollowCam` | Camera trễ quá: giảm damping. Không thấy đường bay: tăng ortho size hoặc lookahead |

**Tuning vật lý:** `Mass` = 1 và damping 1.5 cho vận tốc tối đa khoảng 4 đơn vị/giây khi xì đủ lực. Nếu bóng nặng hoặc nhẹ quá, chỉnh `Damping` và lực chọc cùng nhau.

## 5. Playtest nhanh (smoke test)

| Test | Cách thử | Đạt khi |
|---|---|---|
| Người mới | Đưa máy cho người chưa biết luật, không nói gì | Đoán đúng hướng bay ≥ 8/10 lần trong 2 phút |
| Stress | Chạm liên tục, kéo ra ngoài màn, thả giữa chừng | Không lỗi, không kẹt state Aiming |
| Gai | Cho bóng trượt dọc thanh gai | Không sinh quá 4 lỗ cùng lúc, bóng không bị kẹt |
| Hết khí | Chọc đến khi hết | Bóng xẹp, rơi nhẹ, tự chơi lại sau 1.5 giây |
| Vá | Bị gai rồi chạm keo | Lỗ rò dừng ngay, lực rò biến mất |

## 6. Lưu ý

- Sprite kim cần đầu nhọn hướng lên (+Y), script xoay sao cho đầu kim chĩa vào bóng.
- Particle khí thoát nên phát theo hướng +Y local.
- Nếu bạn dùng UI (Canvas), cần chặn chạm xuyên UI: script này chưa xử lý, vì scene mẫu chưa có UI.
- Mỗi vật tương tác mới nên giới thiệu ở màn riêng trước khi trộn với vật khác (xem bản nháp 10 màn trong cuộc trò chuyện).
