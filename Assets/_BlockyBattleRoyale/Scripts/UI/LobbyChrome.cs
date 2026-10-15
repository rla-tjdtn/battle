using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BlockyBattleRoyale.Core;
using BlockyBattleRoyale.Networking;

namespace BlockyBattleRoyale.UI
{
    /// <summary>Working lobby controls on the existing menu. Steam friends are not connected.</summary>
    public class LobbyChrome : MonoBehaviour
    {
        static readonly Color[] Colors =
        {
            new Color(0.15f, 0.15f, 0.16f),
            new Color(0.85f, 0.85f, 0.82f),
            new Color(0.45f, 0.45f, 0.46f),
            new Color(0.7f, 0.22f, 0.2f),
            new Color(0.2f, 0.4f, 0.75f),
            new Color(0.25f, 0.5f, 0.3f),
            new Color(0.72f, 0.62f, 0.45f)
        };

        private Text info;
        private Text profile;
        private Text modeLabel;
        private Text slotLabel;
        private Text nameTag;
        private Transform hero;
        private bool dragging;
        private bool searching;
        private string state = "대기";
        private GameObject playPage;
        private GameObject profilePage;
        private GameObject customizePage;

        private void Start()
        {
            var heroGo = GameObject.Find("MenuHero");
            hero = heroGo != null ? heroGo.transform : null;
            if (hero != null)
            {
                hero.position = new Vector3(0f, 0f, 0.2f);
                var weapon = hero.Find("Visual/ArmR/HandR/Weapon");
                if (weapon != null) weapon.gameObject.SetActive(false);
            }
            FrameCamera();
            if (hero != null)
            {
                Vector3 toCam = Camera.main != null ? Camera.main.transform.position - hero.position : Vector3.back;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.01f)
                    hero.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            }
            BuildHangar();
            HideLegacyCenter();
            ApplyCosmetic();
            Build();
            Refresh();
        }

