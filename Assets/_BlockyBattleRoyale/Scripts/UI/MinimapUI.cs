using UnityEngine;
using UnityEngine.UI;
using BlockyBattleRoyale.BattleRoyale;

namespace BlockyBattleRoyale.UI
{
    public class MinimapUI : MonoBehaviour
    {
        [SerializeField] private RectTransform playerIcon;
        [SerializeField] private RectTransform zoneRing;
        [SerializeField] private RectTransform nextZoneRing;
        [SerializeField] private float worldSize = 220f;
        [SerializeField] private float mapSize = 180f;
        [SerializeField] private float viewMeters = 70f;
        [SerializeField] private GameObject fullMapPanel;
        [SerializeField] private bool fullMapOpen;
        [SerializeField] private float fullZoom = 1f;
        [SerializeField] private Vector2 fullPan;

        private Transform player;
        private RectTransform fullPlayer;
        private RectTransform fullZone;
        private RectTransform fullNext;
        private readonly System.Collections.Generic.List<Vector3> pings = new System.Collections.Generic.List<Vector3>();
        private readonly System.Collections.Generic.List<RectTransform> pingIcons = new System.Collections.Generic.List<RectTransform>();
        private Text pingLabel;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                fullMapOpen = !fullMapOpen;
                if (fullMapPanel != null)
                    fullMapPanel.SetActive(fullMapOpen);
                Core.InputContext.MapOpen = fullMapOpen;
                EnsureFullMapWidgets();
            }

