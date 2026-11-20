# Pop Jet: đề xuất thiết kế game mobile (GDD v0.2)

> Bản đề xuất dựa trên prototype đang có trong `Assets/PopJet/`. Đây là tài liệu để thảo luận, chưa phải spec đã chốt.
> **Mọi con số mới trong tài liệu là giá trị khởi điểm** (starting value). Con số nào cũng có cách test và hướng chỉnh nếu không đạt, theo đúng quy ước của `README.md`. Nguồn chỉ được trích cho các quy chuẩn kiểm chứng được (Apple HIG, Material Design, WCAG, App Store Review Guidelines).
> Mockup: [mockup-gameplay-hud.svg](mockup-gameplay-hud.svg) · [art-bible.svg](art-bible.svg)
> **v0.2 (2026-11-19):** đã chốt 6 quyết định ở §20: F2P, người chơi từ 13 tuổi, art 100% bằng AI theo hướng giấy cắt, game chỉ có tiếng Anh, Bịt lỗ để sau, giữ rò chiến thuật.

## Mục lục

0. Tóm tắt một trang
1. Prototype đang có gì
2. Bản sắc game
3. Gameplay lõi
4. Tính năng bóng bay
5. Vật cản
6. Vật hỗ trợ
7. Item và booster
8. Thế giới và level design
9. Art style
10. UI/UX
11. Âm thanh và rung
12. Tiến trình và kinh tế
13. Kiếm tiền
14. Đo lường
15. Kỹ thuật: gắn vào code hiện tại
16. Lộ trình
17. Rủi ro và cách bị lạm dụng
18. Kịch bản playtest
19. Bảng tuning tổng
20. Quyết định đã chốt và việc còn mở

---

## 0. Tóm tắt một trang

**Pitch:** Bạn là một quả bóng bay, và cách duy nhất để di chuyển là tự chọc thủng mình. Mỗi cú chọc đẩy bóng đi nhưng làm bóng xẹp bớt. Phải về tới đích trước khi hết hơi.

| | |
|---|---|
| Thể loại | Giải đố vật lý, một ngón tay, màn dọc 9:16 |
| Phiên chơi | Mỗi màn 20–60 giây, mỗi phiên 5–15 phút (giả định, xem §20) |
| Nhân vật | **Pốp** (tên tạm trong tài liệu; tên tiếng Anh chưa chốt, xem §10.8), quả bóng bị bỏ quên sau bữa tiệc sinh nhật, muốn bay lên "Mặt Trăng Bóng Bay" (Balloon Moon), nơi các quả bóng thất lạc tụ về |
| Cấu trúc | 6 thế giới đi từ thấp lên cao: Ngôi nhà → Khu vườn → Thành phố → Xưởng đồ chơi → Tầng mây bão → Vũ trụ. Mỗi thế giới 20 màn |
| Mục tiêu mỗi màn | 3 sao: **Về đích**, **Nhặt Sao**, **Không vượt Par** (Par là số lần chọc chuẩn) |
| Art | Thế giới giấy cắt màu mờ, chỉ có quả bóng bóng loáng như đồ chơi vinyl. Toàn bộ tạo bằng công cụ tạo ảnh của ChatGPT (§9.6) |
| Kiếm tiền | F2P: quảng cáo có thưởng do người chơi tự chọn xem, gói xóa quảng cáo, skin. Không có hệ thống "mạng" chặn lượt chơi |
| Người chơi, ngôn ngữ | Từ 13 tuổi. Game chỉ có tiếng Anh, soft launch ở một nước nói tiếng Anh |

**Ba trụ cột.** Mọi ý tưởng mới phải phục vụ ít nhất một trụ và không được phá trụ nào:

1. **Mỗi cú chọc là một quyết định.** Thế giới dừng khi ngắm, nên đây là game suy nghĩ chứ không phải game phản xạ.
2. **Khí là tất cả.** Khí vừa là nhiên liệu, vừa là kích thước, lực đẩy và mạng sống.
3. **Mềm, nảy, buồn cười.** Thất bại phải vui và nhanh: nổ "bụp" rồi chơi lại trong chưa tới 2 giây.

**Làm gì trước** (chi tiết ở §16):

1. Vá các lỗ hổng Clarity và Response của prototype: dự đoán quỹ đạo, vòng khí, lý do thua, nút chơi lại, chặn chạm xuyên UI.
2. Làm trọn 20 màn của Thế giới 1 với art thật, dùng làm vertical slice.
3. Sau đó mới thêm meta (tiến trình, skin) và kiếm tiền.

---

## 1. Prototype đang có gì

### 1.1 Chấm theo 5 tiêu chí

| Tiêu chí | Đang tốt | Lỗ hổng | Đề xuất (mục) |
|---|---|---|---|
| **Response** (người chơi kiểm soát được) | Thế giới dừng khi ngắm, có vùng hủy, lực áp dụng ngay khi thả | Thả tay xong là hết quyền: không bịt được lỗ, không xem trước được màn, chưa có nút chơi lại. Khi có UI, chạm lên nút sẽ lọt xuống thành ngắm | Bịt lỗ (thử nghiệm, §3.6), kéo nền để nhìn quanh, nút Retry, chặn chạm UI (§15) |
| **Clarity** (đoán được kết quả) | Kim hiện phía ngón tay, có mũi tên hướng bay, kích thước bóng thể hiện lượng khí | Mũi tên dài cố định: không cho biết bay bao xa, không tính quán tính, trọng lực và cú nảy. Lượng khí khó đọc chính xác (scale chỉ đổi từ 1.0 xuống 0.55). Không biết lần chọc tới tốn bao nhiêu khí. Lỗ rò chỉ phụt khói một lần, nên bóng "tự trôi" mà không rõ vì sao. Thua không ghi lý do. Đích có thể nằm ngoài màn hình (camera cao 7 đơn vị, màn chơi cao 10.6) | Quỹ đạo dự đoán, vòng khí chia ô kèm xem trước chi phí, dòng khí rò liên tục, lý do thua, camera giới thiệu màn, mũi tên chỉ đích (§3, §10) |
| **Satisfaction** (phản hồi) | Phụt khí, mỗi âm có 3 biến thể, bóp dẹt khi va, nổ tung kèm rung camera | Chưa rung máy (haptics), bóng chưa có tính cách, về đích chỉ có một tiếng | Mặt bóng biểu cảm, dây bóng, haptics, âm thanh "thời gian dừng", ăn mừng 3 sao (§4, §11) |
| **Fit** (hợp bản sắc) | Vật lý mềm và nảy, "khí = mạng" rất mạch lạc | Art còn placeholder, chưa chốt tông (dễ thương hay nghiêm túc) | Art bible ở §9 |
| **Motivation** (lý do để quan tâm) | Đã có `PlayTime` | Chưa có sao, điểm, tiến trình hay đồ sưu tầm. Thua thì tự chơi lại, thắng thì bóng đứng yên | 3 sao và Par, bản đồ thế giới, skin, thử thách hằng ngày (§12) |

Thứ tự sửa theo quy tắc ưu tiên Response > Clarity > Satisfaction > Fit > Motivation. Motivation là lỗ hổng lớn nhất nhưng nên làm sau: gắn sao và tiến trình lên một lõi còn khó đoán chỉ khuếch đại sự bực bội.

### 1.2 Những tính chất ẩn trong code nên biến thành thiết kế có chủ đích

1. **Bóng xẹp thì nhỏ lại, collider cũng nhỏ theo.** `ApplyScale()` đổi `localScale` của object gốc nên `CircleCollider2D` co theo. Đường kính lúc đầy là 0.9 đơn vị (bán kính collider 0.5 × scale gốc 0.9 = 0.45), lúc gần cạn còn khoảng 0.5 (× `minScale` 0.55). Đây là một cơ chế puzzle có sẵn mà không tốn code: **"xẹp để lọt khe"** (§3.3). Nó cũng là luật bắt buộc khi dựng màn: khe nào muốn chặn bóng tuyệt đối thì phải hẹp hơn 0.45 đơn vị.
2. **Lỗ rò tiết kiệm khí gấp khoảng 1.8 lần cú chọc.** Tổng xung lực của một jet là lực × thời gian / 2. Cú chọc cho 6 × 1.5 / 2 = 4.5 và tốn 0.18 khí, tức 25 trên mỗi đơn vị khí. Lỗ rò cho 1.8 × 3 / 2 = 2.7 và tốn 0.06 khí, tức 45 trên mỗi đơn vị khí. Với damping tuyến tính, quãng đường tỉ lệ thuận với xung lực, nên cố tình cọ vào gai là cách "bay rẻ" (tối đa 4 lỗ, tốn 0.24 khí). **Đã chốt (§20): giữ lại** làm kỹ thuật nâng cao ("rò chiến thuật", Par tính cả cách này), vì nó biến vật cản thành công cụ (§8.2). Phương án bỏ (tăng `leakAirCost` lên khoảng 0.1) chỉ dùng nếu Abuse test ở §18 cho thấy nó phá hỏng màn.
3. **Ngắm rồi hủy giống như tua từng khung hình.** Giữ tay (thế giới dừng), hủy (thế giới chạy), giữ lại: người chơi "nhích" được thời gian để canh vật cản động. Đây không phải bug, nó hợp trụ cột 1. Hệ quả cho level design: **không màn nào được đòi phản xạ**, vật cản động chỉ là bài toán dự đoán.
4. **Chọc dồn thì lực cộng dồn.** Hai cú chọc liền nhau tạo hai jet cùng lúc, thành cú bứt tốc. Giữ nguyên, đây là kỹ năng tự nhiên.
5. **Trạng thái Ready không có trọng lực.** Lần chọc đầu tiên được suy nghĩ bao lâu cũng được, nên mở màn luôn an toàn. Giữ nguyên.

### 1.3 Thiết lập mobile cần sửa ngay (tìm thấy trong project)

| Vấn đề | Hiện tại | Sửa |
|---|---|---|
| Xoay màn hình | `defaultScreenOrientation: 4` (tự xoay, cho phép cả màn ngang) | Player Settings → Default Orientation = **Portrait** |
| Tên và ID | `productName: Test-B`, `companyName: DefaultCompany` | Đặt tên thật và bundle id trước khi lên store |
| FPS | Chưa đặt `targetFrameRate` (trên mobile, mặc định của Unity thường là 30 fps) | `Application.targetFrameRate = 60` khi khởi động |
| Chiều ngang nhìn thấy | Ortho size cố định 3.5: máy 9:16 thấy 3.9 đơn vị chiều ngang, máy 9:20 chỉ còn 3.15, trong khi màn mẫu rộng 5.6 | Tính ortho size theo tỉ lệ màn hình để giữ chiều ngang cố định (§15.4) |
| Chạm xuyên UI | `BalloonInput` chưa chặn (README đã ghi chú) | Kiểm tra `EventSystem.current.IsPointerOverGameObject(...)` trước khi gọi `BeginAim` |
| Overlay debug | `PopJetDebug` vẫn chạy trong bản build | Bọc trong `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` |
| Chưa có UI | Chưa có nút Retry hay Pause. Nếu bị kẹt mà còn khí, cách duy nhất là chọc tới khi nổ | HUD tối thiểu ở §10.3 |

---

## 2. Bản sắc game

### 2.1 Fantasy và giọng điệu

"Một quả bóng bay nhỏ, dũng cảm và hơi ngốc, tự chọc mình để về trời." Giọng điệu dễ thương, hài kiểu phim hoạt hình câm (không thoại). Nguy hiểm phải rõ ràng nhưng không đáng sợ. Pốp nổ là chuyện buồn cười, không phải chuyện buồn.

### 2.2 Câu chuyện khung (không cần thoại)

Sau bữa tiệc sinh nhật, mọi quả bóng đều đã bay ra cửa sổ, chỉ còn Pốp mắc kẹt dưới gầm bàn. Đêm đó Pốp thấy trên trời một vầng trăng làm từ hàng nghìn quả bóng bay. Mỗi thế giới là một chặng đường đi lên. Cuối mỗi thế giới, Pốp cứu một "bạn bóng" đang bị mắc kẹt; bạn này đi theo Pốp trên bản đồ và mở khóa một skin. Câu chuyện được kể bằng một cảnh ngắn 3–5 giây, không chữ, ở đầu và cuối mỗi thế giới.

Lý do chọn hành trình đi lên: hợp với màn dọc (bản đồ cuộn lên trên), hợp với fantasy bóng bay (bóng nào cũng muốn bay lên), và cho mỗi thế giới một lý do chủ đề rõ ràng để đổi vật cản.

### 2.3 Game này KHÔNG phải là

- **Không phải game phản xạ.** Không màn nào đòi bấm đúng khoảnh khắc mà không thể dừng hình.
- **Không phải endless hay hyper-casual một chạm.** Màn được thiết kế bằng tay và có lời giải.
- **Không chặn lượt chơi bằng "mạng" hay "năng lượng".** Thua là chuyện thường ngày của game vật lý, nên chơi lại phải miễn phí.
- **Không pay-to-win.** Loại bóng có ảnh hưởng gameplay do màn quy định; skin chỉ để trang trí.

### 2.4 Bộ lọc ý tưởng

Trước khi thêm một vật hay item mới, phải trả lời "có" cho ít nhất một câu:

1. Nó có thay đổi **hướng bay**, **lượng khí** hoặc **kích thước** của bóng không?
2. Nó có tạo ra một **quyết định mới** lúc đang ngắm (chứ không chỉ thêm một con số mới) không?
3. Nhìn một khung hình tĩnh lúc thế giới đang dừng, người chơi có hiểu được nó không?

Không trả lời "có" được câu nào thì loại, hoặc đưa vào "bãi đỗ ý tưởng" (§16.3).

---

## 3. Gameplay lõi

### 3.1 Vòng lặp ba tầng

| Tầng | Thời lượng | Người chơi làm gì | Phần thưởng |
|---|---|---|---|
| Khoảnh khắc | 3–10 giây | Nhìn → giữ tay (thế giới dừng) → kéo ngắm, xem quỹ đạo → thả → xem bóng bay và nảy → phản ứng (vá lỗ, chọc tiếp) | Cảm giác "đúng như mình tính", tiếng phụt, cú nảy |
| Màn | 20–60 giây | Đọc màn (camera giới thiệu) → lên kế hoạch đường bay → thực hiện → về đích | Sao, xu, biểu cảm ăn mừng |
| Meta | Nhiều ngày | Mở thế giới mới, sưu tầm skin, chơi thử thách hằng ngày | Thế giới mới, skin, bạn bóng |

### 3.2 Điều khiển

Giữ nguyên lõi **giữ trên bóng → kéo → thả**, bổ sung những phần sau:

| Bổ sung | Mô tả | Tiêu chí | Giá trị khởi điểm và cách test |
|---|---|---|---|
| Quỹ đạo dự đoán | Chuỗi chấm đi từ tâm bóng theo đường bay dự kiến, tính cả vận tốc hiện tại, trọng lực và damping. Dừng ở điểm va chạm đầu tiên và đánh dấu điểm đó | Clarity | Dài tối đa 0.6 giây mô phỏng, hoặc tới va chạm đầu tiên. Test: người mới đoán đúng vùng bóng sẽ tới ≥ 8/10 lần trong 2 phút. Không đạt thì tăng lên 1.0 giây. Nếu người giỏi lấy ★Par quá dễ (> 60% số lần qua màn) thì giảm còn 0.4 |
| Vòng khí quanh bóng | Chỉ hiện khi ngắm. Chia thành ô, mỗi ô là 1 lần chọc (`ventAirCost`). Ô sắp bị tiêu thì nhấp nháy | Clarity | 1 ô = 0.18 khí (khoảng 5.5 ô). Test: hỏi "còn mấy lần chọc?", trả lời đúng ≥ 9/10. Không đạt thì thêm số "×3" cạnh vòng |
| Cảnh báo cú chọc cuối | Nếu cú này làm khí về 0 thì vòng và kim chuyển sang cam, hiện icon "bụp" | Clarity | Bật khi `air <= ventAirCost`. Test: trong Readability test không ai bị "nổ bất ngờ" |
| Camera giới thiệu màn | Đầu màn, camera bắt đầu ở đích rồi lướt về bóng | Clarity | 1.5 giây, chạm để bỏ qua. Khi chơi lại thì tự bỏ qua |
| Kéo nền để nhìn quanh | Kéo ở vùng không có bóng thì camera đi theo ngón tay, thả ra thì camera quay về bóng | Response, Clarity | Giới hạn trong biên màn chơi |
| Chỉ báo ngoài màn hình | Mũi tên ở mép màn hình chỉ về Đích và Sao khi chúng nằm ngoài khung | Clarity | Luôn bật |
| Lùi camera khi ngắm | Khi giữ tay, camera lùi ra một chút để thấy rộng hơn | Clarity | Ortho size +15%, chuyển trong 0.25 giây bằng unscaled time. Phải zoom quanh tâm bóng (dịch camera bù lại), nếu không hướng kim sẽ trôi trong lúc zoom. Có người thấy chóng mặt thì tắt |