        private void FrameCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.transform.position = new Vector3(0f, 1.02f, -6.6f);
            cam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.92f, 0.2f) - cam.transform.position, Vector3.up);
            cam.fieldOfView = 28f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.09f);
        }

        private void HideLegacyCenter()
        {
            foreach (var name in new[] { "Title", "HostBtn", "JoinBtn", "Address", "Status" })
            {
                var go = GameObject.Find(name);
                if (go != null) go.SetActive(false);
            }
            var solo = GameObject.Find("SoloBtn");
            if (solo != null) solo.SetActive(false);
            var quit = GameObject.Find("QuitBtn");
            if (quit != null) quit.SetActive(false);
        }

        private void Update()
        {
            if (hero == null) return;
            if (nameTag != null && hero != null && Camera.main != null)
            {
                var head = hero.Find("Visual/Head");
                Vector3 world = (head != null ? head.position : hero.position) + Vector3.up * 0.55f;
                Vector3 screen = Camera.main.WorldToScreenPoint(world);
                nameTag.rectTransform.position = screen;
            }
            if (Input.GetMouseButtonDown(0) && Input.mousePosition.x > Screen.width * 0.35f && Input.mousePosition.x < Screen.width * 0.7f)
                dragging = true;
            if (Input.GetMouseButtonUp(0))
                dragging = false;
            if (dragging)
                hero.Rotate(0f, -Input.GetAxis("Mouse X") * 8f, 0f, Space.World);
        }

        private void BuildHangar()
        {
            if (GameObject.Find("LobbyHangar") != null) return;
            var root = new GameObject("LobbyHangar");
            var floorMat = BlockyBattleRoyale.Player.BlockyCharacterBuilder.CreateLitMaterial(new Color(0.16f, 0.17f, 0.16f));
            var wallMat = BlockyBattleRoyale.Player.BlockyCharacterBuilder.CreateLitMaterial(new Color(0.28f, 0.26f, 0.22f));
            var beamMat = BlockyBattleRoyale.Player.BlockyCharacterBuilder.CreateLitMaterial(new Color(0.2f, 0.18f, 0.15f));
            var trimMat = BlockyBattleRoyale.Player.BlockyCharacterBuilder.CreateLitMaterial(new Color(0.42f, 0.32f, 0.18f));

            Cube(root.transform, "Floor", new Vector3(0f, -0.05f, 8f), new Vector3(8f, 0.1f, 28f), floorMat);
            Cube(root.transform, "WallL", new Vector3(-3.4f, 2.2f, 8f), new Vector3(0.25f, 4.4f, 28f), wallMat);
            Cube(root.transform, "WallR", new Vector3(3.4f, 2.2f, 8f), new Vector3(0.25f, 4.4f, 28f), wallMat);
            Cube(root.transform, "Ceiling", new Vector3(0f, 4.3f, 8f), new Vector3(7f, 0.18f, 28f), beamMat);
            Cube(root.transform, "Back", new Vector3(0f, 2.2f, 21.5f), new Vector3(7f, 4.4f, 0.3f), new Color(0.05f, 0.05f, 0.05f));
            for (int i = 0; i < 8; i++)
            {
                float z = -1f + i * 2.8f;
                Cube(root.transform, "RibL" + i, new Vector3(-3.15f, 2.2f, z), new Vector3(0.18f, 4.2f, 0.18f), beamMat);
                Cube(root.transform, "RibR" + i, new Vector3(3.15f, 2.2f, z), new Vector3(0.18f, 4.2f, 0.18f), beamMat);
                Cube(root.transform, "Beam" + i, new Vector3(0f, 4.05f, z), new Vector3(6.6f, 0.16f, 0.16f), trimMat);
            }

            var key = new GameObject("KeyLight");
            key.transform.SetParent(root.transform, false);
            key.transform.position = new Vector3(1.2f, 3.2f, -1.5f);
            key.transform.rotation = Quaternion.Euler(40f, 160f, 0f);
            var light = key.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.92f, 0.78f);
            light.intensity = 4.5f;
            light.range = 16f;
            light.spotAngle = 55f;
            light.shadows = LightShadows.Soft;

            var fill = new GameObject("FillLight");
            fill.transform.SetParent(root.transform, false);
            fill.transform.position = new Vector3(-1.6f, 2.2f, 1f);
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.65f, 0.72f, 0.8f);
            fillLight.intensity = 1.4f;
            fillLight.range = 8f;

            var face = new GameObject("FaceLight");
            face.transform.SetParent(root.transform, false);
            face.transform.position = new Vector3(0f, 1.7f, -2.4f);
            var faceLight = face.AddComponent<Light>();
            faceLight.type = LightType.Point;
            faceLight.color = new Color(1f, 0.94f, 0.82f);
            faceLight.intensity = 2.2f;
            faceLight.range = 7f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.18f, 0.17f);
        }

        private static void Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            Cube(parent, name, pos, scale, BlockyBattleRoyale.Player.BlockyCharacterBuilder.CreateLitMaterial(color));
        }

        private void Build()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var top = Panel(canvas.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 64f), new Color(0.04f, 0.05f, 0.05f, 0.88f));
            Tab(top.transform, "플레이", new Vector2(-620, -32), font, () => Show("play"));
            Tab(top.transform, "전적", new Vector2(-470, -32), font, () => Show("profile"));
            Tab(top.transform, "꾸미기", new Vector2(-310, -32), font, () => Show("custom"));
            Tab(top.transform, "설정", new Vector2(-160, -32), font, () =>
            {
                var panel = GameObject.Find("SettingsPanel");
                if (panel != null) panel.SetActive(true);
            });
            var quit = Tab(top.transform, "종료", new Vector2(860, -32), font, () => Application.Quit());
            quit.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            playPage = Page(canvas.transform, "PlayPage");
            var left = Panel(playPage.transform, "LeftCard", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 28f), new Vector2(340f, 620f), new Color(0.05f, 0.06f, 0.06f, 0.78f));
            modeLabel = Label(left.transform, "ModeName", new Vector2(24, -28), new Vector2(280, 36), "솔로", font, 28, TextAnchor.MiddleLeft);
            slotLabel = Label(left.transform, "Slots", new Vector2(24, -70), new Vector2(280, 28), "1 / 1", font, 18, TextAnchor.MiddleLeft);
            SideButton(left.transform, "솔로", new Vector2(24, -130), font, () => SetMode(1));
            SideButton(left.transform, "듀오", new Vector2(24, -188), font, () => SetMode(2));
            SideButton(left.transform, "스쿼드", new Vector2(24, -246), font, () => SetMode(4));
            info = Label(left.transform, "LobbyInfo", new Vector2(24, -320), new Vector2(290, 70), "", font, 16, TextAnchor.UpperLeft);
            SideButton(left.transform, "매칭 취소", new Vector2(24, -430), font, () =>
            {
                searching = false;
                state = "대기";
                Refresh();
            });
            var play = SideButton(left.transform, "플레이", new Vector2(24, -548), font, () =>
            {
                var menu = FindFirstObjectByType<MainMenuController>();
                if (menu != null) menu.OnSolo();
            });
            var playRt = play.GetComponent<RectTransform>();
            playRt.sizeDelta = new Vector2(292, 64);
            play.GetComponent<Image>().color = new Color(0.78f, 0.69f, 0.38f, 0.95f);
            play.GetComponentInChildren<Text>().color = new Color(0.12f, 0.1f, 0.06f);
            play.GetComponentInChildren<Text>().fontSize = 26;

            var right = Panel(canvas.transform, "RightCard", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -90), new Vector2(300, 220), new Color(0.05f, 0.06f, 0.06f, 0.72f));
            Label(right.transform, "PartyTitle", new Vector2(16, -16), new Vector2(260, 28), "파티", font, 18, TextAnchor.UpperLeft);
            Label(right.transform, "PartyBody", new Vector2(16, -52), new Vector2(260, 140), "친구 초대는 Steam이 연결되지 않아 사용할 수 없습니다.\n이 PC에서 솔로·듀오·스쿼드 연습을 시작할 수 있습니다.", font, 15, TextAnchor.UpperLeft);

            nameTag = Label(canvas.transform, "HeroName", Vector2.zero, new Vector2(320, 32), "PLAYER", font, 20, TextAnchor.MiddleCenter);
            nameTag.rectTransform.pivot = new Vector2(0.5f, 0f);

            profilePage = Page(canvas.transform, "ProfilePage");
            var card = Panel(profilePage.transform, "ProfileCard", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40, 40), new Vector2(360, 220), new Color(0.05f, 0.06f, 0.06f, 0.82f));
            profile = Label(card.transform, "ProfileLine", new Vector2(20, -20), new Vector2(320, 180), "", font, 18, TextAnchor.UpperLeft);
            profilePage.SetActive(false);

            customizePage = Page(canvas.transform, "CustomizePage");
            var custom = Panel(customizePage.transform, "CustomCard", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40, -80), new Vector2(240, 80), new Color(0.05f, 0.06f, 0.06f, 0.82f));
            SideButton(custom.transform, "의상 색 변경", new Vector2(16, -16), font, CycleColor);
            customizePage.SetActive(false);
        }

        private void Show(string page)
        {
            if (playPage != null) playPage.SetActive(page == "play");
            if (profilePage != null) profilePage.SetActive(page == "profile");
            if (customizePage != null) customizePage.SetActive(page == "custom");
        }

        private static GameObject Page(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static Text Label(Transform parent, string name, Vector2 pos, Vector2 size, string text, Font font, int fontSize, TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = new Color(0.93f, 0.91f, 0.84f);
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return t;
        }

        private static Button Tab(Transform parent, string label, Vector2 pos, Font font, UnityEngine.Events.UnityAction action)
        {
            var btn = MakeButton(parent, label + "Tab", pos, label, font, action);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(130, 40);
            btn.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            return btn;
        }

        private static Button SideButton(Transform parent, string label, Vector2 pos, Font font, UnityEngine.Events.UnityAction action)
        {
            var btn = MakeButton(parent, label + "Btn", pos, label, font, action);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(292, 48);
            return btn;
        }

        public void BeginSearch()
        {
            if (searching) return;
            searching = true;
            state = "매칭 중";
            StartCoroutine(Search());
        }

        private IEnumerator Search()
        {
            float t = 0f;
            while (searching && t < 1.1f)
            {
                t += Time.deltaTime;
                state = $"매칭 중  {t:0.0}초";
                Refresh();
                yield return null;
            }
            if (!searching)
            {
                state = "대기";
                Refresh();
                yield break;
            }
            state = "불러오는 중";
            Refresh();
            searching = false;
            if (NetworkBootstrap.Instance != null)
                NetworkBootstrap.Instance.StartOffline();
        }

        private void SetMode(int size)
        {
            GameSettings.SquadSize = size;
            GameSettings.KnockdownEnabled = size > 1;
            GameSettings.Save();
            state = "모드 선택";
            Refresh();
        }

        private void CycleColor()
        {
            GameSettings.Cosmetic = (GameSettings.Cosmetic + 1) % Colors.Length;
            GameSettings.Save();
            ApplyCosmetic();
            Refresh();
        }

        private void ApplyCosmetic()
        {
            if (hero == null) return;
            Color c = Colors[Mathf.Abs(GameSettings.Cosmetic) % Colors.Length];
            foreach (var r in hero.GetComponentsInChildren<Renderer>())
            {
                if (r.gameObject.name == "Torso" || r.gameObject.name == "Pelvis" || r.gameObject.name.StartsWith("Arm") || r.gameObject.name.StartsWith("Leg"))
                {
                    if (r.material.HasProperty("_BaseColor"))
                        r.material.SetColor("_BaseColor", c);
                    r.material.color = c;
                }
            }
        }

        private void Refresh()
        {
            string mode = GameSettings.SquadSize <= 1 ? "솔로" : GameSettings.SquadSize == 2 ? "듀오" : "스쿼드";
            if (modeLabel != null) modeLabel.text = mode;
            if (slotLabel != null) slotLabel.text = $"인원  1 / {Mathf.Clamp(GameSettings.SquadSize, 1, 4)}";
            if (info != null)
                info.text = $"상태  {state}\n캐릭터를 드래그하면 돌아갑니다.";
            if (profile != null)
                profile.text = PlayerProfile.Summary() + (GameSettings.MinimapRotate ? "\n미니맵: 플레이어 방향" : "\n미니맵: 북쪽 고정");
        }

        private static Text MakeText(Transform parent, string name, Vector2 pos, Font font, int size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.92f, 0.9f, 0.82f);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420, 80);
            rt.anchoredPosition = pos;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, Vector2 pos, string label, Font font, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.14f, 0.2f, 0.16f, 0.94f);
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(180, 48);
            rt.anchoredPosition = pos;
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            var tr = text.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            btn.onClick.AddListener(action);
            return btn;
        }
    }
}