            if (fullMapOpen)
                HandleFullMapInput();

            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
                else return;
            }

            if (playerIcon != null)
            {
                playerIcon.anchoredPosition = Vector2.zero;
                playerIcon.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
            }

            if (ZoneManager.Instance != null)
            {
                UpdateRingRelative(zoneRing, ZoneManager.Instance.Center, ZoneManager.Instance.CurrentRadius);
                UpdateRingRelative(nextZoneRing, ZoneManager.Instance.NextCenter, ZoneManager.Instance.TargetRadius);
            }

            if (fullMapOpen)
                RefreshFullMap();
        }

        private void HandleFullMapInput()
        {
            fullZoom = Mathf.Clamp(fullZoom + Input.mouseScrollDelta.y * 0.08f, 0.6f, 3f);
            if (Input.GetMouseButton(2))
                fullPan += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 18f;
            if (Input.GetKeyDown(KeyCode.Home))
            {
                fullZoom = 1f;
                fullPan = Vector2.zero;
            }
            if (Input.GetMouseButtonDown(0) && fullMapPanel != null)
            {
                Vector3 world = ScreenToWorld(Input.mousePosition);
                if (pings.Count >= 3)
                    pings.RemoveAt(0);
                pings.Add(world);
            }
            if (Input.GetMouseButtonDown(1) && pings.Count > 0)
                pings.RemoveAt(pings.Count - 1);
        }

        private void RefreshFullMap()
        {
            if (fullPlayer != null)
            {
                fullPlayer.anchoredPosition = WorldToFull(player.position);
                fullPlayer.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
            }
            if (ZoneManager.Instance != null)
            {
                PlaceFullRing(fullZone, ZoneManager.Instance.Center, ZoneManager.Instance.CurrentRadius);
                PlaceFullRing(fullNext, ZoneManager.Instance.NextCenter, ZoneManager.Instance.TargetRadius);
            }
            while (pingIcons.Count < pings.Count)
                pingIcons.Add(MakeIcon(fullMapPanel.transform, "Ping", new Color(1f, 0.85f, 0.2f), 12f));
            for (int i = 0; i < pingIcons.Count; i++)
            {
                bool on = i < pings.Count;
                pingIcons[i].gameObject.SetActive(on);
                if (!on) continue;
                pingIcons[i].anchoredPosition = WorldToFull(pings[i]);
            }
            if (pingLabel != null && pings.Count > 0)
            {
                float d = Vector3.Distance(player.position, pings[pings.Count - 1]);
                pingLabel.text = $"핑 {d:0}m   휠 확대  우클릭 드래그 이동  좌클릭 핑  우클릭 삭제";
            }
        }

        private Vector2 WorldToFull(Vector3 world)
        {
            float half = 320f * fullZoom;
            float x = (world.x / (worldSize * 0.5f)) * half + fullPan.x;
            float y = (world.z / (worldSize * 0.5f)) * half + fullPan.y;
            return new Vector2(x, y);
        }

        private Vector3 ScreenToWorld(Vector2 screen)
        {
            if (fullMapPanel == null) return player.position;
            RectTransform rt = fullMapPanel.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, null, out Vector2 local);
            float half = 320f * fullZoom;
            float x = (local.x - fullPan.x) / half * (worldSize * 0.5f);
            float z = (local.y - fullPan.y) / half * (worldSize * 0.5f);
            return new Vector3(x, 0f, z);
        }

        private void PlaceFullRing(RectTransform ring, Vector3 center, float radius)
        {
            if (ring == null) return;
            ring.anchoredPosition = WorldToFull(center);
            float size = (radius / (worldSize * 0.5f)) * 640f * fullZoom;
            ring.sizeDelta = new Vector2(size, size);
        }

        private void EnsureFullMapWidgets()
        {
            if (fullMapPanel == null || fullPlayer != null) return;
            fullPlayer = MakeIcon(fullMapPanel.transform, "FullPlayer", new Color(0.3f, 0.8f, 1f), 14f);
            fullZone = MakeIcon(fullMapPanel.transform, "FullZone", new Color(0.2f, 0.55f, 1f, 0.25f), 80f);
            fullNext = MakeIcon(fullMapPanel.transform, "FullNext", new Color(1f, 1f, 1f, 0.18f), 60f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("MapHelp");
            go.transform.SetParent(fullMapPanel.transform, false);
            pingLabel = go.AddComponent<Text>();
            pingLabel.font = font;
            pingLabel.fontSize = 16;
            pingLabel.alignment = TextAnchor.LowerCenter;
            pingLabel.color = Color.white;
            var rt = pingLabel.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(640, 36);
            AddRegion(fullMapPanel.transform, "마을", new Vector3(20, 0, 16), font);
            AddRegion(fullMapPanel.transform, "숲", new Vector3(-62, 0, 48), font);
            AddRegion(fullMapPanel.transform, "창고", new Vector3(-24, 0, 16), font);
        }

        private void AddRegion(Transform parent, string name, Vector3 world, Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.9f, 0.88f, 0.75f);
            t.text = name;
            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(80, 24);
            rt.anchoredPosition = WorldToFull(world);
        }

        private static RectTransform MakeIcon(Transform parent, string name, Color color, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(size, size);
            return rt;
        }

        private void UpdateRingRelative(RectTransform ring, Vector3 center, float radius)
        {
            if (ring == null || player == null) return;
            Vector3 d = center - player.position;
            float x = (d.x / viewMeters) * mapSize;
            float y = (d.z / viewMeters) * mapSize;
            if (Core.GameSettings.MinimapRotate)
            {
                float yaw = -player.eulerAngles.y * Mathf.Deg2Rad;
                float rx = x * Mathf.Cos(yaw) - y * Mathf.Sin(yaw);
                float ry = x * Mathf.Sin(yaw) + y * Mathf.Cos(yaw);
                x = rx;
                y = ry;
            }
            ring.anchoredPosition = new Vector2(x, y);
            float size = (radius * 2f / viewMeters) * mapSize;
            ring.sizeDelta = new Vector2(size, size);
        }

        private Vector2 WorldToMap(Vector3 world)
        {
            float x = (world.x / worldSize) * mapSize;
            float y = (world.z / worldSize) * mapSize;
            return new Vector2(x, y);
        }

        private void UpdateRing(RectTransform ring, Vector3 center, float radius)
        {
            if (ring == null)
                return;
            ring.anchoredPosition = WorldToMap(center);
            float size = (radius * 2f / worldSize) * mapSize;
            ring.sizeDelta = new Vector2(size, size);
        }

        public void Bind(RectTransform playerRt, RectTransform zone, RectTransform next, GameObject fullMap)
        {
            playerIcon = playerRt;
            zoneRing = zone;
            nextZoneRing = next;
            fullMapPanel = fullMap;
            if (fullMapPanel != null)
                fullMapPanel.SetActive(false);
        }
    }
}