Phương án điều khiển phụ (tùy chọn trong Settings, làm sau): **"Kéo tự do"**. Chạm ở đâu cũng được, hướng kim là hướng kéo tính từ điểm chạm. Cách này giải quyết chuyện ngón tay che bóng và bóng nằm sát mép màn hình. Không bật chung với "kéo nền để nhìn quanh" vì hai cử chỉ xung đột.

### 3.3 Khí: một tài nguyên, bốn ý nghĩa

| Ý nghĩa | Luật | Quyết định nó tạo ra |
|---|---|---|
| Nhiên liệu | Mỗi cú chọc tốn `ventAirCost` (0.18) | Chọc càng ít càng tốt (Par) |
| Kích thước | scale = lerp(0.55, 1, khí), collider co theo | Tiêu khí để lọt khe hẹp, hay giữ khí để khỏi lọt nhầm |
| Lực | Khí thấp thì lực chỉ còn `minThrustWhenLow` (60%) | Cú chọc quan trọng nên làm sớm, khi còn mạnh |
| Mạng | Khí về 0 thì nổ | Có nên mạo hiểm đi qua đường gai để lấy Sao không? |

**Luật kích thước cho level design** (đường kính bóng lúc đầy D = 0.9 đơn vị):

| Loại khe | Bề rộng (đơn vị) | Ý nghĩa |
|---|---|---|
| Khe chặn | < 0.45 (0.5 D) | Bóng không bao giờ lọt, kể cả lúc gần cạn |
| Khe "xẹp mới lọt" | 0.68–0.77 (0.75–0.85 D) | Phải còn ≤ 45–65% khí, tức đã chọc 2–3 lần. Cộng thêm 5% dung sai vì bóng nảy |
| Khe thoải mái | > 1.0 (1.1 D) | Lọt ở mọi mức khí |

Công thức cho designer: bóng lọt khe rộng `g` (tính theo D) khi `khí ≤ (g − 0.55) / 0.45`.

### 3.4 Thắng và 3 sao

| Sao | Điều kiện | Quyết định nó tạo ra |
|---|---|---|
| ★ Về đích | Chạm `GoalZone` | Hoàn thành màn |
| ★ Nhặt Sao | Chạm vật phẩm Sao (mỗi màn 1 cái), đặt lệch khỏi đường ngắn nhất | Đi vòng thì tốn khí và có rủi ro |
| ★ Par | Về đích với số lần chọc ≤ Par | Tìm đường thông minh: nảy tường, dùng quạt, rò chiến thuật |

- Sao được lưu **riêng từng cái** qua nhiều lượt chơi: lần này lấy Sao, lần sau lấy Par thì vẫn đủ 3 sao. Như vậy dễ tiếp cận hơn.
- Ai lấy đủ 3 sao **trong cùng một lượt** thì được thêm dấu **Vương miện**. Đây là mục tiêu cho người chơi giỏi, không chặn tiến trình của ai.
- Par khởi điểm bằng số lần chọc trong lời giải tốt nhất của designer. Test: tỉ lệ đạt ★Par trên số lần qua màn nằm trong khoảng 20–40%. Dưới 10% thì Par + 1. Trên 60% thì Par − 1 hoặc chỉnh lại màn.
- "Par" mượn ngôn ngữ golf, nên "một chọc ăn ngay" (hole-in-one) có thể thành một thành tựu.

### 3.5 Thất bại

| Bước | Hiện tại | Đề xuất |
|---|---|---|
| Lý do thua | Không có | Hiện icon và chữ ngắn ngay tại chỗ nổ trong 1 giây: "Out of air!", "Burned!", "Zapped!", "Cat attack!"... (bảng đầy đủ ở §10.8) |
| Chơi lại | Tự chơi lại sau 1.5 giây | Vẫn tự động, nhưng chạm bất kỳ đâu để chơi lại ngay. Khởi điểm 1.2 giây. Test: trung vị thời gian từ lúc nổ tới lần chạm đầu tiên ở lượt mới < 3 giây |
| Bị kẹt | Không phát hiện | Nếu khí còn ít hơn 1 cú chọc và tốc độ < 0.05 đơn vị/giây trong 3 giây: nút Retry nhấp nháy kèm chữ "Kẹt rồi?" |
| Gợi ý | Không có | Thua cùng một màn 3 lần thì mời xem gợi ý, miễn phí ở lần đầu: một bóng ma diễn lại cú chọc đầu tiên của lời giải |
| Hồi sinh (bằng quảng cáo có thưởng) | Không có | Chỉ mời khi đáng: khoảng cách tới đích đã giảm ≥ 50% so với lúc đầu VÀ lượt này chưa hồi sinh. Bóng hồi sinh tại vị trí của lần chọc cuối, với 40% khí, hết lỗ rò, vận tốc 0, bất tử 1 giây (nhấp nháy). Tối đa 1 lần mỗi lượt |

### 3.6 Thử nghiệm: "Bịt lỗ" (đã chốt: để sau vertical slice, A/B test ở Giai đoạn 3)

Ý tưởng: khi bóng đang xì khí, chạm nhanh vào bóng để bịt lỗ. Cú chọc dừng sớm và **phần khí chưa xì được giữ lại**. Cú chọc cố định trở thành cách điều khiển quãng đường; người chơi đổi ý được giữa chừng (Response) và có thêm chỗ thể hiện kỹ năng.

| Mục | Định nghĩa |
|---|---|
| Điều kiện vào | Đang `Venting` (`VentCount > 0`), ngón tay chạm xuống trong vùng hủy rồi nhấc lên trong < 0.2 giây mà không ra khỏi vùng hủy |
| Hiệu ứng | Xóa mọi jet chủ động (không xóa lỗ rò). Khí còn lại giữ nguyên, vì khí chỉ bị trừ theo phần lực đã xì |
| Điều kiện ra | Ngay lập tức về `Floating` |
| Bị ngắt bởi | Không áp dụng (xảy ra tức thời) |
| Chi phí | Miễn phí |
| Ca biên | Chỉ có lỗ rò, không có cú chọc nào: không tác dụng (lỗ rò phải vá bằng keo). Kéo ra ngoài vùng hủy rồi quay lại: coi là hủy ngắm, không bịt. Bóng đã hết khí hoặc đã về đích: bỏ qua |
| Phản hồi | Tiếng "chụt", mặt bóng mím môi, haptic nhẹ, khói phụt tắt ngay |
| Rủi ro | Người chơi chỉ muốn hủy ngắm nhưng vô tình bịt lỗ. Màn bị "giải bằng tay" thay vì bằng đầu |
| Test | A/B giữa nhóm bật và nhóm tắt. So số lần chọc trung bình mỗi màn, tỉ lệ ★Par, tỉ lệ bỏ game. Sau buổi chơi hỏi: "có lúc nào bóng dừng lại mà bạn không muốn không?" Nếu > 2/10 người trả lời có thì bỏ tính năng hoặc đổi cử chỉ |

### 3.7 Luật thời gian (dễ phá nhất khi thêm tính năng)

- **Thế giới chạy theo scaled time** để dừng khi ngắm: vật cản di chuyển, quạt chạy theo chu kỳ, hoạt ảnh báo trước, particle. Đây là điều mong muốn: nhìn khung hình đứng yên là lên được kế hoạch.
- **Mọi thứ phục vụ việc ngắm và UI chạy theo unscaled time**: vòng khí, quỹ đạo, mắt bóng nhìn theo ngón tay, hoạt ảnh nút, chuyển camera khi ngắm (bật Ignore Time Scale trên CinemachineBrain hoặc tự blend bằng unscaled time).
- Animator của UI dùng Update Mode = Unscaled Time. Animator của vật cản dùng Normal.
- Dùng một `TimeController` gom các lý do dừng (đang ngắm, đang pause). Có như vậy thì `EndAim` mới không trả `timeScale` về 1 trong lúc menu Pause còn mở. Mọi đường chuyển scene và `OnApplicationPause` đều đi qua nó.

---

## 4. Tính năng bóng bay

### 4.1 Mặt bóng và "diễn xuất"

Mặt gồm hai mắt to (hình bầu dục tối màu có đốm sáng) và một miệng nhỏ. Mắt có ba việc: nhìn theo ngón tay khi ngắm (unscaled time), nhìn về vật nguy hiểm gần nhất (báo trước), và nhìn theo hướng bay.

| Tình huống | Biểu cảm | Mục đích |
|---|---|---|
| Ready (chờ lần chọc đầu) | Chớp mắt, lắc lư nhẹ | Sống động, mời người chơi chạm vào |
| Đang ngắm | Mắt nhìn theo ngón tay, nheo một mắt | Xác nhận input |
| Vừa chọc | Nhăn mặt "ui!" 0.3 giây | Hài, phản hồi |
| Bay nhanh | Mắt híp, má phồng | Cảm giác tốc độ |
| Cách vật nguy hiểm < 1.5 đơn vị | Mắt mở to, nhìn về phía nó | Báo trước (Clarity) |
| Có lỗ rò | Hoảng, giọt mồ hôi | Báo đang mất khí |
| Vừa được vá | Thở phào | Phản hồi |
| Khí còn chưa tới 1 cú chọc | Mệt, mồ hôi, phập phồng như thở | Cảnh báo khí |
| Va mạnh | Mắt xoáy ốc 0.5 giây | Hài |
| Về đích | Cười, nảy lên | Ăn mừng |
| Sắp nổ | Mặt "ồ" 0.1 giây trước khi nổ | Hài |

**Lưu ý kỹ thuật:** `BalloonBounce` xoay `Visual` theo pháp tuyến va chạm ("sprite tròn nên xoay không lộ"). Gắn mặt và đốm sáng vào `Visual` thì chúng sẽ bị xoay theo mỗi lần nảy. Cách sửa: mặt là con của `Visual` (để được bóp dẹt theo), và mỗi `LateUpdate` đặt `localRotation = Quaternion.Inverse(visual.localRotation)`, chạy sau `BalloonBounce` (dùng Script Execution Order hoặc gộp vào cùng script).

### 4.2 Dây bóng

Sợi dây 6–8 đốt mô phỏng verlet, vẽ bằng `LineRenderer`, gắn vào núm dưới bóng trên object gốc (gốc không xoay vì `freezeRotation`). Ở MVP dây chỉ để nhìn, không va chạm: dây kéo ngược hướng bay nên cho thấy hướng và tốc độ, và làm bóng sống động hơn. Dây chạy theo scaled time nên đứng sững giữa chừng khi thế giới dừng, đúng cảm giác "thời gian ngừng". Về sau dây dùng để móc chìa khóa và tạ (§7).

### 4.3 Lỗ rò nhìn thấy được

- Mỗi lỗ rò là một decal lỗ nhỏ trên mặt bóng, đặt tại điểm chạm, gắn vào object gốc nên tự co giãn theo khí.
- Một dòng khí phun liên tục (particle loop) từ lỗ theo hướng ra ngoài, cường độ theo hệ số `k` của jet và tắt khi jet hết hạn. Hiện `LeakAdded` chỉ phụt khói một lần. Cần thêm sự kiện `LeakEnded`, hoặc expose danh sách jet chỉ đọc để phần hình ảnh đọc trạng thái.
- Một âm "xì" loop mỏng cho mỗi lỗ, nhỏ dần theo `k`.
- Khi vá, mỗi lỗ được dán một miếng băng keo chéo và giữ tới hết màn. Quả bóng "đầy sẹo chiến trận" vừa dễ thương vừa kể lại được chuyện vừa xảy ra.

Nhờ vậy câu hỏi "vì sao bóng tự trôi?" trong Readability test có câu trả lời ngay trên màn hình.

### 4.4 Các loại bóng (do màn quy định, không do người chơi chọn)

Vì loại bóng thay đổi lời giải, mỗi màn tự quy định loại bóng. Người chơi không được chọn, nên không có chuyện màn nào đó không giải được với một loại bóng nào đó.

| Bóng | Khác gì | Quyết định mới | Thế giới | Tham số khởi điểm | Rủi ro |
|---|---|---|---|---|---|
| Bóng thường | Chuẩn | Không | Tất cả | Như `README` | Không |
| Bóng Heli | Trọng lực âm, tự bay lên chậm | Phải chọc để đi xuống; trần nhà thành "sàn" | 5 Tầng mây | `playGravityScale = -0.05` (code hiện tại đã nhận số âm) | Người chơi quen tay chọc ngược. Cần 2–3 màn giới thiệu |
| Bóng giấy bạc | Gai chỉ gây rò tốn một nửa khí, nảy kém, phản xạ tia laser | Bền hơn nhưng mất cú nảy tường; tia laser thành công cụ | 4 Xưởng | `leakAirCost` ×0.5, bounciness 0.3 | Phản xạ laser cần code riêng |
| Bóng nước | Nặng, rơi nhanh, mỗi cú chọc phun nước (mạnh hơn nhưng ít lần hơn), dập được lửa | Phun nước vào lửa để mở đường | 3 Thành phố | mass 1.6, gravity 0.15, thrust 8, cost 0.3 (khoảng 3 lần chọc) | Có thể thấy nặng nề. Test Fit |
| Bóng phát sáng | Mang ánh sáng trong màn tối, khí càng ít thì vùng sáng càng hẹp | Khí = tầm nhìn: tiêu khí là tự làm mình mù dần | 3 (màn đêm), 6 | Bán kính `Light2D` = lerp(1.5, 3.5, khí) | Light 2D tốn hiệu năng trên máy yếu. Giới hạn 1–2 đèn mỗi màn |
| Bóng đôi | Hai bóng nối nhau bằng `DistanceJoint2D`, mỗi quả có khí riêng, cả hai phải về đích | Chọc quả này thì kéo theo quả kia; phải điều phối hai bóng | Màn đặc biệt từ thế giới 4 | Dây dài 1.2 đơn vị | Input phải biết đang chọn quả nào. Chỉ dùng cho màn đặc biệt |

Mỗi loại bóng là một `BalloonProfile` (ScriptableObject) gắn vào `LevelDefinition`, xem §15.

### 4.5 Trạng thái phụ (status effect)

Trạng thái phụ chạy song song với state chính (`Floating`, `Venting`...). Chúng được lưu thành danh sách giống `jets`, không thêm vào enum `BalloonState`.

