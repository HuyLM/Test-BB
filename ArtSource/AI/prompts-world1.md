# Prompt tạo art Thế giới 1 (Ngôi nhà)

Theo GDD §9.6. Mỗi lần tạo: đính kèm 3 ảnh mốc (sau bước 1), dán **khối cố định** + phần thêm theo loại + dòng `OBJECT`.
Ảnh gốc lưu ở `ArtSource/AI/raw/`, ảnh mốc ở `ArtSource/AI/anchors/`. Sprite thành phẩm vào `Assets/PopJet/Art/World1/`.
Mọi asset: ghi vào bảng nhật ký cuối file (prompt, ảnh mốc, ngày, hậu kỳ).

## Khối cố định

```text
STYLE: layered paper cut-out diorama, matte construction paper with subtle paper grain,
hand-cut slightly uneven edges, soft drop shadows between paper layers,
flat orthographic 2D side view, no perspective, soft light from the top-left.
PALETTE: use only #F7EFE3, #CFE8DF, #C49A6C, #F2C4B0 and #2B2B3A.
OBJECT: <thay dòng này>
RULES: transparent background, no text, no letters, no logo, no watermark, no ground shadow.
```

## Bước 1 — Ảnh mốc phong cách (tạo 10–20 bản, chọn 3)

Riêng prompt này **không** cần nền trong suốt (thay dòng RULES bằng `RULES: no text, no letters, no logo, no watermark.`) và dùng tỉ lệ dọc 9:16.

```text
OBJECT: a cozy living room seen from the side as a vertical 9:16 game level: cream wall with mint wallpaper stripes,
a wooden shelf and a wooden floor, a red glossy vinyl toy balloon (#E63946, perfect circle, one soft highlight at the top-left)
floating in the middle, a silver push pin stuck in the shelf, a small paper desk fan on the floor blowing upward.
Lots of empty space in the middle for the balloon to fly.
```

## Bước 5 — Bộ thử (5 vật + 1 nền). Phải đạt trước khi làm phần còn lại

| # | File đích | Phần thêm | OBJECT |
|---|---|---|---|
| T1 | `Pin.png` | `game prop sprite, high contrast, sharp pointy silhouette, silver metal` | a silver push pin seen from the side, sharp needle pointing down, round flat head; metal colors #9AA3AF and white glint allowed |
| T2 | `Fan.png` | `game prop sprite, high contrast` | a small paper-craft desk fan seen from the side, round cage, short stand, facing up; light blue #BFE9FF accents allowed |
| T3 | `Patch.png` | `game prop sprite, high contrast, rounded friendly silhouette` | a roll of yellow sticky repair tape with a small cross-shaped patch sticker on it; yellow #FFD23F allowed |
| T4 | `Star.png` | `game prop sprite, high contrast, rounded friendly silhouette` | a chunky five-pointed paper star with rounded tips, golden yellow #FFD23F, one small sparkle |
| T5 | `Candle.png` | `game prop sprite, high contrast, jagged silhouette, orange with warning stripes` | a short wax candle with a tall flickering flame, flame orange #FF8A00 with yellow core #FFC83D |
| T6 | `BG_Room_Far.png` | `wide background layer for parallax, low contrast, slightly desaturated, horizontally tileable` | a cream living-room wall with soft mint wallpaper stripes, a faint window and a picture frame, no furniture in the middle |

## Phần còn lại của Thế giới 1 (chỉ làm sau khi bộ thử đạt)

