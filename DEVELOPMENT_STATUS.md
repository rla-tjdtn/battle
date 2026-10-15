# Blocky Battle Royale — Development Status (V5 controls)

Duckov controls: Space dodge, C holster, Ctrl crouch, F interact.  
BR rules unchanged (last alive wins). See `DUCKOV_CONTROL_REFERENCE.md`.

# Blocky Battle Royale — Development Status (V4)

**Unity:** 6000.6.5f1 · **URP** · **Windows** · Legacy Input  
**Last update:** 2026-10-15

## Verdict

Playable solo BR prototype with Duckov-style quarter-view village.  
V4 additions in progress on top of V2/V3.  
Build: `C:\BBR\Builds\Windows\BlockyBattleRoyale.exe` (`-force-d3d11`)

## Working

- Blocky character, WASD, aim, shoot, crouch/run/prone
- Backpack Lv1–3, guns (7) + melee (fists/crowbar/pan), Tab inventory
- Duckov camera profile + F8 debug
- Mesh terrain village, interiors, doors, roof fade, camera occlusion
- Gameplay LOS hide, zone, AI bots, drop/supply, vehicles (basic)
- Vault (Space), footsteps, destructible crates, 2F houses + stairs (V4)

## Deferred / blocked

| Item | Status |
|---|---|
| Input System / NGO / AI Nav package | Unity 6000.6 CS0619 conflicts |
| Full 1024m map / Steam | Later |
| Duo revive polish / footprints / molotov | Partial / next |

## Regen

```
Unity -batchmode -projectPath C:\BBR -executeMethod BlockyBattleRoyale.EditorTools.BatchBuildEntry.GenerateAndBuildAndQuit -logFile C:\BBR\unity_v4_log.txt
```