| Trạng thái | Vào khi | Hiệu ứng | Ra khi | Bị ngắt bởi | Ca biên |
|---|---|---|---|---|---|
| **Khiên băng dính** | Nhặt băng dính hoặc dùng booster | Lần chạm gai tiếp theo không tạo lỗ rò, khiên rách | Bị chạm gai 1 lần | Không | Vật cản loại "nổ ngay" vẫn làm nổ: khiên chỉ chống rò |
| **Co lạnh** | Vào vùng lạnh hoặc ăn kẹo bạc hà | Kích thước ×0.7, không tốn khí | Rời vùng thêm 1 giây, sau đó phình lại trong 0.5 giây | Vào vùng nóng (hai hiệu ứng triệt tiêu) | Đang nằm trong khe hẹp mà phình lại: kiểm tra `Physics2D.OverlapCircle` trước khi phình, nếu chồng lấn thì hoãn |
| **Nở nóng** | Vào luồng hơi nóng | Kích thước ×1.15, thêm lực đẩy lên | Rời vùng thêm 0.5 giây | Vào vùng lạnh | Giống ô trên |
| **Dính** | Chạm keo, mật ong, mạng nhện | Vận tốc về 0, đứng yên tại chỗ | Cú chọc tiếp theo (tính như cú chọc thường, không tốn thêm) | Hết khí | Bị rò lúc đang dính: bỏ qua lực rò cho tới khi thoát, nhưng khí vẫn mất |
| **Ướt** | Vòi tưới, mưa | Khối lượng +50% trong 4 giây (rơi nhanh hơn, chọc đi kém xa hơn) | Hết giờ, hoặc vào luồng hơi nóng (khô ngay) | Không | Quỹ đạo dự đoán phải dùng khối lượng hiện tại |
| **Trong bọt xà phòng** | Chạm bọt | Bay lên chậm 0.5 đơn vị/giây; chạm gai thì bọt vỡ thay cho bóng bị rò | Chọc (bọt vỡ, bóng xì bình thường) hoặc chạm gai | Vật cản "nổ ngay" làm vỡ bọt và nổ bóng luôn | Bọt chạm trần thì vỡ |
| **Mang đồ** | Chạm chìa khóa hoặc tạ | Đồ treo ở dây, cộng thêm khối lượng | Tới ổ khóa hoặc nút nặng | Nổ thì đồ rơi về chỗ cũ | Đồ không va chạm với gai, để khỏi chồng hai lớp luật |

Phản hồi chung cho mọi trạng thái phụ: viền đổi màu nhẹ, particle riêng (bông tuyết, hơi nóng, giọt nước), và một icon nhỏ cạnh vòng khí khi ngắm. Không phân biệt chỉ bằng màu (xem §10.7).

### 4.6 Skin (chỉ để trang trí)

| Loại | Ví dụ | Ghi chú |
|---|---|---|
| Bóng | Chấm bi, sọc kẹo, dưa hấu, hành tinh, bóng hình mèo, giấy bạc vàng | Skin nào cũng có viền tối và quầng sáng trắng để đủ tương phản (§9.3). Hitbox luôn là hình tròn như cũ |
| Kim | Ghim, nĩa, xương rồng, bút chì, que kem | Đầu kim phải luôn rõ hướng |
| Vệt bay | Bong bóng xà phòng, confetti, sao, nốt nhạc | Tắt được trong Settings để màn hình đỡ rối |
| Hiệu ứng nổ | Confetti, cánh hoa, pháo hoa nhỏ | Mảnh vỡ vẫn tô theo màu skin (`BalloonFeedback` đã làm việc này) |
| Phụ kiện mặt | Kính râm, ria mép, má hồng | Một lớp phụ đặt trên mặt bóng |

---

## 5. Vật cản

### 5.1 Bốn nhóm và ngôn ngữ hình ảnh

Người chơi phải biết một vật **làm gì với mình** chỉ bằng cách nhìn, trước khi chạm vào nó. Vì vậy mọi vật cản thuộc một trong bốn nhóm, mỗi nhóm có hình dạng, màu và chuyển động riêng, giữ nguyên qua mọi thế giới.

| Nhóm | Hậu quả | Hình dạng | Màu | Chuyển động và âm thanh | Ví dụ |
|---|---|---|---|---|---|
| **A. Gây rò** (mềm) | Thêm lỗ rò (`AddLeak`), không thua ngay | Mũi nhọn, góc cạnh | Kim loại bạc xám, có ánh lóe trắng | Lóe sáng định kỳ, tiếng "keng" nhỏ | Đinh ghim, xương rồng, ong |
| **B. Lực** (trường lực) | Đổi quỹ đạo, không gây hại | Đường cong, vệt chảy | Trắng hoặc xanh nhạt, trong mờ | Vệt gió chạy, lá và bụi bay theo | Gió, xoáy, máy hút |
| **C. Đổi trạng thái** | Đổi kích thước, khối lượng, hoặc làm dính | Vùng tròn mềm có hạt bay | Mỗi loại một màu hào quang và một loại hạt | Hạt bay (tuyết, hơi nóng, giọt nước) | Tủ đông, hơi nóng, mạng nhện |
| **D. Nổ ngay** (cứng) | Nổ tức thì (`Pop(reason)`) | Răng cưa, zigzag, sọc cảnh báo | Vàng cam. Không dùng đỏ để khỏi trùng màu bóng mặc định | Luôn chuyển động, phát sáng nhấp nháy, có âm báo trước | Lửa nến, lưỡi cưa, hồ quang điện |

**Luật bắt buộc cho nhóm D:**

- Vật bật tắt theo chu kỳ phải có hoạt ảnh và âm báo trước ít nhất 0.6 giây trước khi bật.
- Không đặt vật nhóm D ở chỗ bóng có thể bay tới trong cú chọc kế tiếp mà người chơi chưa nhìn thấy: nó phải nằm trong khung hình lúc ngắm, hoặc có chỉ báo ngoài màn hình.
- Mỗi màn có tối đa 2 loại vật nhóm D.
- Nhóm D xuất hiện lần đầu ở màn 15 của Thế giới 1, khi người chơi đã quen nhóm A.

### 5.2 Danh sách vật cản

Mọi số trong bảng là giá trị khởi điểm.

| # | Vật cản | Nhóm | Hành vi | Báo trước | Thế giới | Khởi điểm | Code |
|---|---|---|---|---|---|---|---|
| 1 | Đinh ghim, thanh gai | A | Đứng yên | Lóe sáng | 1 | Như `Spike` hiện tại | `Spike` (có sẵn) |
| 2 | Gai di chuyển | A | Đi qua lại theo đường ray | Đường ray vẽ bằng chấm | 1 (từ màn 9) | 1.0 đơn vị/giây (khoảng 25% tốc độ tối đa của bóng) | `Spike` + `PathMover` |
| 3 | Thanh gai xoay | A | Quay quanh trục | Trục có vạch chỉ chiều quay | 2 | 45°/giây | `Spike` + `Rotator` |
| 4 | Gai thò thụt (rào chống chim) | A | Thò ra, thụt vào theo chu kỳ | Rung và lóe 0.5 giây trước khi thò | 3 | Thò 2 giây, thụt 2 giây | `PeriodicHazard` |
| 5 | Xương rồng | A | Đứng yên, lắc nhẹ | Gai to, rõ | 2 | Như gai | `Spike` (đổi skin) |
| 6 | Ong | A | Bóng vào bán kính thì bay đuổi chậm, chích 1 lần rồi về tổ nghỉ 3 giây | Dấu "!" và tiếng vo ve to dần | 2 | Bán kính 2.5, đuổi 1.2 đơn vị/giây | `Bee` (AI nhỏ) |
| 7 | Chim bồ câu | A | Bay tuần tra ngang, mổ thì rò. Xì khí gần chim (< 1.5) làm chim hoảng bay đi 3 giây | Bóng đổ của chim | 3 | Tuần tra 0.8 đơn vị/giây | `Bird`, nghe sự kiện `VentStarted` |
| 8 | Ống bắn phi tiêu | A | Bắn theo chu kỳ dọc một đường thẳng | Tia ngắm mờ 0.6 giây trước khi bắn | 4 | 1 phát mỗi 2.5 giây, tốc độ 4 | `DartLauncher` |
| 9 | Mưa đá | A | Rơi từ mây đen trong một vùng | Mây rung 0.8 giây trước | 5 | 1 đợt mỗi 3 giây | `PeriodicHazard` |
| 10 | Thiên thạch | A | Trôi chậm, nảy | Có vệt đuôi | 6 | 0.5 đơn vị/giây | `Spike` + Rigidbody2D |
| 11 | Gió giật | B | Như quạt nhưng bật tắt theo chu kỳ | Lá bay báo hướng 0.6 giây trước | 2 | Lực 4, bật 2 giây, tắt 2 giây | `Fan` + `Cycle` |
| 12 | Máy hút bụi | B, miệng là D | Hút về phía miệng, càng gần càng mạnh. Chạm miệng thì nổ | Vệt hút và tiếng ù | 1 (cuối), 4 | Lực tối đa 5 | `ForceField` (hướng tâm) + `HardHazard` |
| 13 | Xoáy lốc | B | Kéo vào tâm và xoay vòng | Vòng xoắn | 5 | Lực hướng tâm 3, tiếp tuyến 2 | `ForceField` |
| 14 | Giếng trọng lực (hành tinh) | B | Hút theo 1/r | Vòng quỹ đạo vẽ mờ | 6 | Tùy màn | `ForceField` |
| 15 | Tủ đông, hơi lạnh | C | Làm bóng co lại (§4.5) | Hơi lạnh xanh, bông tuyết | 4 | ×0.7 | `TemperatureZone` |
| 16 | Mạng nhện, mật ong, kẹo cao su | C | Làm bóng dính | Bề mặt bóng loáng, nhỏ giọt | 2 | Không | `StickySurface` |
| 17 | Vòi tưới, mưa | C | Làm bóng ướt và nặng | Giọt nước | 2, 5 | +50% khối lượng trong 4 giây | `WetZone` |
| 18 | Vùng chân không | C | Rò thụ động khi ở bên trong | Hạt tím trôi ra khỏi bóng | 6 | 0.03 khí/giây | `DrainZone` |
| 19 | Nến sinh nhật | D | Lửa đứng yên, chập chờn | Quầng sáng, lửa động | 1 (từ màn 15) | Không | `HardHazard` |
| 20 | Mèo ngủ | D | Bóng vào bán kính thì mèo dậy, 0.6 giây sau vồ | Bong bóng "Zzz"; tai giật khi bóng tới gần mép bán kính | 1 (từ màn 18) | Bán kính 1.8 | `Cat` |
| 21 | Hồ quang điện (dây điện, biển neon) | D | Bật tắt theo chu kỳ | Lách tách và chớp nhỏ 0.6 giây trước | 3 | Bật 1.5 giây, tắt 2 giây | `PeriodicHazard` |
| 22 | Lưỡi cưa | D | Quay, có thể chạy theo ray | Tiếng rít, luôn quay | 4 | Không | `HardHazard` + `PathMover` |
| 23 | Máy dập | D | Nâng lên chậm, dập xuống nhanh | Bóng đổ to dần, rung 0.6 giây | 4 | Nâng 1.5 giây, dập 0.15 giây | `Crusher` |
| 24 | Tia laser | D | Bật tắt hoặc quét; bóng giấy bạc phản xạ được | Tia mờ hiện trước 0.6 giây | 4 | Không | `Laser` |
| 25 | Sét | D | Đánh vào điểm đã đánh dấu | Vòng sáng trên mặt đất 0.8 giây | 5 | Không | `PeriodicHazard` |

**Test chung cho vật cản mới:**

1. Readability: người đứng xem giải thích đúng "vì sao bóng bị rò hoặc bị nổ" ≥ 8/10 lần. Không đạt thì tăng thời gian báo trước thêm 0.3 giây hoặc tăng tương phản. **Không** giảm độ khó bằng con số trước khi sửa phần nhìn.
2. Tốc độ vật cản động giữ ≤ 30% tốc độ tối đa của bóng (khoảng 4 đơn vị/giây), để bài toán là dự đoán chứ không phải đuổi bắt.
3. Quỹ đạo dự đoán chỉ tính tường tĩnh. Nếu playtest cho thấy người chơi bị vật động "đánh úp", thêm một bóng mờ thể hiện vị trí của vật động tại thời điểm cuối quỹ đạo.

---

## 6. Vật hỗ trợ

| # | Vật | Hành vi | Quyết định tạo ra | Thế giới | Khởi điểm | Code |
|---|---|---|---|---|---|---|
| 1 | Quạt (có sẵn). Biến thể: bật tắt bằng nút, quay quét, chạy theo chu kỳ | Đẩy theo trục +X | Di chuyển miễn phí, không tốn khí | 1 | Lực 4 | `Fan` |
| 2 | Luồng hơi nóng | Đẩy lên, bóng nở nhẹ | Đi lên miễn phí nhưng bóng to ra | 3 | Lực đẩy lên 3.5 | `Fan` + `TemperatureZone` |
| 3 | Đệm nảy (gối, bạt nhún, nấm) | Nảy mạnh hơn tường | Cú nảy tường (bank shot) | 1, 2 | Bounciness 1.3 | `PhysicsMaterial2D` + hoạt ảnh bóp dẹt |
| 4 | Gối bông, đám mây | Hấp thụ cú va, bóng đứng yên | Điểm dừng chính xác, chỗ nghỉ để tính toán | 1, 5 | Bounciness 0, friction 1 | `PhysicsMaterial2D` |
| 5 | Pháo giấy (ống bắn) | Bóng chui vào, ống tự xoay chậm, chạm để bắn: bóng bay mạnh theo hướng ống mà không tốn khí | Canh hướng thay vì tốn khí | 1 | Xung lực bằng 1.5 cú chọc, xoay 60°/giây | `Launcher` |
| 6 | Công tắc và cửa (có sẵn) | Chạm công tắc thì cửa mở | Thứ tự đi | 1 | Không | `ButtonSwitch`, `Door` |
| 6a | Công tắc hẹn giờ | Cửa đóng lại sau N giây, đồng hồ hiện trên cửa | Đua với thời gian (thời gian thế giới, nên vẫn dừng khi ngắm) | 3 | 4 giây | `ButtonSwitch` + bộ đếm |
| 6b | Nhiều công tắc | Cửa chỉ mở khi mọi công tắc đều đã bấm | Lập lộ trình | 4 | Không | `DoorLogic` (AND) |
| 6c | Nút nặng | Chỉ mở khi bóng đang mang tạ | Đi lấy tạ trước | 4 | Không | `ButtonSwitch` + điều kiện |
| 7 | Chìa và ổ khóa | Mang chìa trên dây tới ổ | Lộ trình hai chặng, bóng nặng hơn khi mang | 4 | Chìa +0.3 khối lượng | `KeyPickup`, `Lock` |
| 8 | Trạm bơm (gắn tường, đã làm 2026-11-19) | Ở trong vùng thì khí hồi dần; khi không chọc, trạm kéo nhẹ bóng về tâm vùng | Dừng lại bơm, tốn thời gian khi có vật động | Từ 1 (đưa lên sớm vì keo vá không còn hồi khí): 1-19, 1-20 | 0.25 khí/giây, mỗi trạm hồi tối đa 0.5 | `PumpStation` |
| 9 | Cột mốc (checkpoint) | Chạm vào thì lưu vị trí và lượng khí; thua thì về đây | Màn dài đỡ nản | Màn trùm | Không | `Checkpoint` |
| 10 | Ống hơi | Chui vào đầu này, chạy theo đường, ra đầu kia với vận tốc định sẵn | Đi tắt miễn phí | 3 | Tốc độ ra 4 | `Tube` (đi theo waypoint) |
| 11 | Cổng dịch chuyển | Vào A ra B, giữ độ lớn vận tốc, xoay theo hướng cổng ra | Puzzle về hướng | 4, 6 | Hồi chiêu 0.3 giây để khỏi lặp vô tận | `Portal` |
| 12 | Máy thổi bọt xà phòng | Sinh bọt định kỳ | Bay lên an toàn qua vùng gai | 2 | 1 bọt mỗi 4 giây | `BubbleSpawner` |

