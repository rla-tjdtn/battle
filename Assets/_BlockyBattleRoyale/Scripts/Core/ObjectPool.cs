using System.Collections.Generic;
using UnityEngine;

namespace BlockyBattleRoyale.Core
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        private readonly Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();

        private void Awake()
        {
            Instance = this;
        }

        public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            string key = prefab.name;
            if (!pools.TryGetValue(key, out var q))
            {
                q = new Queue<GameObject>();
                pools[key] = q;
            }

            GameObject go;
            if (q.Count > 0)
            {
                go = q.Dequeue();
                go.transform.SetPositionAndRotation(pos, rot);
                go.SetActive(true);
            }
            else
            {
                go = Instantiate(prefab, pos, rot);
                go.name = prefab.name;
            }

            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null)
                return;
            go.SetActive(false);
            string key = go.name;
            if (!pools.TryGetValue(key, out var q))
            {
                q = new Queue<GameObject>();
                pools[key] = q;
            }
            q.Enqueue(go);
        }
    }
}
