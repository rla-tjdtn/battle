# Blocky Battle Royale

3D 쿼터뷰 배틀로얄 (Unity **6000.6.5f1** / URP / Windows)

## 상태

- Unity Hub + Unity 6000.6.5f1 설치됨
- Personal 라이선스 활성화됨
- **Full Game 씬 생성 완료** (MainMenu + GameWorld)
- URP Pipeline Asset 할당 완료
- ScriptableObject 무기/아이템/존 설정 생성 완료

## 실행 방법

1. **Unity Hub** → Open
2. 프로젝트 경로 (둘 중 하나):
   - `C:\BBR` (권장, 한글 경로 회피용 정션)
   - 또는 `OneDrive\바탕 화면\배틀게임`
3. 씬 `Assets/_BlockyBattleRoyale/Scenes/MainMenu` 열기
4. **Play** → `혼자 플레이`

직접 전투만 테스트하려면 `GameWorld` 씬을 열고 Play.

## 조작

| 키 | 동작 |
|---|---|
| WASD / Shift / C | 이동 / 달리기 / 앉기 |
| Mouse / LMB / RMB | 조준 / 사격 / ADS |
| R / E / F | 재장전 / 루팅 / 차량 |
| Tab / M / G | 인벤 / 맵 / 수류탄 |
| 1 / 2 / 4 | 무기1 / 무기2 / 회복 |

## 생성된 씬

- `MainMenu.unity` — 솔로 / 호스트 / 참가 / 종료
- `GameWorld.unity` — 맵·봇7·자기장·루팅·HUD·차량

## 참고

- Unity 6000.6 패키지 호환 문제로 Input System / NGO / AI Navigation 패키지는 제외.
  조작은 Legacy Input, 멀티는 오프라인·Host 폴백, AI는 NavMeshAgent(모듈) + 폴백 이동.
- 배치 생성: `C:\BBR` 경로로 Unity `-executeMethod BlockyBattleRoyale.EditorTools.BatchBuildEntry.GenerateAndQuit`