**Test chung:** ở màn giới thiệu, người chơi phải tự dùng vật hỗ trợ có chủ đích ≥ 8/10 lần. Nếu họ né nó vì tưởng nguy hiểm, sửa hình dạng trước (bo tròn, màu ấm, hoạt ảnh "mời gọi"), chưa đổi số.

**Rủi ro:** trạm bơm hoặc keo vá có `respawnDelay > 0` có thể bị "cày" khí vô hạn, nên mỗi trạm có tổng mức hồi tối đa.

---

## 7. Item và booster

### 7.1 Item nhặt trong màn

| Item | Hiệu ứng | Lý do tồn tại | Khởi điểm | Code |
|---|---|---|---|---|
| Băng keo vá (có sẵn) | Vá mọi lỗ rò, **không** hồi khí (đổi 2026-11-19: keo chỉ vá, bơm khí là việc của trạm bơm) | Cứu vãn sau khi dính gai | Hồi 0 | `PatchPickup` |
| Bình khí nhỏ | Cộng khí, không vá lỗ | Phần thưởng cho đường vòng | +0.18 (đúng 1 cú chọc) | `AirPickup` |
| Sao | Mục tiêu ★ thứ hai | Rủi ro đổi phần thưởng | 1 cái mỗi màn | `StarPickup` |
| Xu | Tiền, đồng thời là "vệt bánh mì" gợi ý đường đi | Motivation và Clarity | 5–15 xu mỗi màn, chỉ tính lần nhặt đầu tiên | `CoinPickup` |
| Băng dính khiên | Khiên chống 1 lần rò | Cho phép đi sát gai | 1 lần | `ShieldPickup` |
| Kẹo bạc hà | Co lạnh 3 giây, kích hoạt khi chạm | Một cách co người khác với vùng lạnh cố định | ×0.7 trong 3 giây | `StatusPickup` |
| Ớt cay | Cú chọc kế tiếp mạnh gấp 1.5 lần, tốn khí như thường | Một cú "siêu chọc" để vượt quãng xa | ×1.5 lực | `StatusPickup` |
| Kim vàng | Cú chọc kế tiếp không tốn khí | Thưởng cho đường khó | 1 lần | `StatusPickup` |
| Chìa khóa, tạ | Mang theo trên dây | Puzzle | Không | `CarryPickup` |

Luật: mỗi lúc chỉ giữ một hiệu ứng "cú chọc kế tiếp" (Ớt cay hoặc Kim vàng); nhặt cái mới thì thay cái cũ. Khi đang có hiệu ứng, kim đổi màu và vòng khí có icon nhỏ để người chơi biết cú tới sẽ khác.

### 7.2 Booster (mang vào màn, kiếm bằng xu hoặc quảng cáo có thưởng)

| Booster | Hiệu ứng | Giới hạn |
|---|---|---|
| Kính ngắm | Quỹ đạo dự đoán dài tới cú nảy thứ hai | Trong 1 màn |
| Bình dự phòng | Lần đầu hết khí thì tự bơm lại 30% thay vì nổ | 1 lần mỗi màn |
| Khiên băng dính | Bắt đầu màn với khiên | Chống 1 lần rò |
| Gợi ý | Bóng ma diễn lời giải: cú chọc kế tiếp, hoặc cả màn | Dùng lời giải đã ghi của designer (§15) |

- Dùng booster vẫn được sao, nhưng không được Vương miện.
- Mỗi loại booster được tặng miễn phí lần đầu, kèm hướng dẫn ngắn, khi người chơi gặp màn khó đầu tiên (màn 9 của Thế giới 1).

---

## 8. Thế giới và level design

### 8.1 Sáu thế giới (hành trình đi lên)

| # | Thế giới | Bối cảnh | Cơ chế mới | Bóng đặc biệt | Màn trùm | Nhạc |
|---|---|---|---|---|---|---|
| 1 | Ngôi nhà sau bữa tiệc | Phòng khách, bếp, gác xép; đích cuối là ô cửa sổ áp mái | Chọc, nảy tường, đinh ghim, keo vá, quạt trần, công tắc đèn và cửa, pháo giấy, gối bông. Cuối thế giới: nến sinh nhật, mèo, máy hút bụi | Không | Thoát khỏi máy hút bụi đuổi theo qua 3 tầng nhà | Piano đồ chơi, ukulele |
| 2 | Khu vườn | Bụi hồng, nhà kính, cây cao | Xương rồng, ong, gió giật (có lá báo hướng), mạng nhện và mật ong, vòi tưới, bọt xà phòng, gai xoay, trạm bơm | Không | Tổ ong khổng lồ | Guitar mộc, tiếng chim |
| 3 | Thành phố | Mái nhà, ống khói, biển hiệu | Hơi nóng đẩy lên, bồ câu, rào chống chim (gai thò thụt), dây điện và neon, ống hơi, công tắc hẹn giờ | Bóng nước, bóng phát sáng (màn đêm) | Leo tòa tháp neon trong cơn giông | Lo-fi jazz |
| 4 | Xưởng đồ chơi | Dây chuyền sản xuất đồ chơi | Lưỡi cưa, máy dập, laser, tủ đông, cổng dịch chuyển, nhiều công tắc, chìa khóa và nút nặng, phi tiêu | Bóng giấy bạc, bóng đôi | Thoát khỏi lò dập cuối dây chuyền | Bộ gõ cơ khí, hộp nhạc |
| 5 | Tầng mây bão | Trên những tầng mây, chim bay thành đàn | Mây đệm, sét, mưa đá, xoáy lốc, gió mạnh | Bóng Heli | Mắt bão | Dàn dây kịch tính |
| 6 | Vũ trụ | Quỹ đạo, vành đai thiên thạch | Không trọng lực, vùng chân không, thiên thạch, giếng trọng lực (bay vòng để lấy đà), cổng dịch chuyển | Bóng phát sáng | Mặt Trăng Bóng Bay | Synth ambient và hộp nhạc |

### 8.2 Cấu trúc 20 màn của mỗi thế giới

Theo nguyên tắc **Cho xem → Thực hành an toàn → Kiểm tra → Phối trộn**:

| Màn | Vai trò | Nội dung |
|---|---|---|
| 1–3 | Giới thiệu | Cơ chế mới đứng riêng, không có cách nào thua vì nó |
| 4–8 | Luyện | Cơ chế mới kết hợp với 1 cơ chế cũ |
| 9–13 | Thử thách | Par chặt hơn, 2 cơ chế cùng lúc |
| 14 | Nghỉ | Dễ, đẹp, nhiều xu. Cho người chơi thở sau chuỗi màn khó |
| 15–19 | Phối trộn | Trộn với cơ chế của thế giới trước, có "cú xoay" (vật cản trở thành công cụ) |
| 20 | Màn trùm | Dài 3 màn hình, có checkpoint, kết thúc bằng cảnh chuyển sang thế giới mới |

**"Cú xoay": dùng vật cản làm công cụ.** Đây là cách làm cho màn sau hay hơn màn trước mà không cần thêm vật mới:

- Gai → "rò chiến thuật": cố tình cọ vào gai để được đẩy giá rẻ (§1.2).
- Quạt → tắt quạt đúng lúc để bóng rơi vào chỗ cần tới.
- Ong → dụ ong đuổi theo vào mạng nhện, ong dính lại và đường được mở.
- Chim → xì khí gần chim để dọa nó bay khỏi lối đi.
- Nóng và lạnh → co hoặc nở cho vừa khe.
- Giếng trọng lực → bay vòng quanh để lấy đà mà không tốn khí.
- Laser và bóng giấy bạc → phản xạ tia vào bộ nhận để mở cửa.

### 8.3 Luật dựng màn (checklist cho designer)

1. Mỗi lần chỉ giới thiệu một thứ mới (README đã ghi).
2. Bề rộng khe theo bảng ở §3.3.
3. Đích phải xuất hiện trong camera giới thiệu. Sao nằm lệch khỏi đường chính không quá 1 cú chọc.
4. Màn rộng tối đa bằng chiều ngang nhìn thấy (§15.4), để người chơi không bao giờ phải đoán tường ở đâu. Màn cao 1–3 màn hình.
5. Designer ghi lại lời giải: số cú chọc dùng làm Par, và bản ghi vị trí dùng làm bóng ma cho Gợi ý.
6. Từ nhóm màn "Luyện" trở đi, mỗi màn có ít nhất 2 lời giải: một cách an toàn tốn khí và một cách khéo đạt Par. Như vậy kỹ năng mới có giá trị.
7. Không có "vùng chết": không có chỗ nào bóng tới được mà không thoát ra được. Nếu có, sửa cho thoát được, hoặc cho thua nhanh. Tránh chết từ từ.
8. Mở màn luôn an toàn: không vật nào chạm tới được bóng trước cú chọc đầu tiên.
9. Kiểm tra mỗi màn trên máy thật với tỉ lệ 9:16, 9:20 và 3:4 (tablet).
10. Par tính cả "rò chiến thuật" (đã chốt ở §20). Riêng các màn giới thiệu gai ở Thế giới 1 không đòi kỹ thuật này để đạt Par.

### 8.4 Tutorial: 10 màn đầu của Thế giới 1

Người chơi không đọc chữ, nên dạy bằng cách cho làm. Chữ, nếu có, chỉ một dòng kèm icon.

| Màn | Dạy gì | Bố cục | Gợi ý trên màn | Đạt khi (khởi điểm) |
|---|---|---|---|---|
| 1-1 | Giữ, kéo, thả | Đích ngay phía trên, 1 cú chọc là tới | Bàn tay mờ nhấp nháy trên bóng. Khi giữ tay: bàn tay ma kéo xuống | ≥ 95% qua màn không cần trợ giúp |
| 1-2 | Bay ngược hướng kim | Đích ở phía trên bên phải | Không có chữ. Quỹ đạo dự đoán là đủ | ≥ 90% |
| 1-3 | Khí có hạn | 2–3 cú chọc. Vòng khí hiện lần đầu, ô sắp tiêu nhấp nháy | "Every poke costs air" (1 dòng có icon) | ≥ 85% |
| 1-4 | Nảy tường | Đích nằm sau một góc, cần nảy 1 lần | Không | ≥ 80% trong tối đa 3 lượt |
| 1-5 | Gai và keo vá | Gai chắn đường chính, keo đặt ngay phía sau | Dòng khí rò hiện rõ | Người chơi nói được "gai làm bóng rò" |
| 1-6 | Sao | Sao đặt lệch khỏi đường đi | Ô Sao trên HUD nhấp nháy | ≥ 60% lấy được Sao trong 3 lượt |
| 1-7 | Quạt | Quạt đưa bóng lên cao miễn phí | Lá giấy bay theo gió | ≥ 80% dùng quạt có chủ đích |
| 1-8 | Công tắc và cửa | Công tắc đèn mở cửa tủ | Dây nối mờ giữa công tắc và cửa | ≥ 80% |
| 1-9 | Par lần đầu, gai di chuyển | Ô Par trên HUD sáng lên | Lần đầu hiện số "Par 2" | Không bắt buộc ★Par |
| 1-10 | Màn nghỉ | Nhiều xu, đẹp | Không | ≥ 90% |

Cử chỉ hủy được dạy theo ngữ cảnh: lần đầu ngón tay đi vào vùng hủy khi đang ngắm thì hiện "Release in the middle to cancel". Chữ trong game là tiếng Anh, xem §10.8.

### 8.5 Đường cong độ khó

Tỉ lệ thắng ngay lượt đầu, theo vai trò màn (khởi điểm, đo bằng analytics ở §14):

| Vai trò màn | Mục tiêu |
|---|---|
| Giới thiệu, Nghỉ | ≥ 85% |
| Luyện | 60–75% |
| Thử thách | 35–55% |
| Phối trộn | 30–50% |
| Màn trùm | 20–35% (có checkpoint) |

- Độ khó đi theo hình răng cưa: sau 2 màn khó là 1 màn dễ.
- Màn nào có tỉ lệ bỏ game sau khi thua cao gấp đôi trung bình của thế giới đó thì xem heatmap vị trí nổ và sửa Clarity trước, chưa đổi số.

---

## 9. Art style

### 9.1 Ba hướng và khuyến nghị

| Hướng | Mô tả | Ưu | Nhược | Chi phí |
|---|---|---|---|---|
| **A. Giấy cắt + bóng vinyl** (khuyến nghị) | Thế giới là các lớp giấy cắt màu mờ, có vân giấy và bóng đổ giữa các lớp. Chỉ có quả bóng (và vật tương tác) là bóng loáng, có highlight như đồ chơi vinyl | Tách hình–nền rất tự nhiên: thứ gì bóng loáng thì là thứ quan trọng. Khác biệt, ấm áp, hợp câu chuyện "đồ trang trí tiệc". Rẻ: hình phẳng + 1 texture vân giấy + bóng đổ | Cần kỷ luật giữ cho nền đừng rối | Vừa |
| B. Đồ chơi bơm hơi toàn bộ | Mọi thứ phồng, bóng loáng như phao bơi | Rất "mềm", hợp trụ cột 3 | Mọi thứ đều bóng nên bóng không còn nổi bật. Shading tốn công | Cao |
| C. Phẳng tối giản | Hình học phẳng, gradient | Rẻ nhất, đọc rất rõ | Dễ lẫn với hàng nghìn game hyper-casual | Thấp |

Chọn **A**: hướng này biến chuyện dễ đọc (Clarity) thành bản sắc (Fit) thay vì phải đánh đổi giữa hai thứ. **Đã chốt (§20): hướng A, toàn bộ asset tạo bằng công cụ tạo ảnh của ChatGPT**, theo pipeline ở §9.6. Nếu bước thử ở §9.6 không đạt, vertical slice tạm dùng C nhưng giữ nguyên luật màu và hình dạng ở §9.2, để sau nâng lên A không phải dựng lại màn.

### 9.2 Art bible

**Ngôn ngữ hình dạng:** nhọn là nguy hiểm, tròn là an toàn. Gai, cưa, sét có góc nhọn và răng cưa. Vật hỗ trợ và item bo tròn. Bóng là hình tròn hoàn hảo. Tường là giấy cắt bo góc nhẹ, mép hơi lởm chởm như cắt tay.

**Thứ bậc độ sáng (value), từ thấp tới cao:**

1. Nền xa: tương phản thấp, giảm độ bão hòa, có thể mờ nhẹ (parallax 2–3 lớp).
2. Tường và sàn: tương phản vừa, có bóng đổ giấy rõ ràng để thấy "đây là thứ va chạm được".
3. Vật tương tác: tương phản cao, viền tối.
4. Bóng, kim, quỹ đạo: tương phản cao nhất và có quầng sáng.

**Tương phản bắt buộc:** bóng, vật cản và đường ngắm phải đạt tỉ lệ tương phản ≥ 3:1 so với nền ngay cạnh. Đây là mức WCAG 2.1 (tiêu chí 1.4.11, Non-text Contrast) yêu cầu cho thành phần đồ họa quan trọng. Đã tính sẵn cho màu bóng mặc định `#E63946`:

| Nền | Tỉ lệ với bóng `#E63946` | Kết luận |
|---|---|---|
| Kem `#F7EFE3` | 3.65:1 | Đạt |
| Chàm đêm `#2A2850` | 3.32:1 | Đạt |
| Navy vũ trụ `#141B3A` | 4.04:1 | Đạt |
| Xám xanh tông trung `#7D8CA3` | 1.22:1 | **Hỏng**: bóng gần như biến mất khi nhìn ảnh grayscale |
| Xanh két tông trung `#2F6F73` | 1.38:1 | **Hỏng** dù khác màu (đỏ và xanh két là hai màu bổ túc) |

→ Luật rút ra: **nền vùng chơi phải rất sáng hoặc rất tối, tránh tông trung**. Tông trung chỉ dùng cho khung và đồ trang trí ở xa vùng bay. Skin có màu sáng (vàng, trắng) dựa vào viền tối để đủ tương phản trên nền sáng; skin màu tối dựa vào quầng sáng trắng trên nền tối. Kiểm tra mỗi màn bằng cách xem ảnh chụp ở chế độ grayscale.

