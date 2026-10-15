# Implementation Checklist (V3 Visual + V2 BR)

Legend: `[x]` done · `[~]` partial · `[ ]` not done · `[!]` needs Play Mode verify

## V3 PHASE 1 — Camera
- [x] Reference analysis docs + JSON
- [x] CameraProfile SO
- [x] QuarterViewCamera ApplyProfile
- [x] CameraDebugPanel (F8)
- [x] CameraReferenceCalibration
- [x] CameraReferenceScene
- [!] Human Play Mode screenshot vs Duckov outdoor 9% char H

## V3 PHASE 2–3 — World / Buildings
- [x] MeshTerrainBuilder
- [x] ProceduralBuildingFactory (house/warehouse/garage + interior)
- [x] VegetationFactory (5 trees, bush, rock, fence, props)
- [x] Village slice wired into GameWorld
- [x] Warm lighting
- [!] Visual density QA in Play Mode

## V3 PHASE 4 — Occlusion / Visibility
- [x] RoofFadeController alpha
- [x] BuildingInteriorVolume
- [x] CameraOcclusionController + marker
- [x] VisibilityController gameplay split
- [x] VisibilityTestScene / BuildingTestScene

## V2 BR (retained)
- [x] Solo menu → GameWorld
- [x] Combat / zone / AI / drop system code
- [x] Windows Mono build
- [ ] NGO online MP
