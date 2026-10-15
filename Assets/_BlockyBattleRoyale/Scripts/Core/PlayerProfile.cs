using UnityEngine;

namespace BlockyBattleRoyale.Core
{
    /// <summary>Local profile only. Not an official online ranked record.</summary>
    public static class PlayerProfile
    {
        const string Ver = "bbr_save_ver";
        static bool recorded;

        public static int Matches => PlayerPrefs.GetInt("bbr_matches", 0);
        public static int Wins => PlayerPrefs.GetInt("bbr_wins", 0);
        public static int Kills => PlayerPrefs.GetInt("bbr_kills", 0);
        public static float BestTime => PlayerPrefs.GetFloat("bbr_best_time", 0f);
        public static int BestPlace => PlayerPrefs.GetInt("bbr_best_place", 99);

        public static void BeginMatch()
        {
            recorded = false;
            PlayerPrefs.SetInt(Ver, 1);
        }

        public static void RecordOnce(bool victory, int kills, float surviveSeconds, int place)
        {
            if (recorded) return;
            recorded = true;
            PlayerPrefs.SetInt("bbr_matches", Matches + 1);
            if (victory) PlayerPrefs.SetInt("bbr_wins", Wins + 1);
            PlayerPrefs.SetInt("bbr_kills", Kills + kills);
            if (surviveSeconds > BestTime) PlayerPrefs.SetFloat("bbr_best_time", surviveSeconds);
            if (place > 0 && place < BestPlace) PlayerPrefs.SetInt("bbr_best_place", place);
            float total = PlayerPrefs.GetFloat("bbr_time_sum", 0f) + surviveSeconds;
            PlayerPrefs.SetFloat("bbr_time_sum", total);
            PlayerPrefs.Save();
        }

        public static float AverageTime => Matches <= 0 ? 0f : PlayerPrefs.GetFloat("bbr_time_sum", 0f) / Matches;

        public static string Summary()
        {
            return $"경기 {Matches}  승 {Wins}  킬 {Kills}\n평균 생존 {AverageTime:0}s  최고 {BestTime:0}s  최고순위 {BestPlace}";
        }
    }
}