**Viền:** bóng và vật tương tác có viền tối `#2B2B3A` khoảng 3 px (ở 1080 px chiều ngang). Nền không có viền.

**Ánh sáng:** URP 2D đã có trong project. Dùng 1 Global Light và tối đa 2 Light2D mỗi màn (đèn bàn, ô cửa sổ, bóng phát sáng). Có thể dùng Shadow Caster 2D để tạo bóng đổ giữa các lớp giấy, nhưng phải đo hiệu năng trên máy yếu trước (§15.6).

### 9.3 Bảng màu theo thế giới

Màu nền vùng chơi đã chọn để đạt ≥ 3:1 với bóng mặc định. Màu nhấn chỉ dùng cho mảng nhỏ.

| Thế giới | Nền vùng chơi | Phụ | Nhấn | Ghi chú |
|---|---|---|---|---|
| 1 Ngôi nhà | Kem `#F7EFE3`, giấy dán tường bạc hà `#CFE8DF` (3.23:1) | Gỗ `#C49A6C` | Đào `#F2C4B0` (đồ đạc, xa vùng bay) | Ấm, ánh đèn vàng |
| 2 Khu vườn | Trời `#CFEFFF` (3.46:1) | Lá `#8CCB7A`, lá đậm `#4F9A5B` | Hoa `#F7A8C4`, đất `#9C7653` | Sáng, trong |
| 3 Thành phố | Chàm đêm `#2A2850` (3.32:1) | Tím `#4B3F8C` | Cam đèn `#F7A072`, neon `#4DF0FF` | Hoàng hôn rồi đêm |
| 4 Xưởng đồ chơi | Xanh két nhạt `#D5E5E2` (3.20:1) | Máy móc xanh két `#2F6F73` (chỉ làm khung) | Vàng mù tạt `#E8B04B`, xanh đồ chơi `#4A7BD0` | Nhiều kim loại |
| 5 Tầng mây | Sương `#DCE6F0` (3.30:1) | Mây đậm `#4A5568` (chỉ ở mép) | Sét `#FFE66D` | Mây tối đóng khung, không nằm sau vùng bay |
| 6 Vũ trụ | Navy `#141B3A` (4.04:1) | Tinh vân tím `#5B3E8F` | Hồng tinh vân `#E58FC1`, sao `#FFF6D6` | Bóng cần quầng sáng trắng |

**Màu chức năng (giữ nguyên ở mọi thế giới):**

| Vai trò | Màu |
|---|---|
| Bóng mặc định | `#E63946`, highlight `#FFD6D6` |
| Kim | Bạc `#C9CED6`, mũi `#3A3F4B` |
| Quỹ đạo, ngắm | Xanh bạc hà `#1D9E75` (giống mũi tên hiện tại), viền trắng |
| Nhóm A (gây rò) | Bạc `#9AA3AF` + lóe trắng |
| Nhóm B (lực) | `#BFE9FF`, độ trong 40% |
| Nhóm C (trạng thái) | Lạnh `#7FD3FF`, nóng `#FF9F43`, dính `#9BD34A`, ướt `#4AA3FF`, chân không `#9B7BFF` |
| Nhóm D (nổ ngay) | Cam `#FF8A00` + lõi vàng `#FFC83D` + sọc cảnh báo |
| Sao, xu | Vàng kim `#FFD23F` (xoay nhẹ, lấp lánh; phân biệt với nhóm D bằng hình dạng và chuyển động) |

### 9.4 Hoạt ảnh và VFX

- **Bóng:** bóp dẹt khi va (đã có); kéo dài theo hướng bay khi bay nhanh; phập phồng khi thở; dây lắc theo quán tính.
- **Chọc:** kim đâm (đã có), khói phụt trắng tròn mềm, vài mảnh giấy vụn bay.
- **Thời gian dừng:** khi ngắm, viền màn hình hơi tối và hơi lạnh (vignette), particle đứng sững giữa không trung. Khung hình "đóng băng" này vừa đẹp vừa nói rõ "thế giới đang dừng".
- **Nổ:** mảnh cao su cùng màu và confetti giấy, khung hình đứng 0.05 giây (hit-stop, bằng unscaled time), rung camera (đã có).
- **Sao:** lấp lánh, bay về ô sao trên HUD.
- **Về đích:** pháo giấy, bóng nảy vui, các bạn bóng (nếu đã cứu) bay ra chào.
- **Easing:** UI và vật nhỏ dùng ease-out-back (vọt quá một chút rồi về). Vật nặng như máy dập dùng ease-in mạnh.

### 9.5 Quy trình sản xuất asset

| Việc | Đề xuất |
|---|---|
| Công cụ | Công cụ tạo ảnh của ChatGPT theo pipeline ở §9.6, cộng một trình sửa ảnh để tách nền, chỉnh màu và cắt. Vân giấy là 1 texture lặp (cũng tạo bằng AI) phủ bằng shader hoặc lớp multiply |
| Kích thước | Vẽ ở 2× độ phân giải hiển thị trên máy 1080×1920. Thống nhất Pixels Per Unit cho toàn project (ví dụ 256 PPU cho bóng đường kính 0.9 đơn vị) |
| Đóng gói | Sprite Atlas theo thế giới, nén ASTC trên mobile |
| Hoạt ảnh | Mặt bóng: thay sprite hoặc dùng 2D Animation (đã cài). Vật cản: Animator hoặc code tween |
| Mockup | `mockup-gameplay-hud.svg` (màn chơi lúc đang ngắm), `art-bible.svg` (bảng màu, nhóm vật cản, biểu cảm) |

### 9.6 Sản xuất art 100% bằng AI (ChatGPT)

Đã chốt: mọi asset hình ảnh tạo bằng công cụ tạo ảnh mới nhất của ChatGPT, theo hướng A. Với art AI, khó nhất không phải là làm cho đẹp mà là làm cho **đồng bộ**: mỗi lần tạo, ánh sáng, nét viền, độ bão hòa và góc nhìn lại lệch đi một chút. Pipeline dưới đây nhằm giữ đồng bộ.

**Bước 1. Ảnh mốc phong cách.** Tạo 10–20 bản cho cùng một cảnh mẫu (phòng khách Thế giới 1 có bóng, đinh ghim, quạt) rồi chọn 3 ảnh tốt nhất làm "ảnh mốc". Từ đó trở đi, lần tạo nào cũng đính kèm ảnh mốc làm tham chiếu và dùng cùng một khối mô tả phong cách. Đổi ảnh mốc giữa chừng nghĩa là phải kiểm tra lại toàn bộ asset cũ.

**Bước 2. Khối prompt cố định.** Viết bằng tiếng Anh để kết quả ổn định hơn. Mỗi lần chỉ thay dòng `OBJECT`:

```text
STYLE: layered paper cut-out diorama, matte construction paper with subtle paper grain,
hand-cut slightly uneven edges, soft drop shadows between paper layers,
flat orthographic 2D side view, no perspective, soft light from the top-left.
PALETTE: use only #F7EFE3, #CFE8DF, #C49A6C, #F2C4B0 and #2B2B3A.
OBJECT: <mô tả một vật>, single object, centered, whole object visible, empty margin around it.
RULES: transparent background, no text, no letters, no logo, no watermark, no ground shadow.
```

Thêm vào prompt tùy loại asset (dòng `PALETTE` đổi theo bảng màu thế giới ở §9.3):

| Loại | Thêm vào prompt | Ghi chú |
|---|---|---|
| Lớp nền | `wide background layer for parallax, low contrast, slightly desaturated, horizontally tileable` | Tách lớp xa và lớp giữa để làm parallax |
| Vật cản, vật hỗ trợ | `game prop sprite, high contrast`, cộng luật hình dạng của nhóm: nhóm A `sharp pointy silhouette, silver metal`, nhóm D `jagged silhouette, orange with warning stripes` | Viền tối thêm sau bằng shader để dày đều |
| Thân bóng | `glossy vinyl toy balloon, perfect circle, light grey, one soft highlight at the top-left` | Unity tô màu theo skin (bước 4) |
| Bộ icon | `flat sticker icon set, same line weight, 6 icons in a 3x2 grid` | Tạo cả bộ trong một ảnh rồi cắt, đồng bộ hơn tạo lẻ từng cái |

**Bước 3. Hậu kỳ (bắt buộc với mọi ảnh).**

1. Tách nền nếu ảnh chưa trong suốt; cắt sát và chừa lề đều.
2. Kéo màu về đúng mã hex của thế giới, vì AI hay lệch màu. Kiểm tra bằng eyedropper.
3. Không dùng viền do AI vẽ (dày mỏng không đều). Mọi vật tương tác nhận viền `#2B2B3A` từ một shader outline chung trong Unity.
4. Đưa về kích thước theo PPU chung, nén ASTC, gom Sprite Atlas theo thế giới.
5. Kiểm tra: ảnh xám (tương phản ≥ 3:1 với bóng, §9.2), hình dạng đúng nhóm vật cản (nhóm A nhọn, nhóm D răng cưa), và đặt cạnh ảnh mốc xem có lạc tông không.
6. Ghi lại cho từng asset: prompt, ảnh mốc đã dùng, ngày tạo, các bước đã chỉnh. Vừa để tạo lại khi cần, vừa là bằng chứng phần đóng góp của người (xem phần pháp lý bên dưới).

**Bước 4. Những thứ không nên trông vào một lần tạo ảnh đơn lẻ.**

| Thứ | Vì sao | Cách làm (vẫn bằng AI) |
|---|---|---|
| Mặt bóng, 11 biểu cảm | Tạo từng ảnh thì mỗi lần mắt và miệng một kiểu | Tạo cả bộ biểu cảm trong **một** ảnh dạng lưới rồi cắt. Vẫn lệch thì tạo bộ phận rời (mắt, miệng) rồi ghép trong Unity |
| Thân bóng và skin | Skin phải đổi màu mà giữ nguyên hình, và hình phải khớp collider tròn | Một quả bóng xám nhạt và một lớp highlight riêng; Unity tô màu theo skin. Hoa văn (chấm bi, sọc) là lớp phủ riêng |
| Hoạt ảnh | AI không giữ được một vật giống hệt nhau qua nhiều khung hình | Tạo chuyển động bằng code (bóp dẹt đã có), xoay, tween, 2D Animation (đã cài) trên ảnh tĩnh |
| Chữ | Chữ nằm trong ảnh thì khó sửa, khó dịch, có thể sai chính tả | Mọi chữ dùng TextMeshPro; prompt luôn có `no text` |
| Nút UI co giãn | Nút cần 9-slice | Tạo nút trơn không chữ, phần giữa phẳng, rồi đặt viền 9-slice trong Sprite Editor |
| Nền toàn màn hình | Ảnh đầu ra bị giới hạn kích thước và tỉ lệ | Ghép nền từ 2–3 lớp parallax và các mảnh lặp. Kiểm tra kích thước tối đa mà công cụ đang hỗ trợ |
| Collider | Mép ảnh AI lởm chởm, `PolygonCollider2D` tự sinh sẽ có rất nhiều đỉnh | Đặt collider đơn giản bằng tay (box, circle, polygon ít đỉnh) |

**Bước 5. Thử trước khi sản xuất (điều kiện để làm tiếp).** Dùng cùng khối prompt để tạo 5 vật (đinh ghim, quạt, keo vá, Sao, nến) và 1 lớp nền, đặt chung vào một màn rồi chụp màn hình. Đạt khi 5/5 người xem nói "cùng một game" và mọi vật qua kiểm tra ảnh xám. Không đạt thì siết khối prompt (bớt tính từ, thêm ảnh mốc) và thử lại. Thử 2 lần vẫn không đạt thì vertical slice tạm dùng hướng C.

**Thư mục.** Ảnh gốc, ảnh mốc và nhật ký prompt để ở `ArtSource/AI/`, ngoài thư mục `Assets/`, để Unity không import ảnh nặng. Sprite thành phẩm để trong `Assets/PopJet/Art/<thế giới>/`. Không đặt trùng tên `Circle.png`, `Square.png`, `Triangle.png`, `SoftDot.png`, vì `PopJetSceneBuilder` ghi đè các file này mỗi lần chạy.

**Pháp lý và rủi ro** (không phải tư vấn pháp lý; kiểm tra lại trước khi phát hành):

- Theo Điều khoản sử dụng của OpenAI, người dùng sở hữu đầu ra do mình tạo và được dùng cho mục đích thương mại. Đọc lại điều khoản hiện hành.
- Ở Mỹ, Cục Bản quyền (hướng dẫn tháng 3/2023, báo cáo tháng 1/2025) coi phần do AI tạo hoàn toàn là không được bảo hộ bản quyền; phần do người chọn lọc, sắp xếp hoặc chỉnh sửa đáng kể thì có thể được. Hệ quả: art thuần AI có thể bị người khác sao chép. Những thứ bảo vệ được là code, thiết kế màn, tên và thương hiệu, nên cân nhắc đăng ký nhãn hiệu cho tên game.
- Không đưa tên game, nhân vật, thương hiệu hay tên họa sĩ còn sống vào prompt, để không tạo ra thứ giống tác phẩm có bản quyền.
- Một bộ phận người chơi phản ứng tiêu cực với art AI. Giảm rủi ro bằng chất lượng đồng đều và hậu kỳ kỹ, không để lộ lỗi AI như chi tiết vô nghĩa hay chữ méo.
- Nếu sau này phát hành trên Steam, Steam yêu cầu khai báo nội dung tạo bằng AI.

---

## 10. UI/UX

### 10.1 Phong cách UI: "bơm hơi trên giấy"

- **Nút:** hình viên thuốc phồng như được bơm hơi, có vệt sáng phía trên và môi tối phía dưới. Khi nhấn thì xẹp xuống (scale 0.92), thả ra thì bật lại vọt quá một chút. Hoạt ảnh chạy bằng unscaled time.
- **Bảng (panel):** tấm thiệp giấy mép xé, có bóng đổ mềm, được "ghim" bằng chính cây ghim hoặc băng keo trong game.
- **Popup:** "bơm phồng" khi hiện (0 → 1.08 → 1), "xì hơi" khi tắt.
- **Chuyển màn hình:** một quả bóng to phình kín màn hình rồi nổ thành confetti để lộ màn hình mới (0.5 giây). Khi chơi lại thì bỏ qua để giữ nhịp nhanh.
- **Font:** Baloo 2 cho tiêu đề và Nunito cho chữ thường. Game chỉ có tiếng Anh (§20) nên font asset TextMeshPro chỉ cần bộ ký tự Latin cơ bản. Chữ hiển thị trong game ở §10.8.
- **Icon:** nét dày bo tròn, đồng bộ với hình dạng của game.

### 10.2 Luồng màn hình

```
Khởi động lần đầu ──► Màn 1-1 ngay (không qua menu)
                         │ hết màn 1-3
                         ▼
Trang chủ ◄──────────► Bản đồ thế giới ──► Màn chơi ──► Kết quả ──► Màn kế tiếp
  │ Chơi tiếp             │ (cuộn từ dưới lên)   │  ▲            │
  │ Tủ đồ (skin)          │                      │  └── Thua ────┘ (tự chơi lại)
  │ Thử thách ngày        │                      ▼
  │ Cài đặt               │                  Tạm dừng
```

- Lần đầu mở game: vào thẳng màn 1-1, không có menu, không có popup. Trang chủ chỉ hiện sau màn 1-3.
- Trang chủ là một sân chơi nhỏ: Pốp lơ lửng ở giữa, chạm vào để chọc thử cho vui. Động từ chính của game được dạy ngay từ trang chủ.
- Nút lớn nhất là **Chơi tiếp**: vào thẳng màn kế tiếp chưa qua.

