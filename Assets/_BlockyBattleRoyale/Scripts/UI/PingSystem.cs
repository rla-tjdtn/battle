using UnityEngine;

namespace BlockyBattleRoyale.UI
{
    public class PingSystem : MonoBehaviour
    {
        [SerializeField] private float pingLife = 6f;
        private GameObject activePing;

        private void Update()
        {
            if (!Input.GetMouseButtonDown(2))
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
                return;

            if (activePing != null)
                Destroy(activePing);

            activePing = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            activePing.name = "WorldPing";
            Object.Destroy(activePing.GetComponent<Collider>());
            activePing.transform.position = hit.point + Vector3.up * 0.4f;
            activePing.transform.localScale = Vector3.one * 0.55f;
            var r = activePing.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Sprites/Default"));
                r.material.color = new Color(1f, 0.85f, 0.1f, 0.95f);
            }
            Destroy(activePing, pingLife);
        }
    }
}
