using UnityEngine;

namespace BlockyBattleRoyale.Core
{
    public static class GameSettings
    {
        const string Master = "bbr_master";
        const string Sfx = "bbr_sfx";
        const string Music = "bbr_music";
        const string Sens = "bbr_sens";
        const string Fps = "bbr_fps";
        const string Squad = "bbr_squad";
        const string Knock = "bbr_knock";

        public static int SquadSize
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(Squad, 1), 1, 4);
            set => PlayerPrefs.SetInt(Squad, Mathf.Clamp(value, 1, 4));
        }

        public static bool KnockdownEnabled
        {
            get => PlayerPrefs.GetInt(Knock, SquadSize > 1 ? 1 : 0) != 0;
            set => PlayerPrefs.SetInt(Knock, value ? 1 : 0);
        }

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(Master, 1f);
            set { PlayerPrefs.SetFloat(Master, value); AudioListener.volume = value; }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(Sfx, 1f);
            set => PlayerPrefs.SetFloat(Sfx, value);
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(Music, 0.6f);
            set => PlayerPrefs.SetFloat(Music, value);
        }

        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat(Sens, 1f);
            set => PlayerPrefs.SetFloat(Sens, value);
        }

        public static bool MinimapRotate
        {
            get => PlayerPrefs.GetInt("bbr_map_rot", 0) != 0;
            set => PlayerPrefs.SetInt("bbr_map_rot", value ? 1 : 0);
        }

        public static float HudScale
        {
            get => PlayerPrefs.GetFloat("bbr_hud", 1f);
            set => PlayerPrefs.SetFloat("bbr_hud", Mathf.Clamp(value, 0.8f, 1.4f));
        }

        public static int Cosmetic
        {
            get => PlayerPrefs.GetInt("bbr_cosmetic", 0);
            set => PlayerPrefs.SetInt("bbr_cosmetic", value);
        }

        public static bool AutoWeapon
        {
            get => PlayerPrefs.GetInt("bbr_auto_wpn", 1) != 0;
            set => PlayerPrefs.SetInt("bbr_auto_wpn", value ? 1 : 0);
        }

        public static bool AutoArmor
        {
            get => PlayerPrefs.GetInt("bbr_auto_armor", 1) != 0;
            set => PlayerPrefs.SetInt("bbr_auto_armor", value ? 1 : 0);
        }

        public static bool AutoBag
        {
            get => PlayerPrefs.GetInt("bbr_auto_bag", 1) != 0;
            set => PlayerPrefs.SetInt("bbr_auto_bag", value ? 1 : 0);
        }

        public static bool AutoAttachment
        {
            get => PlayerPrefs.GetInt("bbr_auto_att", 1) != 0;
            set => PlayerPrefs.SetInt("bbr_auto_att", value ? 1 : 0);
        }

        public static bool AutoUpgrade
        {
            get => PlayerPrefs.GetInt("bbr_auto_up", 1) != 0;
            set => PlayerPrefs.SetInt("bbr_auto_up", value ? 1 : 0);
        }

        public static bool TutorialDone
        {
            get => PlayerPrefs.GetInt("bbr_tutorial", 0) != 0;
            set { PlayerPrefs.SetInt("bbr_tutorial", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static int TargetFps
        {
            get => PlayerPrefs.GetInt(Fps, 60);
            set
            {
                PlayerPrefs.SetInt(Fps, value);
                Application.targetFrameRate = value;
            }
        }

        public static void ApplyAll()
        {
            AudioListener.volume = MasterVolume;
            Application.targetFrameRate = TargetFps;
            QualitySettings.vSyncCount = 0;
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