### 10.3 HUD trong màn (màn dọc)

```
┌───────────────────────────────┐
│ [II]  1-7   ⚑ ★ ≤2      [↻]  │ ← thanh trên (trong safe area)
│       ▮▮▮▯▯  khí              │ ← thanh khí chia ô, luôn hiện
│                         ↗ đích│ ← chỉ báo ngoài màn hình
│                               │
│            ( •‿• )            │ ← bóng: khi ngắm có vòng khí
│           ·  ·  ·             │ ← quỹ đạo dự đoán
│                               │
│ [👁]                     [💡] │ ← thanh dưới: xem màn, gợi ý/booster
└───────────────────────────────┘
```

| Ưu tiên | Thông tin | Hiển thị |
|---|---|---|
| Quan trọng nhất | Lượng khí, số cú chọc còn lại | Luôn có: kích thước và mặt bóng (trong thế giới), thanh khí chia ô (HUD). Khi ngắm: vòng khí quanh bóng |
| Quan trọng | Mục tiêu sao, số cú chọc so với Par | Liếc là thấy: thanh trên |
| Tham khảo | Xu, tên màn | Chỉ ở màn Kết quả và Tạm dừng. Không đặt trên HUD cho đỡ rối |

- Nút **Chơi lại** ở góc trên bên phải: hơi xa ngón tay một chút, cố ý, vì đây là thao tác phá hủy (mất lượt đang chơi).
- Nút **Tạm dừng** ở góc trên bên trái.
- Thanh dưới có nút Xem màn (camera thu toàn cảnh khi thế giới dừng) và Gợi ý. Chạm lên các nút này **không được** bắt đầu ngắm (§15.5).
- Đầu màn: hiện 3 mục tiêu ở giữa màn hình trong 2 giây, chạm lần đầu thì tắt ngay. Không có popup chặn.

### 10.4 Màn Kết quả, Thua, Tạm dừng

- **Kết quả (thắng):** ba sao lần lượt "phồng" lên, mỗi sao một nốt nhạc cao dần. Hiện "Số cú chọc 3 / Par 2", xu nhận được, Vương miện nếu có. Nút **Tiếp** to ở giữa phía dưới (trong tầm ngón cái), nút phụ: Chơi lại, Bản đồ, và "×2 xu" bằng quảng cáo có thưởng. Chữ thật trong game là tiếng Anh (Next, Retry, Double Coins: §10.8).
- **Thua:** không có màn hình riêng. Lý do thua hiện ngay tại chỗ nổ (§3.5), rồi tự chơi lại. Sau 3 lần thua cùng màn thì có một dải nhỏ mời xem Gợi ý.
- **Tạm dừng:** Tiếp tục, Chơi lại, Bản đồ, Âm thanh/Nhạc/Rung (bật tắt nhanh), Cài đặt.

### 10.5 Bản đồ thế giới

Cuộn dọc từ dưới lên, đúng với hành trình đi lên: nhà → vườn → phố → xưởng → mây → vũ trụ. Mỗi màn là một nút tròn hiện số sao và Vương miện. Pốp đứng ở màn hiện tại, các bạn bóng đã cứu bay quanh. Màn thưởng mở bằng sao nằm trên các nhánh rẽ (§12.2).

### 10.6 Quy tắc mobile

| Quy tắc | Giá trị | Nguồn hoặc cách test |
|---|---|---|
| Vùng chạm tối thiểu | 44×44 pt (iOS), 48×48 dp (Android) | Apple Human Interface Guidelines, Material Design |
| Safe area | Mọi phần HUD nằm trong `Screen.safeArea` (tai thỏ, Dynamic Island, thanh home) | Test trên máy có tai thỏ và trên simulator |
| Canvas Scaler | Scale With Screen Size, tham chiếu 1080×1920, Match = 0 (theo chiều ngang) | Khởi điểm. Test ở 9:16, 9:19.5, 9:21 và 3:4. Nếu nút bị tràn trên tablet thì đổi Match về 0.5 |
| Chơi một tay | Mọi thao tác thường xuyên làm được bằng ngón cái | Test: 5 người cầm máy một tay chơi 3 màn, không ai phải đổi tay |
| Vùng chạm vào bóng | Bóng đầy có đường kính khoảng 309 px trên máy 1080×2400 (camera ortho 3.5). Lúc gần cạn còn khoảng 170 px, vẫn hơn 44 pt (khoảng 132 px ở mật độ 3×) | Tính lại nếu đổi ortho size (§15.4) |

### 10.7 Accessibility (trợ năng)

- **Mù màu:** nhóm vật cản phân biệt bằng hình dạng, chuyển động và icon, không chỉ bằng màu (§5.1). Tùy chọn "Hoa văn nguy hiểm" phủ sọc lên vật nhóm D.
- **Giảm chuyển động:** tắt rung camera, chớp sáng khi nổ, parallax và hiệu ứng chuyển màn bằng bóng nổ.
- **Rung máy:** bật tắt riêng.
- **Thuận tay trái:** đảo vị trí các nút ở thanh dưới.
- **Phương án điều khiển "Kéo tự do"** (§3.2).
- **Không cần phản xạ:** việc thế giới dừng khi ngắm đã là một tính năng trợ năng lớn. Đừng bỏ nó vì lý do "cho khó hơn".

### 10.8 Chữ trong game (tiếng Anh)

Game chỉ có tiếng Anh (§20). Tài liệu này vẫn viết bằng tiếng Việt; bảng dưới là chữ hiển thị trong game. Mọi chữ để trong một bảng chuỗi (ScriptableObject hoặc CSV), không viết cứng trong code và không nhúng vào ảnh, để sau này dịch được.

| Trong tài liệu | Trong game | Ghi chú |
|---|---|---|
| Pốp (nhân vật) | Chưa chốt. Gợi ý: Pip, Bloop | Tránh "Poppy" vì dễ bị liên tưởng tới loạt game kinh dị Poppy Playtime. Kiểm tra trùng tên trên store |
| Mặt Trăng Bóng Bay | Balloon Moon | |
| 6 thế giới | The House, The Garden, The City, Toy Factory, Storm Sky, Outer Space | |
| Chơi tiếp, Tiếp | Play, Next | |
| Chơi lại | Retry | |
| Tạm dừng | Paused | |
| Xem màn | Look | |
| Gợi ý | Hint | |
| Tủ đồ | Wardrobe | |
| Thử thách ngày | Daily Challenge | |
| Cài đặt | Settings | |
| Khí | Air | |
| Cú chọc | Poke | Ô Par trên HUD hiện "1/2" |
| Sao, Par, Vương miện | Star, Par, Crown | |
| Xu | Coins | |
| Thế giới đang dừng | Time stopped | Nhãn khi đang ngắm |
| Hết khí! | Out of air! | Lý do thua |
| Bị cháy! | Burned! | Lý do thua |
| Điện giật! | Zapped! | Lý do thua |
| Mèo vồ! | Cat attack! | Lý do thua |
| Bị cưa! | Sliced! | Lý do thua |
| Bị dập! | Squashed! | Lý do thua |
| Bị hút! | Sucked in! | Lý do thua |
| Kẹt rồi? | Stuck? | |
| Mỗi cú chọc tốn khí | Every poke costs air | Tutorial màn 1-3 |
| Thả ở giữa để hủy | Release in the middle to cancel | Gợi ý theo ngữ cảnh |
| Một chọc ăn ngay | Hole in One | Thành tựu |
| Ba băng | Triple Bank | Thành tựu |
| Tiết kiệm | Thrifty | Thành tựu |
| Ngàn cân treo sợi tóc | By a Thread | Thành tựu |
| Không trầy xước | Not a Scratch | Thành tựu |
| Một hơi, Heli, Gió ngang, Bịt mắt, Bóng mini, Gai độc | Half Tank, Helium, Crosswind, Blindfold, Mini Balloon, Nasty Spikes | Biến tấu của thử thách ngày |
| Xóa quảng cáo | Remove Ads | |
| ×2 xu | Double Coins | |
| Hồi sinh | Continue | |

---

## 11. Âm thanh và rung

### 11.1 Nhạc

- Mỗi thế giới một chủ đề nhạc (xem §8.1), vòng lặp 1–2 phút.
- **"Thời gian dừng" nghe được:** khi bắt đầu ngắm, nhạc đi qua bộ lọc low-pass (cutoff khởi điểm 800 Hz) và nhỏ đi 4 dB trong 0.15 giây. Khi thả tay: tiếng "vút" và bộ lọc mở lại trong 0.2 giây. Làm bằng AudioMixer snapshot, đặt **Update Mode = Unscaled Time** cho mixer, vì lúc ngắm `timeScale = 0` (AudioSource vẫn phát khi timeScale bằng 0, nên nhạc không tự dừng). Test: hỏi người mới "khi bạn giữ tay thì thế giới thế nào?", ≥ 8/10 trả lời "dừng lại".

### 11.2 Hiệu ứng âm thanh

| Sự kiện | Âm | Ghi chú |
|---|---|---|
| Chọc | Tiếng cao su rít và "phụt" | Pitch tăng dần khi khí giảm, từ 1.0 lúc đầy lên 1.3 lúc cạn (khởi điểm). Nghe thôi cũng biết sắp hết khí |
| Lỗ rò | "Xì" mỏng, loop riêng cho từng lỗ | Nhỏ dần theo lực rò |
| Khí còn chưa tới 1 cú chọc | "Cót két" nhẹ mỗi giây | Tắt khi được bơm thêm khí |
| Nảy | "Bộp" (đã có 3 biến thể) | Âm lượng theo lực va (đã có) |
| Vá | Băng keo "rẹt" rồi "chụt" | |
| Sao | Tiếng chuông | Ở màn Kết quả: 3 nốt Đô, Mi, Son |
| Về đích | Kèn tiệc và tiếng confetti | |
| Nổ | "Bụp", mảnh cao su, tiếng "ồ..." hài | Ép nhạc nhỏ đi 0.5 giây (ducking) |
| Báo trước nhóm D | Mỗi vật một âm riêng: lách tách (điện), rít (cưa), ầm ì (máy dập), gừ (mèo) | Phát trước khi kích hoạt ≥ 0.6 giây |
| UI | Nút kêu "pốp" nhỏ, popup kêu "phồng" | |

- Âm nào phát thường xuyên cần ≥ 3 biến thể. Đã có cho chọc, rò, nảy, nổ; cần thêm cho vá, sao và UI.
- Nhóm mixer: Music, SFX thế giới, SFX UI, Ambience. Thứ tự ưu tiên ducking: Nổ > Về đích > Chọc và rò > Nảy > Môi trường.

### 11.3 Rung (haptics)

| Sự kiện | Kiểu rung |
|---|---|
| Bắt đầu ngắm (thế giới dừng) | Tick nhẹ |
| Thả tay (chọc) | Va chạm nhẹ đến vừa |
| Nảy | Va rất nhẹ, mạnh theo lực va, cooldown 0.08 giây (giống âm thanh) |
| Bị rò | Hai tick nhẹ |
| Vá | Rung mềm kiểu "thành công" |
| Nhặt Sao | Tick nhẹ |
| Về đích | Mẫu rung "thành công" |
| Nổ | Va chạm mạnh |

Unity chỉ có sẵn `Handheld.Vibrate()` (một kiểu rung dài duy nhất), nên cần plugin native hoặc thư viện như Nice Vibrations (kiểm tra license hiện tại trước khi dùng). Có công tắc tắt rung trong Settings.

---

## 12. Tiến trình và kinh tế

### 12.1 Tiến trình

- **Mở thế giới mới:** qua màn 20 của thế giới trước. **Không** khóa đường chính bằng sao, vì khóa cứng bằng sao làm người chơi kẹt lại.
- **Sao dùng để mở phần phụ:** mỗi thế giới có 3 màn thưởng, mở ở mốc 20, 40 và 55 sao của thế giới đó (khởi điểm), kèm quà theo mốc sao (skin, xu).
- **Bạn bóng:** cứu ở màn trùm, mở một skin và đi theo Pốp trên bản đồ.
- **Thành tựu** thưởng cho kỹ năng và lối chơi sáng tạo:
  - "Một chọc ăn ngay": qua màn chỉ bằng 1 cú chọc.
  - "Ba băng": 1 cú chọc nảy 3 lần rồi về đích.
  - "Tiết kiệm": về đích với ≥ 60% khí.
  - "Ngàn cân treo sợi tóc": về đích khi còn < 5% khí.
  - "Không trầy xước": qua cả thế giới mà không bị rò lần nào.

### 12.2 Kinh tế (một loại tiền duy nhất: Xu)

| Nguồn xu | Lượng (khởi điểm) |
|---|---|
| Qua màn lần đầu | 20 |
| Mỗi sao, lần đầu đạt | 10 |
| Xu nhặt trong màn | 5–15, chỉ tính lần nhặt đầu tiên |
| Chơi lại màn đã qua | 5 |
| Thử thách ngày | 50 |
| Thành tựu | 25–100 |
| Quảng cáo ×2 ở màn Kết quả | Gấp đôi phần thưởng của màn đó |

| Chỗ tiêu xu | Giá (khởi điểm) |
|---|---|
| Skin thường, hiếm, đặc biệt | 300, 800, 1500 |
| Booster | 100–150 |
| Gợi ý | 100 (hoặc xem 1 quảng cáo) |

**Mục tiêu cân bằng:**

- Mua được skin đầu tiên ngay trong phiên chơi đầu. 10 màn đầu cho khoảng 10 × (20 + 20 + 8) ≈ 480 xu, nhiều hơn 300. Test: ≥ 30% người chơi mua skin trong phiên đầu. Thấp hơn thì đưa skin giá 300 lên đầu Tủ đồ, hoặc hạ giá còn 200.
- Không để xu dư thừa vô nghĩa: sau Thế giới 2, trung vị xu tồn phải thấp hơn giá 2 skin đặc biệt. Nếu vượt, thêm skin hoặc booster để có chỗ tiêu.
- Không bán Sao hay Par bằng xu.

### 12.3 Thử thách ngày và sự kiện

- Mỗi ngày một màn cũ cộng một "biến tấu": **Một hơi** (chỉ có 50% khí), **Heli** (trọng lực ngược), **Gió ngang** (gió thổi khắp màn), **Bịt mắt** (tắt quỹ đạo dự đoán), **Bóng mini** (bắt đầu ở cỡ 0.7), **Gai độc** (rò tốn khí gấp đôi). Cách này nhân lượng nội dung mà không phải dựng màn mới.
- Bảng xếp hạng: ít cú chọc nhất; hòa thì ai nhanh hơn thắng. `PlayTime` đếm bằng unscaled time nên thời gian dừng ngắm cũng bị tính, như vậy là công bằng.
- Sự kiện theo mùa với skin giới hạn, chọn theo thị trường nói tiếng Anh: Halloween (bóng bí ngô), Giáng sinh (bóng người tuyết), Valentine (bóng trái tim), Tết Nguyên đán (bóng lì xì).

---

## 13. Kiếm tiền (F2P, đã chốt)

**Mô hình đã chốt (§20):** miễn phí, quảng cáo có thưởng, một gói "Xóa quảng cáo" và bán skin.

### 13.1 Quảng cáo có thưởng (người chơi tự chọn xem)

| Vị trí | Phần thưởng |
|---|---|
| Màn Kết quả | ×2 xu |
| Hồi sinh (khi đủ điều kiện ở §3.5) | Chơi tiếp 1 lần |
| Gợi ý | 1 gợi ý |
| Hộp quà mỗi ngày | 1 booster |

### 13.2 Quảng cáo xen giữa màn (interstitial), nếu dùng