| File đích | Phần thêm | OBJECT | Dùng cho |
|---|---|---|---|
| `Balloon_Body.png` | `glossy vinyl toy balloon, perfect circle, light grey, one soft highlight at the top-left` | a single round balloon body without string, light grey so it can be tinted | `BalloonController` (tô màu theo skin) |
| `Balloon_Faces.png` | `flat sticker icon set, same line weight, 12 faces in a 4x3 grid` | cute simple cartoon faces (only eyes and mouth, dark #2B2B3A on transparent): neutral, happy, excited, scared, dizzy, sleepy, determined, surprised, sad, wink, worried, celebrating | `BalloonFace` |
| `Needle.png` | `game prop sprite, high contrast, sharp pointy silhouette, silver metal` | a long sewing needle, silver #C9CED6 with dark tip #3A3F4B, horizontal | kim ngắm |
| `Coin.png` | `game prop sprite, high contrast, rounded friendly silhouette` | a round paper-craft gold coin with a small balloon emblem embossed, gold #FFD23F | `CoinPickup` |
| `Goal.png` | `game prop sprite, high contrast, rounded friendly silhouette` | a festive paper party garland hoop with small bunting flags, round opening in the middle | `GoalZone` |
| `Cannon.png` | `game prop sprite, high contrast, rounded friendly silhouette` | a paper confetti cannon / party popper tube, pink #F2C4B0 with stripes, pointing up | `Launcher` |
| `Pillow.png` | `game prop sprite, high contrast, rounded friendly silhouette` | a fluffy rectangular cushion pillow, soft peach #F2C4B0 | gối bông (nảy) |
| `Cat_Sleep.png` / `Cat_Awake.png` | `game prop sprite, high contrast` | a round paper-craft house cat sitting, side view, grey fur, (a) eyes closed sleeping / (b) eyes wide open with a raised claw | `Cat` |
| `Vacuum.png` | `game prop sprite, high contrast, jagged silhouette, orange with warning stripes` | a small robot vacuum cleaner nozzle with orange #FF8A00 warning stripes | `ForceField` / `HardHazard` |
| `Door.png` | `game prop sprite, high contrast` | a tall narrow wooden paper door panel #C49A6C, plain, flat middle | `Door` (9-slice) |
| `Button.png` | `game prop sprite, high contrast, rounded friendly silhouette` | a big round push button on a small base, raspberry red | `ButtonSwitch` |
| `Wall_Tile.png` | `game prop sprite, tileable texture, medium contrast` | a flat strip of layered cardboard paper wall, wooden brown #C49A6C, slightly uneven cut edges, plain middle for 9-slice | tường / sàn |
| `BG_Room_Mid.png` | `wide background layer for parallax, low contrast, slightly desaturated, horizontally tileable` | a row of paper-cut furniture silhouettes: sofa, lamp, bookshelf, along the bottom only | nền lớp giữa |
| `Paper_Grain.png` | `seamless tileable texture` | subtle construction paper grain, neutral light grey | phủ multiply |
| `Icons_HUD.png` | `flat sticker icon set, same line weight, 6 icons in a 3x2 grid` | pause, retry, eye (look), home, map pin, gear (settings) | HUD / `GameScreens` |

## Nhật ký

| Ngày | File | Ảnh mốc | Prompt (tóm tắt / link chat) | Hậu kỳ |
|---|---|---|---|---|
| 2026-11-19 | raw/anchor_01.png | — | Bước 1, prompt gốc (chat "Create Room Diorama Image") | chưa |
| 2026-11-19 | raw/anchor_02.png | — | Bước 1, biến thể: cửa sổ, bàn nhỏ, khung tranh | chưa |
| 2026-11-19 | raw/anchor_03.png | — | Bước 1, biến thể dạng màn chơi: 2 kệ zig-zag, 3 đinh ghim, quạt, vòng hoa đích | chưa |
| 2026-11-19 | Pin.png | anchor_01–03 | Bước 5 T1, cùng chat | cắt sát, lề 3%, ≤512px |
| 2026-11-19 | Fan.png | anchor_01–03 | Bước 5 T2 (nhìn chính diện, không mũi tên gió) | như trên |
| 2026-11-19 | Patch.png | anchor_01–03 | Bước 5 T3 | như trên |
| 2026-11-19 | Star.png | anchor_01–03 | Bước 5 T4 | như trên |
| 2026-11-19 | Candle.png | anchor_01–03 | Bước 5 T5 (đế sọc cảnh báo) | như trên |
| 2026-11-19 | Pump.png | anchor_01–03 | Trạm bơm gắn tường: hộp xanh nhạt, đồng hồ áp suất, giá treo trái, vòi chĩa phải | cắt sát, lề 3%, ≤512px; builder lật ngang khi gắn tường phải |
| 2026-11-19 | BG_Room_Far.png | anchor_01–03 | Bước 5 T6, 9:16 không trong suốt | giữ nguyên; cắt thêm BG_Room_Wall.png (dải sọc y 480–1316) để lát |
