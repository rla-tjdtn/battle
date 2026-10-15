# Reference Analysis — Escape from Duckov style (V3)

측정일: 2026-10-15  
원본 해상도: **1024 × 576** (첨부 3장 모두)

정규화: `N = pixel / dimension`  
공식 카메라 수치(FOV/거리)는 미공개 → **화면 비율만** 기록. 추측 수치 없음.

## Image A — Outdoor container yard

역할: `Reference_Camera`, `Reference_Environment`, `Reference_Lighting`, `Reference_UI`

| 항목 | Pixel (approx) | Normalized | Notes |
|---|---|---|---|
| Character height | ~52 px | **0.090** (9.0% screen H) | Blocky figure near center |
| Character center X | ~512 | 0.50 | Near horizontal center |
| Character center Y | ~310 | 0.54 | Slightly below geometric center |
| Container height vs char | ~2.2× char | — | Door ≈ slightly taller than char |
| Car length vs char | ~2.5–3× char H | — | Low-poly sedans |
| Projection | — | Perspective | Clear foreshortening |
| Pitch (visual) | — | ~60–70° down | Tops + sides of props visible |
| Yaw (visual) | — | ~45° | Diagonal grid feel |
| Lighting | — | Warm afternoon | Soft long shadows |

**미확인:** 엔진 FOV, orthographic 여부(시각상 Perspective), 정확한 pitch 각도.

## Image B — House interior

역할: `Reference_Indoor`, `Reference_Buildings`, `Reference_Visibility`

| 항목 | Pixel (approx) | Normalized | Notes |
|---|---|---|---|
| Character height | ~38 px | **0.066** (6.6% screen H) | Smaller on screen (wider framing / indoor) |
| Wall height vs char | ~2× | — | Interior walls clear |
| Roof | — | Hidden/removed | Quarter-view maintained |
| Furniture | — | Present | Beds, desks, plants |
| Interaction | — | F prompt | Visible in scene |

**미확인:** Roof fade implementation (hide vs alpha).

## Image C — Forest campsite

역할: `Reference_Forest`, `Reference_CharacterScale`, `Reference_Lighting`

| 항목 | Pixel (approx) | Normalized | Notes |
|---|---|---|---|
| Character height | ~48 px | **0.083** (8.3% screen H) | Near campfire |
| Tree height vs char | ~4–6× | — | Layered pine silhouettes |
| Truck vs char | ~2.5× H | — | Pickup scale |
| Props | — | Dense | Tent, barrels, sandbags, tires |

## Target bands for Unity

| Metric | Target | Tolerance |
|---|---|---|
| Char screen height (outdoor) | **8–10%** | ±5% relative |
| Char screen height (indoor) | **6–8%** | ±5% relative |
| Pitch | start 60°, explore 55–70 | visual match |
| Yaw | 45° | fixed (no orbit with aim) |
| FOV | start 40°, explore 35–50 | visual match |
| Projection | Perspective | Ortho debug only |

## Unity baseline (pre-V3)

| Setting | Value |
|---|---|
| Pitch | 60 |
| Yaw | 45 |
| Distance | 22 |
| FOV | 40 |
| Est. char screen ratio | ~6–10% (code estimate) |