- Không hiện trước màn 10. Không bao giờ hiện ngay sau khi thua. Tối đa 1 lần sau mỗi 3 màn đã qua và cách nhau ≥ 180 giây (khởi điểm).
- Test: A/B với nhóm không có interstitial. Nếu retention D1 của nhóm có quảng cáo thấp hơn quá 2 điểm phần trăm thì giãn tần suất gấp đôi.
- Mua "Xóa quảng cáo" thì tắt interstitial nhưng giữ quảng cáo có thưởng, vì người chơi vẫn muốn ×2 xu.

### 13.3 Mua trong ứng dụng (IAP)

Xóa quảng cáo (mua một lần), gói skin, gói xu, và gói khởi đầu (xóa quảng cáo + skin + xu) hiện sau màn 20.

### 13.4 Không làm

- Không có hệ thống mạng hay năng lượng.
- Không bán hộp quà ngẫu nhiên. Nếu có vật phẩm ngẫu nhiên mua bằng tiền thật, App Store bắt buộc công bố tỉ lệ trước khi mua (App Store Review Guidelines, mục 3.1.1).
- Không đặt banner quảng cáo trong lúc chơi, vì nó chiếm chỗ ngắm và chỗ chạm.

### 13.5 Checklist tuân thủ (đối chiếu chính sách hiện hành trước khi phát hành)

- Có chính sách quyền riêng tư, link trong game và trên trang store.
- Người dùng EEA và UK: màn hỏi đồng ý qua CMP được Google chứng nhận nếu dùng AdMob.
- iOS: hỏi App Tracking Transparency nếu theo dõi người dùng cho mục đích quảng cáo.
- Độ tuổi: đã chốt từ 13 tuổi (§20). Khai báo nhóm tuổi 13+ trong Play Console và App Store Connect. Art dễ thương vẫn có thể bị xem là thu hút trẻ em: trả lời trung thực câu hỏi về việc này trong Play Console, không làm marketing nhắm vào trẻ em, và đọc lại Families Policy hiện hành trước khi phát hành.
- Android: target API level theo yêu cầu hiện hành của Google Play.

---

## 14. Đo lường

### 14.1 Sự kiện analytics

| Sự kiện | Tham số |
|---|---|
| `level_start` | level_id, attempt, balloon_type, booster |
| `poke` | level_id, attempt, poke_index, air_before, angle (lấy mẫu 10% nếu quá nhiều) |
| `leak` | level_id, hazard_id |
| `level_fail` | level_id, attempt, reason, pokes, air, time, x, y |
| `level_complete` | level_id, attempt, pokes, par, stars, crown, air_left, time |
| `hint_used`, `revive_offered`, `revive_accepted`, `booster_used` | level_id |
| `ad_rewarded`, `ad_interstitial`, `iap_purchase` | placement, product |
| `tutorial_step` | step |
| `skin_equipped` | skin_id |

### 14.2 Chỉ số theo dõi (mục tiêu nội bộ khởi điểm, chỉnh lại sau soft launch)

- ≥ 90% người cài qua được màn 1-3, ≥ 70% qua màn 1-10.
- Mỗi màn: tỉ lệ thắng lượt đầu như bảng §8.5. Với màn thường, trung vị số lượt tới khi thắng ≤ 3.
- Điểm rơi rụng: màn có tỉ lệ thoát game cao bất thường.
- Retention D1 và D7, độ dài phiên, số màn mỗi phiên: so với benchmark của nền tảng analytics bạn dùng, khi đã có dữ liệu.
- Heatmap vị trí nổ (x, y) theo từng màn, để tìm chỗ "chết oan".

Công cụ: Unity Gaming Services Analytics, Firebase hoặc GameAnalytics. Project hiện chỉ có module cũ `com.unity.modules.unityanalytics`; nếu dùng UGS Analytics thì cần cài package `com.unity.services.analytics`.

---

## 15. Kỹ thuật: gắn vào code hiện tại

### 15.1 Nguyên tắc giữ nguyên

- `BalloonController` vẫn là hub. Script khác chỉ gọi API công khai hoặc nghe sự kiện của nó.
- Vật tương tác tìm bóng bằng `GetComponentInParent<BalloonController>()`. Vật dạng trigger tự bật `isTrigger` trong `Awake`.
- Thế giới chạy theo scaled time, phần ngắm và UI chạy theo unscaled time (§3.7).
- Biến dạng chỉ để nhìn đặt trên `Visual`, không bao giờ đặt trên transform gốc (gốc thể hiện lượng khí).
- Đổi tên field `[SerializeField]` thì phải sửa chuỗi tương ứng trong `PopJetSceneBuilder`, hoặc thêm `[FormerlySerializedAs]`.

### 15.2 API mới cho `BalloonController`

| API | Dùng cho |
|---|---|
| `int PokeCount` (đếm số lần `TryVent` thành công) và `event Action<int> Poked` | Par, HUD |
| `float PokesLeft => air / ventAirCost` | Vòng khí, thanh khí |
| `Vector2 Velocity`, `Mass`, cùng các giá trị chỉ đọc `ventThrust`, `ventDuration`, `damping` | Quỹ đạo dự đoán |
| `void Pop(FailReason reason)` và `FailReason LastFailReason` | Vật nhóm D. Hiện `BecomeEmpty()` là private và sự kiện `BecameEmpty` không mang lý do |
| `int ShieldCharges` và `bool TryConsumeShield()` (`AddLeak` kiểm tra khiên trước) | Khiên băng dính |
| `void SetSizeModifier(object source, float mul)`, `ApplyScale()` nhân thêm hệ số này | Nóng, lạnh |
| `void SetStuck(bool value)` | Bề mặt dính |
| `void StopVents()` | Bịt lỗ (thử nghiệm) |
| `void Revive(Vector2 pos, float air)` và `event Action Revived` | Hồi sinh: đặt lại `empty = false`, `rb.simulated = true`; `BalloonFeedback` nghe `Revived` để bật lại renderer mà nó đã tắt lúc nổ |
| `event Action<Vector2> LeakEnded`, hoặc danh sách jet chỉ đọc | Dòng khí rò liên tục |
| `BalloonProfile profile` (ScriptableObject) | Loại bóng |

### 15.3 Script mới

| Script | Cách làm | Ghi chú |
|---|---|---|
| `TrajectoryPreview` | Chạy khi `BalloonInput` đang ngắm | Tích phân Euler bước 0.02 giây: lực jet giảm tuyến tính nhân `thrustScale`, cộng trọng lực, damping và vận tốc hiện tại. Mỗi bước gọi `Physics2D.CircleCast` với bán kính bóng để tìm va chạm đầu tiên. Chạy trong `Update` (thế giới đang dừng nên không cần FixedUpdate) |
| `AirRingUI`, `AirBarUI` | Đọc `Air01`, `PokesLeft` | Unscaled time |
| `BalloonFace` | Nghe sự kiện của hub | Xoay bù `Visual` (§4.1) |
| `BalloonString` | Verlet + `LineRenderer` | Scaled time |
| `LeakStreamFX` | Nghe `LeakAdded`, `LeakEnded` | Particle loop |
| `LevelSession` | Thay thế hoặc mở rộng `LevelManager` | Đếm cú chọc, sao, Par, lý do thua, hồi sinh, checkpoint |
| `StarPickup`, `CoinPickup`, `AirPickup`, `ShieldPickup`, `StatusPickup` | Trigger + `GetComponentInParent` | Giống `PatchPickup` |
| `HardHazard` | Trigger hoặc collision gọi `Pop(reason)` | Vật nhóm D đứng yên |
| `PeriodicHazard` | Chu kỳ Tắt → Báo trước → Bật, chạy bằng scaled time | Điện, gai thò thụt, sét, mưa đá |
| `PathMover`, `Rotator` | Rigidbody2D kinematic, gọi `MovePosition`/`MoveRotation` trong `FixedUpdate` | Tự dừng khi `timeScale = 0` vì FixedUpdate không chạy |
| `ForceField` | Tổng quát hóa `Fan`: lực theo hướng, hướng tâm, hoặc 1/r | Máy hút, xoáy, giếng trọng lực |
| `TemperatureZone`, `StickySurface`, `WetZone`, `DrainZone` | Trigger hoặc collision gọi API trạng thái phụ | |
| `Launcher`, `Portal`, `Tube`, `PumpStation`, `Checkpoint` | Trigger | |
| `Bee`, `Bird`, `Cat` | State machine nhỏ | `Bird` nghe `VentStarted` |
| `TimeController` | Gom các lý do dừng thời gian | §3.7 |
| `SaveSystem` | File JSON trong `Application.persistentDataPath` | Sao, Vương miện, xu, skin, settings. Ghi khi qua màn và khi `OnApplicationPause(true)` |
| `HapticsService`, `AudioDirector` | Dịch vụ dùng chung | Chuyển snapshot mixer khi ngắm |

### 15.4 Camera giữ chiều ngang cố định

```csharp
// Giữ chiều ngang nhìn thấy cố định trên mọi tỉ lệ màn hình.
// visibleWidth khởi điểm = bề rộng trong của màn + 1.0 (mỗi bên chừa 0.5 ngoài tường).
float aspect = (float)Screen.width / Screen.height;
var lens = vcam.Lens;
lens.OrthographicSize = visibleWidth / (2f * aspect);
vcam.Lens = lens;
```

- Lề 0.5 đơn vị ngoài mỗi bức tường là để khi bóng nằm sát tường vẫn còn chỗ đặt ngón tay ngắm ngược ra. Đây chính là lý do README không dùng Confiner. Test: bóng sát tường vẫn ngắm được hướng ra xa tường ≥ 9/10 lần; không đạt thì tăng lề.
- Ví dụ: màn rộng 4.0 thì `visibleWidth` = 5.0, ortho size 4.44 ở máy 9:16, và mỗi đơn vị chiếm 216 px trên màn hình rộng 1080 px. Bóng đầy khoảng 194 px; vùng giữ ở mức khí thấp nhất khoảng 150 px, vẫn trên 44 pt.
- Màn mẫu hiện rộng 5.6. Nếu giữ bề rộng đó thì vùng giữ lúc gần cạn chỉ còn khoảng 114 px, dưới 44 pt. Nên thu màn về khoảng 4.0, hoặc tăng `grabPadding`.

### 15.5 Chặn chạm xuyên UI

```csharp
// using System.Collections.Generic; using UnityEngine.EventSystems;
static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

static bool IsOverUI(Vector2 screenPos)
{
    var es = EventSystem.current;
    if (es == null) return false;
    var data = new PointerEventData(es) { position = screenPos };
    uiHits.Clear();
    es.RaycastAll(data, uiHits);
    return uiHits.Count > 0;
}

// Trong BalloonInput.Update, trước nhánh BeginAim:
// if (PointerHelper.Down && IsOverUI(PointerHelper.ScreenPos)) return;
```

Raycast thủ công như vậy không phụ thuộc pointer id, nên chạy được với cả chuột lẫn cảm ứng. Nhớ tắt `raycastTarget` cho các ảnh trang trí trên HUD, nếu không chúng sẽ chặn cả những lần chạm hợp lệ.

### 15.6 Hiệu năng mobile

- Nhắm 60 fps với `Application.targetFrameRate = 60`.
- Vùng gió to, trong suốt, phủ cả màn gây overdraw. Dùng sprite nhỏ lặp hoặc particle thay cho một quad to.
- Light2D: tối đa 2 đèn mỗi màn. Đo Shadow Caster 2D trên máy yếu trước khi dùng rộng.
- `BalloonFeedback` đang `Instantiate`/`Destroy` mỗi hiệu ứng. Chuyển sang object pool khi số hiệu ứng tăng.
- Physics 2D: giữ fixed timestep mặc định 0.02 giây kèm Interpolate (đã bật). Nếu thấy bóng rung nhẹ ở 60 fps thì thử 1/60 và đo CPU.
- Âm thanh: nhạc dùng Vorbis streaming, SFX ngắn dùng ADPCM hoặc PCM, decompress on load.

### 15.7 Màn chơi dạng dữ liệu

- `LevelDefinition` (ScriptableObject): prefab màn, Par, loại bóng (`BalloonProfile`), thế giới, mốc sao, bản ghi lời giải.
- `LevelCatalog`: danh sách màn theo thứ tự.
- Chỉ một scene gameplay. Chơi lại bằng cách hủy và tạo lại prefab màn, nhanh hơn load lại scene (thời gian vẫn phải đi qua `TimeController`).
- Giữ `PopJetSceneBuilder` làm công cụ prototype. Màn thật dựng tay thành prefab, vì builder ghi đè scene và asset. Thêm field vào hub thì cập nhật builder.
- Bản ghi lời giải cho Gợi ý: ghi vị trí bóng mỗi 0.05 giây trong lượt chơi của designer, rồi phát lại thành bóng ma. Không mô phỏng lại vật lý để phát lại, vì vật lý có thể lệch nhẹ giữa các thiết bị.

### 15.8 Package cần thêm (khi tới giai đoạn tương ứng)

- Chưa cần `com.unity.localization` vì game chỉ có tiếng Anh. Vẫn gom mọi chữ vào một bảng chuỗi (§10.8) để sau này dịch được.
- `com.unity.purchasing` cho IAP, cùng SDK quảng cáo và mediation.
- Analytics (§14).
- `com.unity.splines` nếu ống hơi hoặc đường ray cần đường cong.
- Plugin haptics (§11.3).

---

## 16. Lộ trình

Cỡ việc là ước lượng thô, giả định 1–2 người làm (§20): S = vài ngày, M = 1–3 tuần, L = 1–2 tháng, XL = nhiều tháng.

| Giai đoạn | Nội dung | Cỡ | Điều kiện để qua giai đoạn |
|---|---|---|---|
| 1. Cảm giác lõi | Sửa thiết lập mobile (§1.3), quỹ đạo dự đoán, vòng khí và thanh khí, lý do thua, Retry, phát hiện kẹt, mặt bóng, dây bóng, dòng khí rò, haptics, âm "thời gian dừng". Song song: thử pipeline art AI (§9.6, bước 5) | M | New player test: đoán đúng hướng bay ≥ 8/10; không ai hỏi "sao bóng tự trôi?"; bước thử art AI đạt 5/5 |
| 2. Vertical slice | `LevelSession` (sao, Par, Vương miện), `LevelDefinition` và `LevelCatalog`, lưu game, HUD, Trang chủ, Bản đồ, Kết quả, Tạm dừng, Cài đặt. 20 màn Thế giới 1 với art hướng A tạo bằng AI. Bảng chuỗi tiếng Anh (§10.8). Analytics | L | ≥ 90% qua màn 1-3; trung vị thời gian mỗi màn 20–60 giây; 5 người chơi thử muốn chơi tiếp sau màn 20 |
| 3. Soft launch | Thế giới 2–3 (thêm 40 màn), vật cản động, nóng và lạnh, dính, ong và chim. Tủ đồ, xu, skin. Quảng cáo có thưởng và Xóa quảng cáo. Thử thách ngày. A/B test Bịt lỗ (§3.6). Soft launch ở một nước nói tiếng Anh | L | Funnel từng màn ổn định; đã tìm và sửa các màn rơi rụng |
| 4. Ra mắt và vận hành | Thế giới 4–6, bóng đặc biệt, sự kiện, bảng xếp hạng, cloud save, thành tựu | XL | Theo các chỉ số ở §14 |

### 16.1 Thứ tự việc trong Giai đoạn 1 (việc nhỏ mà có ích nhất làm trước)

**Trạng thái (2026-11-19):** bước 1–7 đã có trong code và scene mẫu (xem `README.md`, bảng script phần "Giai đoạn 1"). Bước 8 (thử art AI) là việc của bạn trong ChatGPT. Chưa playtest trên máy thật.

1. Đặt Portrait, `targetFrameRate = 60`, camera giữ chiều ngang. (S)
2. Nút Retry và chặn chạm xuyên UI. (S)
3. `Pop(reason)` và chữ báo lý do thua. (S)
4. Quỹ đạo dự đoán. (M)
5. Vòng khí và cảnh báo cú chọc cuối. (S)
6. Dòng khí rò liên tục và sự kiện `LeakEnded`. (S)
7. Mặt bóng, dây bóng, haptics, âm "thời gian dừng". (M)
8. Thử pipeline art AI (§9.6, bước 5), làm song song với các bước trên. (S)

