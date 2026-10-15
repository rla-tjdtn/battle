using UnityEngine;
using BlockyBattleRoyale.Occlusion;
using BlockyBattleRoyale.World;

namespace BlockyBattleRoyale.Buildings
{
    public enum BuildingKind
    {
        HouseSmall,
        HouseTwoFloor,
        Warehouse,
        Garage
    }

    public static class ProceduralBuildingFactory
    {
        public static GameObject Build(Transform parent, BuildingKind kind, Vector3 position, Material wallMat, Material roofMat, Material floorMat, Material woodMat)
        {
            Vector3 size = kind switch
            {
                BuildingKind.Warehouse => new Vector3(14f, 4.2f, 10f),
                BuildingKind.Garage => new Vector3(7f, 3f, 6f),
                BuildingKind.HouseTwoFloor => new Vector3(11f, 6.2f, 9f),
                _ => new Vector3(10f, 3f, 8f)
            };

            var root = new GameObject(kind.ToString());
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            float w = size.x, h = size.y, d = size.z;
            float t = 0.25f;

            // Floor
            var floor = Box(root.transform, "Floor", new Vector3(0, 0.05f, 0), new Vector3(w - 0.1f, 0.1f, d - 0.1f), floorMat, true);

            // Walls with door gap on -Z
            float doorW = 1.8f;
            float doorH = 2.3f;
            float side = (w - doorW) * 0.5f;

            Box(root.transform, "Wall_N", new Vector3(0, h * 0.5f, d * 0.5f - t * 0.5f), new Vector3(w, h, t), wallMat, true);
            Box(root.transform, "Wall_E", new Vector3(w * 0.5f - t * 0.5f, h * 0.5f, 0), new Vector3(t, h, d), wallMat, true);
            Box(root.transform, "Wall_W", new Vector3(-w * 0.5f + t * 0.5f, h * 0.5f, 0), new Vector3(t, h, d), wallMat, true);

            // South wall split for door
            Box(root.transform, "Wall_S_L", new Vector3(-(doorW * 0.5f + side * 0.5f), h * 0.5f, -d * 0.5f + t * 0.5f), new Vector3(side, h, t), wallMat, true);
            Box(root.transform, "Wall_S_R", new Vector3((doorW * 0.5f + side * 0.5f), h * 0.5f, -d * 0.5f + t * 0.5f), new Vector3(side, h, t), wallMat, true);
            Box(root.transform, "Wall_S_Top", new Vector3(0, doorH + (h - doorH) * 0.5f, -d * 0.5f + t * 0.5f), new Vector3(doorW, h - doorH, t), wallMat, true);

            AddWindow(root.transform, new Vector3(-w * 0.25f, 1.6f, d * 0.5f - t), woodMat);
            AddWindow(root.transform, new Vector3(w * 0.25f, 1.6f, d * 0.5f - t), woodMat);
            if (kind == BuildingKind.HouseTwoFloor)
            {
                AddWindow(root.transform, new Vector3(-w * 0.22f, 4.4f, d * 0.5f - t), woodMat);
                AddWindow(root.transform, new Vector3(w * 0.22f, 4.4f, d * 0.5f - t), woodMat);
            }

            // Base trim and porch step (does not block the door)
            Box(root.transform, "Trim", new Vector3(0, 0.18f, 0), new Vector3(w + 0.08f, 0.22f, d + 0.08f), floorMat, false);
            Box(root.transform, "Porch", new Vector3(0, 0.04f, -d * 0.5f - 0.9f), new Vector3(2.2f, 0.08f, 0.8f), floorMat, false);

            var hinge = new GameObject("DoorHinge");
            hinge.transform.SetParent(root.transform, false);
            hinge.transform.localPosition = new Vector3(-doorW * 0.5f, 0f, -d * 0.5f + t * 0.5f);
            var door = Box(hinge.transform, "Door", new Vector3(doorW * 0.46f, doorH * 0.5f, 0f), new Vector3(doorW * 0.9f, doorH, 0.1f), woodMat, true);
            var doorComp = door.AddComponent<DoorInteractable>();
            doorComp.Configure(hinge.transform, -105f);

            // Interior partition + props
            if (kind == BuildingKind.HouseSmall)
            {
                Box(root.transform, "Partition", new Vector3(0.5f, h * 0.45f, 0.2f), new Vector3(t, h * 0.9f, d * 0.55f), wallMat, true);
                Box(root.transform, "Bed", new Vector3(-2.2f, 0.35f, 1.5f), new Vector3(2.0f, 0.5f, 1.2f), woodMat, true);
                Box(root.transform, "Pillow", new Vector3(-2.6f, 0.7f, 1.5f), new Vector3(0.45f, 0.22f, 0.7f), floorMat, false);
                Box(root.transform, "Table", new Vector3(2.0f, 0.45f, -0.4f), new Vector3(1.3f, 0.7f, 0.8f), woodMat, true);
                Box(root.transform, "Chair", new Vector3(2.0f, 0.4f, -1.5f), new Vector3(0.45f, 0.8f, 0.45f), woodMat, true);
                Box(root.transform, "Shelf", new Vector3(w * 0.35f, 1.2f, d * 0.28f), new Vector3(0.35f, 1.6f, 1.4f), woodMat, true);
                Box(root.transform, "Fridge", new Vector3(-w * 0.32f, 0.9f, -1.2f), new Vector3(0.7f, 1.6f, 0.7f), floorMat, true);
            }
            else if (kind == BuildingKind.HouseTwoFloor)
            {
                // Mid floor slab
                float mid = 3.1f;
                Box(root.transform, "Floor2", new Vector3(0, mid, 0), new Vector3(w - 0.2f, 0.15f, d - 0.2f), floorMat, true);
                // Stair steps along +X wall
                for (int s = 0; s < 8; s++)
                {
                    float sy = 0.2f + s * 0.38f;
                    float sz = -d * 0.35f + s * 0.35f;
                    Box(root.transform, "Step" + s, new Vector3(w * 0.32f, sy, sz), new Vector3(1.6f, 0.18f, 0.5f), woodMat, true);
                }
                Box(root.transform, "Bed2", new Vector3(-2f, mid + 0.35f, 1.2f), new Vector3(2f, 0.5f, 1.2f), woodMat, true);
                Box(root.transform, "Rail", new Vector3(-w * 0.1f, mid + 0.5f, 0), new Vector3(0.12f, 1f, d * 0.5f), woodMat, true);
            }
            else if (kind == BuildingKind.Warehouse)
            {
                for (int i = 0; i < 4; i++)
                    Box(root.transform, "Crate" + i, new Vector3(-3f + i * 2f, 0.6f, 2f), new Vector3(1.2f, 1.2f, 1.2f), woodMat, true);
                Box(root.transform, "ShelfRow", new Vector3(4f, 1.5f, 0f), new Vector3(0.5f, 2.8f, 7f), wallMat, true);
            }
            else
            {
                Box(root.transform, "Workbench", new Vector3(0f, 0.5f, 1.2f), new Vector3(2.5f, 0.8f, 1.0f), woodMat, true);
            }

            // Roof visual (separate from collision slab slightly)
            var roof = Box(root.transform, "RoofVisual", new Vector3(0, h + 0.15f, 0), new Vector3(w + 0.4f, 0.28f, d + 0.4f), roofMat, false);
            var roofRend = roof.GetComponent<Renderer>();
            var roofMarker = roof.AddComponent<CameraOccluderMarker>();
            roofMarker.Configure(roofRend);

            // Thin roof collision (stays even when faded) — collider only
            var roofCol = new GameObject("RoofCollision");
            roofCol.transform.SetParent(root.transform, false);
            roofCol.transform.localPosition = new Vector3(0, h + 0.15f, 0);
            var rbc = roofCol.AddComponent<BoxCollider>();
            rbc.size = new Vector3(w + 0.4f, 0.2f, d + 0.4f);

            // Interior volume
            var volume = new GameObject("InteriorVolume");
            volume.transform.SetParent(root.transform, false);
            volume.transform.localPosition = new Vector3(0, h * 0.5f, 0);
            var box = volume.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(w - 0.6f, h - 0.2f, d - 0.6f);

            var chimney = Box(root.transform, "Chimney", new Vector3(w * 0.28f, h + 0.7f, d * 0.15f), new Vector3(0.55f, 1.1f, 0.55f), floorMat, false);
            chimney.transform.SetParent(roof.transform, true);

            var lampGo = new GameObject("InteriorLamp");
            lampGo.transform.SetParent(root.transform, false);
            lampGo.transform.localPosition = new Vector3(0, Mathf.Min(2.4f, h - 0.4f), 0);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.86f, 0.65f);
            lamp.intensity = 0.55f;
            lamp.range = Mathf.Max(6f, w * 0.7f);
            lamp.shadows = LightShadows.None;

