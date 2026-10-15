using UnityEngine;
using BlockyBattleRoyale.Occlusion;
using BlockyBattleRoyale.Visibility;

namespace BlockyBattleRoyale.Environment
{
    public enum TreeKind
    {
        Small,
        Medium,
        Large,
        Pine,
        Dry
    }

    public static class VegetationFactory
    {
        public static GameObject CreateTree(Transform parent, Vector3 pos, TreeKind kind, Material trunkMat, Material leafMat)
        {
            float height = kind switch
            {
                TreeKind.Small => Random.Range(2.6f, 3.8f),
                TreeKind.Large => Random.Range(6.5f, 8.5f),
                TreeKind.Pine => Random.Range(5.5f, 7.5f),
                TreeKind.Dry => Random.Range(3.5f, 5.5f),
                _ => Random.Range(4.2f, 6.2f)
            };
            float canopy = kind switch
            {
                TreeKind.Small => 1.4f,
                TreeKind.Large => 3.2f,
                TreeKind.Pine => 2.2f,
                TreeKind.Dry => 1.6f,
                _ => 2.4f
            };

            var root = new GameObject("Tree_" + kind);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            float trunkR = kind == TreeKind.Large ? 0.42f : 0.28f;
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0, height * 0.22f, 0);
            trunk.transform.localScale = new Vector3(trunkR, height * 0.22f, trunkR);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
            TrySetTag(trunk, "Cover");

            var upper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upper.name = "TrunkUpper";
            upper.transform.SetParent(root.transform, false);
            upper.transform.localPosition = new Vector3(0.04f, height * 0.48f, 0);
            upper.transform.localScale = new Vector3(trunkR * 0.62f, height * 0.16f, trunkR * 0.62f);
            upper.transform.localRotation = Quaternion.Euler(0, 0, 6f);
            upper.GetComponent<Renderer>().sharedMaterial = trunkMat;
            Object.DestroyImmediate(upper.GetComponent<Collider>());

            if (kind == TreeKind.Pine)
            {
                for (int i = 0; i < 4; i++)
                {
                    float y = height * (0.38f + i * 0.15f);
                    float s = canopy * (1.05f - i * 0.2f);
                    AddCanopy(root.transform, new Vector3((i % 2 == 0 ? -0.15f : 0.12f), y, i * 0.05f), new Vector3(s, s * 0.42f, s), leafMat);
                }
            }
            else if (kind == TreeKind.Dry)
            {
                AddCanopy(root.transform, new Vector3(0.1f, height * 0.72f, 0), new Vector3(canopy * 0.55f, canopy * 0.28f, canopy * 0.5f), leafMat);
                AddCanopy(root.transform, new Vector3(-0.25f, height * 0.62f, 0.15f), new Vector3(canopy * 0.35f, canopy * 0.2f, canopy * 0.35f), leafMat);
            }
            else
            {
                AddCanopy(root.transform, new Vector3(0, height * 0.7f, 0), new Vector3(canopy, canopy * 0.62f, canopy * 0.9f), leafMat);
                AddCanopy(root.transform, new Vector3(canopy * 0.28f, height * 0.58f, -canopy * 0.12f), new Vector3(canopy * 0.55f, canopy * 0.4f, canopy * 0.5f), leafMat);
                AddCanopy(root.transform, new Vector3(-canopy * 0.22f, height * 0.78f, canopy * 0.1f), new Vector3(canopy * 0.48f, canopy * 0.36f, canopy * 0.48f), leafMat);
                if (kind == TreeKind.Large)
                    AddCanopy(root.transform, new Vector3(0.1f, height * 0.9f, 0.05f), new Vector3(canopy * 0.4f, canopy * 0.3f, canopy * 0.4f), leafMat);
            }

            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            return root;
        }

        private static void AddCanopy(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(parent, false);
            canopy.transform.localPosition = localPos;
            canopy.transform.localScale = scale;
            canopy.GetComponent<Renderer>().sharedMaterial = mat;
            var col = canopy.GetComponent<Collider>();
            if (col != null) col.isTrigger = true; // camera occlusion only, no bullet block
            // Do NOT set custom tags (undefined tags corrupt/crash player builds)
            var marker = canopy.AddComponent<CameraOccluderMarker>();
            marker.Configure(canopy.GetComponent<Renderer>());
        }

        public static GameObject CreateBush(Transform parent, Vector3 pos, Material mat)
        {
            var root = new GameObject("Bush");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            for (int i = 0; i < 3; i++)
            {
                var blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blob.name = "BushBlob";
                blob.transform.SetParent(root.transform, false);
                blob.transform.localPosition = new Vector3((i - 1) * 0.35f, 0.35f + (i == 1 ? 0.15f : 0f), (i % 2) * 0.15f);
                blob.transform.localScale = new Vector3(0.9f, 0.65f, 0.85f);
                blob.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(blob.GetComponent<Collider>());
            }
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 0.4f, 0);
            trigger.size = new Vector3(1.8f, 1f, 1.6f);
            root.AddComponent<GrassPatch>();
            return root;
        }

        public static GameObject CreateRock(Transform parent, Vector3 pos, Material mat)
        {
            var go = new GameObject("Rock");
            go.transform.SetParent(parent, false);
            float s = Random.Range(0.7f, 2.1f);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            int chunks = Random.Range(2, 4);
            for (int i = 0; i < chunks; i++)
            {
                var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = "RockChunk";
                chunk.transform.SetParent(go.transform, false);
                chunk.transform.localPosition = new Vector3((i - 1) * s * 0.22f, s * 0.28f, i * 0.05f);
                chunk.transform.localScale = new Vector3(s * Random.Range(0.55f, 0.9f), s * Random.Range(0.35f, 0.7f), s * Random.Range(0.5f, 0.85f));
                chunk.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0, 40), Random.Range(-8f, 8f));
                chunk.GetComponent<Renderer>().sharedMaterial = mat;
                if (i > 0) Object.DestroyImmediate(chunk.GetComponent<Collider>());
            }
            TrySetTag(go, "Cover");
            return go;
        }

        public static GameObject CreateFenceSegment(Transform parent, Vector3 pos, float yaw, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Fence";
            go.transform.SetParent(parent, false);
            go.transform.position = pos + Vector3.up * 0.6f;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = new Vector3(2.2f, 1.2f, 0.12f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            TrySetTag(go, "Cover");
            return go;
        }

        public static GameObject CreateProp(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            TrySetTag(go, "Cover");
            return go;
        }

        public static GameObject CreateGrassPatch(Transform parent, Vector3 pos, Material mat)
        {
            var go = new GameObject("TallGrass");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            int blades = 7;
            for (int i = 0; i < blades; i++)
            {
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = "Blade";
                blade.transform.SetParent(go.transform, false);
                float a = i * 51.4f;
                blade.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.45f, 0.35f, Mathf.Sin(a) * 0.45f);
                blade.transform.localScale = new Vector3(0.08f, 0.7f, 0.22f);
                blade.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), a * Mathf.Rad2Deg, 0f);
                blade.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(blade.GetComponent<Collider>());
            }
            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 0.4f, 0);
            trigger.size = new Vector3(1.6f, 1f, 1.6f);
            go.AddComponent<GrassPatch>();
            return go;
        }

        private static void TrySetTag(GameObject go, string tag)
        {
            try { go.tag = tag; }
            catch (UnityException) { }
        }
    }
}
