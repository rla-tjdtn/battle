using UnityEngine;

namespace BlockyBattleRoyale.Visibility
{
    public class GrassStealth : MonoBehaviour
    {
        [SerializeField] private float revealDistance = 6f;
        [SerializeField] private bool isHidden;

        public bool IsHidden => isHidden;
        public float RevealDistance => revealDistance;

        private int grassOverlap;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Cover") || other.name.Contains("Grass"))
            {
                grassOverlap++;
                isHidden = grassOverlap > 0;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Cover") || other.name.Contains("Grass"))
            {
                grassOverlap = Mathf.Max(0, grassOverlap - 1);
                isHidden = grassOverlap > 0;
            }
        }

        public void SetHidden(bool value)
        {
            isHidden = value;
        }
    }
}
