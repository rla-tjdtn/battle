using UnityEngine;

namespace BlockyBattleRoyale.TerrainSystem
{
    public static class MeshTerrainBuilder
    {
        public static GameObject Build(Transform parent, float halfExtent, int resolution, float maxHeight, Material mat, int seed)
        {
            var go = new GameObject("MeshTerrain");
            go.transform.SetParent(parent, false);
            go.tag = "Ground";

            int res = Mathf.Clamp(resolution, 16, 128);
            var mesh = new Mesh { name = "VillageTerrain" };
            int vertCount = (res + 1) * (res + 1);
            var verts = new Vector3[vertCount];
            var norms = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var colors = new Color[vertCount];

            Random.InitState(seed);
            float size = halfExtent * 2f;
            for (int z = 0; z <= res; z++)
            {
                for (int x = 0; x <= res; x++)
                {
                    int i = z * (res + 1) + x;
                    float nx = x / (float)res;
                    float nz = z / (float)res;
                    float wx = -halfExtent + nx * size;
                    float wz = -halfExtent + nz * size;
                    float h = SampleHeight(wx, wz, halfExtent, maxHeight, seed);
                    verts[i] = new Vector3(wx, h, wz);
                    uvs[i] = new Vector2(nx * 8f, nz * 8f);
                    // Blend grass/dirt by height and noise
                    float n = Mathf.PerlinNoise(wx * 0.04f + 3f, wz * 0.04f);
                    float patch = Mathf.PerlinNoise(wx * 0.11f, wz * 0.11f + 8f);
                    Color grassA = new Color(0.36f, 0.55f, 0.3f);
                    Color grassB = new Color(0.24f, 0.42f, 0.22f);
                    Color dirtC = new Color(0.48f, 0.38f, 0.26f);
                    Color col = Color.Lerp(grassB, grassA, n);
                    float dirt = Mathf.Clamp01(h / Mathf.Max(0.01f, maxHeight) * 0.8f + patch * 0.45f - 0.25f);
                    if (Mathf.Abs(wx) < 6.5f || Mathf.Abs(wz) < 6.5f)
                        dirt = Mathf.Max(dirt, 0.55f);
                    colors[i] = Color.Lerp(col, dirtC, dirt);
                    norms[i] = Vector3.up;
                }
            }

            int triCount = res * res * 6;
            var tris = new int[triCount];
            int t = 0;
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int i = z * (res + 1) + x;
                    tris[t++] = i;
                    tris[t++] = i + res + 1;
                    tris[t++] = i + 1;
                    tris[t++] = i + 1;
                    tris[t++] = i + res + 1;
                    tris[t++] = i + res + 2;
                }
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            return go;
        }

        public static float SampleHeight(float x, float z, float halfExtent, float maxHeight, int seed)
        {
            // Keep center flatter for village roads
            float dist = Mathf.Sqrt(x * x + z * z) / halfExtent;
            float n = Mathf.PerlinNoise((x + seed) * 0.035f, (z + seed * 0.7f) * 0.035f);
            float n2 = Mathf.PerlinNoise((x + 40f) * 0.08f, (z - 20f) * 0.08f);
            float h = (n * 0.7f + n2 * 0.3f) * maxHeight;
            // Flatten near origin corridor
            float flat = Mathf.SmoothStep(0.15f, 0.45f, dist);
            return h * flat;
        }
    }
}