            TintWalls(root, position);

            var fade = root.AddComponent<RoofFadeController>();
            fade.Configure(new[] { roofRend, chimney.GetComponent<Renderer>() });
            var vol = volume.AddComponent<BuildingInteriorVolume>();
            vol.Configure(fade);

            // Legacy RoofFader compat if present elsewhere — not required

            // Loot points
            var lootA = new GameObject("LootPoint");
            lootA.transform.SetParent(root.transform, false);
            lootA.transform.localPosition = new Vector3(0, 0.2f, 0);

            return root;
        }

        private static void AddWindow(Transform parent, Vector3 localPos, Material frame)
        {
            var glass = Box(parent, "Window", localPos, new Vector3(1.15f, 0.9f, 0.06f), frame, false);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(0.55f, 0.72f, 0.82f));
            glass.GetComponent<Renderer>().SetPropertyBlock(block);
            Box(parent, "FrameT", localPos + new Vector3(0, 0.52f, 0), new Vector3(1.35f, 0.08f, 0.1f), frame, false);
            Box(parent, "FrameB", localPos + new Vector3(0, -0.52f, 0), new Vector3(1.35f, 0.08f, 0.1f), frame, false);
            Box(parent, "FrameL", localPos + new Vector3(-0.62f, 0, 0), new Vector3(0.08f, 1.05f, 0.1f), frame, false);
            Box(parent, "FrameR", localPos + new Vector3(0.62f, 0, 0), new Vector3(0.08f, 1.05f, 0.1f), frame, false);
        }

        private static void TintWalls(GameObject root, Vector3 worldPos)
        {
            float h = Mathf.Abs(Mathf.Sin(worldPos.x * 0.17f + worldPos.z * 0.11f));
            Color mul = Color.Lerp(new Color(0.86f, 0.9f, 0.96f), new Color(1.06f, 1f, 0.9f), h);
            var block = new MaterialPropertyBlock();
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r.gameObject.name.StartsWith("Wall") || r.sharedMaterial == null)
                    continue;
                Color baseCol = r.sharedMaterial.HasProperty("_BaseColor")
                    ? r.sharedMaterial.GetColor("_BaseColor")
                    : r.sharedMaterial.color;
                block.SetColor("_BaseColor", baseCol * mul);
                r.SetPropertyBlock(block);
            }
        }

        private static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (mat != null)
                go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!solid)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            else
            {
                TrySetTag(go, "Cover");
            }
            return go;
        }

        private static void TrySetTag(GameObject go, string tag)
        {
            try { go.tag = tag; }
            catch (UnityException) { /* tag not in TagManager — skip */ }
        }
    }
}
