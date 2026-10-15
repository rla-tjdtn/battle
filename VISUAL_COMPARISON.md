# Visual Comparison Log (V3)

| Date | Item | Reference | Unity | Diff | Status |
|---|---|---|---|---|---|
| 2026-10-15 | Char screen H (outdoor) | 9.0% | CameraProfile target 9% + AutoFit | calibrate in Play F8 | ok_tooling |
| 2026-10-15 | Pitch | ~60–70° | 60° (CameraProfile) | in band | ok |
| 2026-10-15 | Yaw | ~45° | 45° fixed | 0 | ok |
| 2026-10-15 | Projection | Perspective | Perspective | 0 | ok |
| 2026-10-15 | Terrain density | High props | Village slice: mesh hills + 12 buildings + 70 trees + props | improved | ok_v3 |
| 2026-10-15 | Roof fade | Smooth hide | RoofFadeController alpha MPB | quality | ok_v3 |
| 2026-10-15 | Interiors | Rooms+furniture | House partition + bed/table/shelf; warehouse crates | improved | ok_v3 |
| 2026-10-15 | Camera occlusion | Canopy fade | CameraOcclusionController | added | ok_v3 |
| 2026-10-15 | Gameplay vs camera sight | Separated | VisibilityController ≠ CameraOcclusion | documented | ok_v3 |

## Scenes

- `GameWorld` — BR + V3 village
- `CameraReferenceScene` — calibration
- `VisibilityTestScene` — LOS hide
- `BuildingTestScene` — roof fade / interiors

## Controls

- F3 camera HUD
- F8 live pitch/yaw/distance/FOV
- Tools > Battle Royale > Camera Calibration