### 16.1b Giai đoạn 2 chia nhỏ

| Phần | Nội dung | Trạng thái |
|---|---|---|
| 2.1 Dữ liệu màn | `LevelDefinition`, `LevelCatalog`, prefab màn, `LevelLoader`, một scene gameplay | Xong (2026-11-19). Chơi lại bằng load lại scene thay vì tạo lại prefab |
| 2.2 Sao và Par | `LevelSession`, 3 sao, Vương miện, `StarPickup` | Xong (2026-11-19). Thành tích chỉ trong bộ nhớ tới phần 2.3 |
| 2.3 Lưu game | `SaveSystem` JSON | Xong (2026-11-19): thành tích, màn gần nhất, xu, cài đặt, file .bak, mở khóa màn |
| 2.4 HUD đầy đủ + TextMeshPro | Pause, ô mục tiêu, Xem màn, camera giới thiệu, `TimeController` | Xong (2026-11-19). UI dựng bằng code lúc chạy (`GameScreens`, `UiKit`). Font tạm LiberationSans (chưa có Baloo 2/Nunito) |
| 2.5 Các màn hình | Kết quả, Trang chủ, Bản đồ, Cài đặt | Xong (2026-11-19). Chưa có: ×2 xu bằng quảng cáo, Tủ đồ, Thử thách ngày, chuyển màn bằng bóng nổ, sân chơi Pốp ở Trang chủ |
| 2.6 Vật mới Thế giới 1 | Xu, gai di chuyển, pháo giấy, gối bông, nến, mèo, máy hút bụi | Xong (2026-11-19). Hình vẫn là placeholder |
| 2.7 Dựng 20 màn | Tutorial §8.4, Par và lời giải từng màn | Bản nháp (2026-11-19): 20 màn dựng bằng builder, Par và bố cục **chưa playtest**; màn trùm chưa có checkpoint, chưa có bản ghi lời giải cho Gợi ý |
| 2.8 Analytics + kiểm tra cuối | Sự kiện, build Android, chơi thử máy thật | Chưa |

### 16.2 Nếu thiếu thời gian, cắt theo thứ tự này

Bóng đôi → ống hơi → cổng dịch chuyển → bảng xếp hạng → sự kiện theo mùa → Bịt lỗ (thử nghiệm).

### 16.3 Bãi đỗ ý tưởng (đã cân nhắc nhưng chưa nên làm)

| Ý tưởng | Vì sao chưa |
|---|---|
| Kéo càng xa chọc càng mạnh | Thêm gánh nặng về độ chính xác, làm Par khó hiểu. Có thể làm thành một loại bóng riêng ("bóng có van") |
| Tua lại cú chọc | Phải tua cả trạng thái thế giới (cửa, vật động, item) nên rất tốn công. Xem lại nếu analytics cho thấy nhiều người bỏ game sau khi thua |
| Bóng dài xoay được | Phá luật `freezeRotation`, vật lý phức tạp |
| Trình tạo màn cho người chơi | Phạm vi lớn, để sau khi ra mắt |
| Đua với bóng ma của bạn bè | Hay và rẻ hơn multiplayer thật (chỉ phát lại bản ghi vị trí), nhưng cần backend |
| Chế độ vô tận | Trái trụ cột 1 |

---

## 17. Rủi ro và cách bị lạm dụng

| Rủi ro hoặc cách lạm dụng | Hậu quả | Xử lý |
|---|---|---|
| Rò chiến thuật: cọ vào gai để bay rẻ (§1.2) | Par quá dễ ở màn có gai | Chọn một: giữ làm kỹ năng và tính vào Par, hoặc tăng `leakAirCost` lên khoảng 0.1. Ghi rõ lựa chọn trong `LevelDefinition` |
| Tua từng khung hình bằng cách ngắm rồi hủy | Vật cản động hết khó | Chấp nhận (trụ cột 1). Không làm màn nào đòi phản xạ |
| Cố tình xả khí để lọt khe không định cho qua | Đi tắt, bỏ qua nửa màn | Mọi khe không muốn cho qua phải hẹp hơn 0.45 (§3.3). Test: chọc xả khí trước rồi thử chui qua mọi khe |
| Cày khí ở trạm bơm hoặc keo có hồi lại | Khí vô hạn | Mỗi trạm có tổng mức hồi tối đa. Keo có hồi lại thì giảm `airRefill` |
| Hồi sinh liên tục bằng quảng cáo | Hết thử thách | Tối đa 1 lần mỗi lượt, chỉ khi đủ điều kiện |
| Cày xu bằng cách chơi lại màn dễ | Kinh tế vỡ | Xu trong màn chỉ tính lần nhặt đầu, chơi lại chỉ được 5 xu |
| Lệ thuộc gợi ý | Mất cảm giác tự giải được | Gợi ý mặc định chỉ diễn cú chọc kế tiếp; diễn cả màn tốn gấp đôi |
| Bóng phình lại khi đang nằm trong khe | Kẹt hoặc bị văng ra | Kiểm tra chồng lấn trước khi phình (§4.5) |
| App bị đưa xuống nền khi đang ngắm | Kẹt ở `timeScale = 0` | `TimeController` và `OnApplicationPause`. Test ở §18.2 |
| Chạm lên UI lọt xuống thành ngắm | Chọc nhầm | §15.5 |
| Mèo, ong đuổi bóng ra ngoài khung hình | Thua oan | AI chỉ hoạt động khi đang nằm trong khung hình, hoặc trong bán kính đã hiện ra |
| Art dễ thương thu hút trẻ em | Bị ràng buộc chính sách quảng cáo | Đã chốt từ 13 tuổi; làm theo §13.5 |
| Phạm vi quá lớn | Không bao giờ ra mắt được | Làm vertical slice 20 màn trước. Danh sách cắt ở §16.2 |
| Art AI không đồng bộ giữa các ảnh | Game trông chắp vá, khó đọc | Ảnh mốc, khối prompt cố định, hậu kỳ bắt buộc, viền thêm bằng shader (§9.6) |
| Art thuần AI có thể không được bảo hộ bản quyền | Người khác sao chép được art | Ghi nhật ký prompt và các bước chỉnh sửa; bảo vệ tên game bằng nhãn hiệu (§9.6) |
| Người chơi phản ứng với art AI | Đánh giá thấp trên store | Chất lượng đồng đều, không để lộ lỗi AI (chi tiết vô nghĩa, chữ méo) |

---

## 18. Kịch bản playtest

### 18.1 New player test

- **Chuẩn bị:** người chưa từng thấy game, không giải thích gì, chơi trên máy thật, cầm một tay.
- **Nhiệm vụ:** chơi từ màn 1-1 tới 1-10.
- **Đạt khi:** đoán đúng hướng bay ≥ 8/10 lần trong 2 phút đầu; ≥ 90% người qua màn 1-3 mà không cần giúp; người chơi tự phát hiện cử chỉ hủy, hoặc hiểu ngay khi gợi ý hiện ra.
- **Quan sát:** khi ngắm họ nhìn vào đâu, bóng hay HUD? Có ai hỏi "sao bóng tự trôi?" không? Có ai thử chạm vào nền không?

### 18.2 Stress test

- **Thử:** chạm liên tục lên bóng lúc đang xì; kéo ra khỏi mép màn hình rồi thả; chạm hai ngón; chạm lên nút UI rồi kéo sang bóng; đưa app xuống nền, hoặc có cuộc gọi đến, khi đang ngắm; xoay máy; bóng nằm sát tường và sát góc màn hình; chọc khi khí chỉ còn 0.01.
- **Đạt khi:** không lỗi, không kẹt ở `Aiming`, không kẹt `timeScale = 0`, chạm lên UI không bắt đầu ngắm.

### 18.3 Skill test

- **Cách làm:** so 5 người mới với 5 người đã chơi xong Thế giới 1, trên 5 màn "Thử thách".
- **Đạt khi:** người chơi giỏi dùng ít hơn ít nhất 1 cú chọc (tính theo trung vị), và tỉ lệ đạt ★Par của họ ít nhất gấp đôi người mới. Nếu không có cách biệt, màn đó thiếu lời giải khéo (§8.3, điều 6).

### 18.4 Abuse test

- **Thử:** rò chiến thuật, xả khí để lọt khe, tua từng khung hình, cày trạm bơm, hồi sinh liên tục, cày xu, chạm đích xuyên qua tường mỏng.
- **Đạt khi:** mỗi cách hoặc bị chặn, hoặc được chấp nhận có chủ đích và ghi vào tài liệu của màn.

### 18.5 Readability test

- **Cách làm:** một người đứng xem sau vai người chơi. Sau mỗi lần bóng rò hoặc nổ, hỏi "vừa rồi chuyện gì xảy ra, và vì sao?".
- **Đạt khi:** trả lời đúng ≥ 8/10. Không đạt thì tăng báo trước, tương phản, dòng khí rò. Chưa chỉnh độ khó bằng con số.

---

## 19. Bảng tuning tổng

Đây là các giá trị khởi điểm **mới**. Các giá trị đang có trong code xem ở `README.md` §4.

| Thông số | Khởi điểm | Ở đâu (dự kiến) | Test | Nếu không đạt |
|---|---|---|---|---|
| Độ dài quỹ đạo dự đoán | 0.6 giây, hoặc tới va chạm đầu tiên | `TrajectoryPreview` | Người mới đoán đúng ≥ 8/10 | Khó đoán: 1.0 giây. Quá dễ (★Par > 60%): 0.4 giây |
| Ô khí | 1 ô = `ventAirCost` | `AirRingUI` | Trả lời "còn mấy cú chọc" đúng ≥ 9/10 | Thêm số đếm |
| Lùi camera khi ngắm | Ortho +15%, 0.25 giây | Aim camera | Không ai chóng mặt hay bị lệch hướng | Giảm hoặc tắt |
| Camera giới thiệu màn | 1.5 giây | `LevelSession` | Thấy đích trước lần chọc đầu ≥ 9/10 | 2.0 giây |
| Chiều ngang nhìn thấy | Bề rộng màn + 1.0 | Camera | Ngắm được khi bóng sát tường ≥ 9/10 | Tăng lề |
| Thời gian chờ chơi lại | 1.2 giây, chạm để bỏ qua | `LevelSession` | Trung vị từ lúc nổ tới lúc chạm lại < 3 giây | 0.8 giây |
| Phát hiện kẹt | Khí < 1 cú chọc, tốc độ < 0.05, trong 3 giây | `LevelSession` | Không ai phải chờ quá 5 giây khi bị kẹt | 2 giây |
| Mời xem gợi ý | Sau 3 lần thua cùng màn | `LevelSession` | Tỉ lệ bỏ game sau khi thua giảm | Sau 2 lần |
| Hồi sinh | Còn ≤ 50% khoảng cách tới đích; 1 lần mỗi lượt; 40% khí; bất tử 1 giây | `LevelSession` | Tỉ lệ người nhận; thắng không trở nên quá dễ | Đổi ngưỡng |
| Par | Lời giải tốt nhất của designer | `LevelDefinition` | ★Par chiếm 20–40% số lần qua màn | ±1 |
| Báo trước nhóm D | ≥ 0.6 giây | `PeriodicHazard` | Readability ≥ 8/10 | +0.3 giây |
| Tốc độ vật cản động | ≤ 30% tốc độ tối đa của bóng (khoảng 1.2) | `PathMover` | Không ai thua vì "không kịp nhìn" | Giảm |
| Co lạnh, nở nóng | ×0.7, ×1.15 | `TemperatureZone` | Ở màn giới thiệu, dùng có chủ đích ≥ 8/10 | Đổi hệ số |
| Ướt | +50% khối lượng trong 4 giây | `WetZone` | Người chơi cảm nhận được khác biệt | +25% |
| Đệm nảy | Bounciness 1.3 | Physics material | Có cú nảy tường chủ đích | Thử 1.15–1.5 |
| Pháo giấy | Xung lực 1.5 cú chọc, xoay 60°/giây | `Launcher` | Canh trúng hướng trong ≤ 3 lượt | 45°/giây |
| Trạm bơm | 0.25 khí/giây, tối đa 0.5 mỗi trạm | `PumpStation` | Không cày được khí | Giảm tổng |
| Bình khí nhỏ | +0.18 | `AirPickup` | Người chơi chịu đi vòng để lấy | Tăng lên +0.25 |
| Ong | Bán kính 2.5, 1.2 đơn vị/giây, nghỉ 3 giây | `Bee` | Né được ≥ 6/10 sau 2 lần gặp | Chậm lại |
| Mèo | Bán kính 1.8, vồ sau 0.6 giây | `Cat` | Hiểu bán kính sau 1 lần thua | Hiện vòng bán kính |
| Pitch tiếng chọc | 1.0 → 1.3 khi cạn khí | `BalloonFeedback` | Nhận ra khí thấp bằng tai | Thêm tiếng "cót két" |
| Nhạc khi ngắm | Low-pass 800 Hz, −4 dB | AudioMixer | ≥ 8/10 hiểu "thế giới đang dừng" | 500 Hz |
| Xu | 20 khi qua màn, 10 mỗi sao, 5–15 trong màn | `LevelSession` | ≥ 30% mua skin đầu tiên trong phiên đầu | Hạ giá skin |
| Interstitial | Từ màn 10, tối đa 1 lần mỗi 3 màn, cách nhau ≥ 180 giây, không bao giờ sau khi thua | Ads | D1 không giảm quá 2 điểm % | Giãn ×2 |

---

## 20. Quyết định đã chốt và việc còn mở

Chốt ngày 2026-11-19:

| # | Câu hỏi | Quyết định | Mục bị ảnh hưởng |
|---|---|---|---|
| 1 | Mô hình kinh doanh | F2P: quảng cáo có thưởng, gói Xóa quảng cáo, bán skin | §13 giữ nguyên |
| 2 | Độ tuổi mục tiêu | Từ 13 tuổi | §13.5 |
| 3 | Art | 100% tạo bằng công cụ tạo ảnh mới nhất của ChatGPT, theo hướng A (giấy cắt) | §9.1, §9.5, §9.6 (mới), §17 |
| 4 | Ngôn ngữ | Chỉ tiếng Anh, soft launch ở thị trường nói tiếng Anh | §10.1, §10.8 (mới), §12.3, §15.8 |
| 5 | Bịt lỗ | Để sau vertical slice, chạy A/B ở Giai đoạn 3 | §3.6, §16 |
| 6 | Rò chiến thuật | Giữ làm kỹ năng, Par tính cả cách này | §1.2, §8.3 |

**Còn mở (chưa cần chốt ngay):**

1. Quốc gia soft launch cụ thể. Chọn trước Giai đoạn 3, dựa trên chi phí cài đặt và dữ liệu của mạng quảng cáo bạn dùng.
2. Tên tiếng Anh cho nhân vật (§10.8). Kiểm tra trùng tên trên store cho cả tên nhân vật lẫn tên game "Pop Jet".
3. Kết quả thử pipeline art AI (§9.6, bước 5). Nếu không đạt, vertical slice tạm dùng hướng C.

**Giả định còn lại:**

```
GIẢ ĐỊNH:   Đội 1–2 người; art do AI tạo, người lo viết prompt và hậu kỳ.
TÁC ĐỘNG:   MVP 20 màn; cỡ việc ở §16.
NẾU SAI:    Hậu kỳ art tốn hơn dự kiến thì giảm số vật mới mỗi thế giới và tái dùng asset giữa các màn.
KIỂM CHỨNG: Đo thời gian làm trọn 1 màn (prompt, hậu kỳ, dựng, test), nhân với 20.
```
