using UnityEngine;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.Core;
using BlockyBattleRoyale.Player;
using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.World
{
    /// <summary>Recalls a finally-dead teammate. Separate from knockdown revive.</summary>
    public class RecallStation : MonoBehaviour
    {
        [SerializeField] private int usesLeft = 1;
        [SerializeField] private float channelTime = 8f;
        [SerializeField] private int lockFromPhase = 5;

        private float progress;
        private Vector3 startPos;
        private bool channeling;

        private void Update()
        {
            if (GameSettings.SquadSize <= 1 || usesLeft <= 0)
                return;
            var zone = BattleRoyale.ZoneManager.Instance;
            if (zone != null && zone.PhaseIndex + 1 >= lockFromPhase)
                return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            if (Vector3.Distance(player.transform.position, transform.position) > 2.4f)
            {
                channeling = false;
                return;
            }

            var self = player.GetComponent<KnockdownSystem>();
            if (self != null && self.IsKnocked) return;
            var dead = FindDeadTeammate(self != null ? self.TeamId : 1);
            if (dead == null) return;

            if (Input.GetKey(KeyCode.F))
            {
                if (!channeling)
                {
                    channeling = true;
                    progress = 0f;
                    startPos = player.transform.position;
                }
                if (Vector3.Distance(player.transform.position, startPos) > 0.4f)
                {
                    channeling = false;
                    return;
                }
                var hp = player.GetComponent<Health>();
                if (hp != null && !hp.IsAlive)
                {
                    channeling = false;
                    return;
                }
                progress += Time.deltaTime;
                if (progress >= channelTime)
                {
                    Recall(dead);
                    usesLeft--;
                    channeling = false;
                }
            }
            else
            {
                channeling = false;
            }
        }

        private KnockdownSystem FindDeadTeammate(int team)
        {
            foreach (var kd in FindObjectsByType<KnockdownSystem>(FindObjectsSortMode.None))
            {
                if (kd.TeamId != team || kd.IsKnocked) continue;
                var hp = kd.GetComponent<Health>();
                if (hp != null && !hp.IsAlive && !kd.WasRecalled)
                    return kd;
            }
            return null;
        }

        private void Recall(KnockdownSystem kd)
        {
            var hp = kd.GetComponent<Health>();
            hp?.RestoreAfterRecall(100f);
            kd.MarkRecalled();
            kd.transform.position = transform.position + Vector3.right * 1.5f;
            var motor = kd.GetComponent<PlayerMotor>();
            if (motor != null) motor.enabled = true;
            var weapon = kd.GetComponent<WeaponController>();
            if (weapon != null) weapon.enabled = true;
            var anim = kd.GetComponent<BlockyAnimator>();
            if (anim != null) anim.enabled = true;
        }

        private void OnGUI()
        {
            if (!channeling) return;
            GUI.Label(new Rect(Screen.width * 0.5f - 80, Screen.height * 0.62f, 200, 24),
                $"복귀 {progress:0.0}/{channelTime:0}");
        }
    }
}
