using UnityEngine;

namespace BlockyBattleRoyale.Occlusion
{
    /// <summary>Marks renderers that may fade when between camera and player.</summary>
    public class CameraOccluderMarker : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;

        public Renderer[] Renderers
        {
            get
            {
                if (renderers != null && renderers.Length > 0)
                    return renderers;
                return GetComponentsInChildren<Renderer>();
            }
        }

        public void Configure(params Renderer[] r) => renderers = r;
    }
}
