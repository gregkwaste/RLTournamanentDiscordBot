using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TourneyDiscordBotWPF.Common
{
    public enum RL_RANK
    {
        UNRANKED = 0x0,
        BRONZE_I,
        BRONZE_II,
        BRONZE_III,
        SILVER_I,
        SILVER_II,
        SILVER_III,
        GOLD_I,
        GOLD_II,
        GOLD_III,
        PLATINUM_I,
        PLATINUM_II,
        PLATINUM_III,
        DIAMOND_I,
        DIAMOND_II,
        DIAMOND_III,
        CHAMPION_I,
        CHAMPION_II,
        CHAMPION_III,
        GRAND_CHAMPION_I,
        GRAND_CHAMPION_II,
        GRAND_CHAMPION_III,
        SUPERSONIC_LEGEND,
        NONE
    }

    //Delegates
    public delegate void Log(string msg);

    public static class Common
    {
        public static Dictionary<string, Rank> Ranks = new Dictionary<string, Rank>();
        public static List<string> RankNames = new List<string>();
        public static Log loggerFunc;

        public static void Populate()
        {
            //Clear
            RankNames.Clear();
            Ranks.Clear();
            
            RankNames.Add("Bronze I");
            RankNames.Add("Bronze II");
            RankNames.Add("Bronze III");
            RankNames.Add("Silver I");
            RankNames.Add("Silver II");
            RankNames.Add("Silver III");
            RankNames.Add("Gold I");
            RankNames.Add("Gold II");
            RankNames.Add("Gold III");
            RankNames.Add("Platinum I");
            RankNames.Add("Platinum II");
            RankNames.Add("Platinum III");
            RankNames.Add("Diamond I");
            RankNames.Add("Diamond II");
            RankNames.Add("Diamond III");
            RankNames.Add("Champion I");
            RankNames.Add("Champion II");
            RankNames.Add("Champion III");
            RankNames.Add("Grand Champion I");
            RankNames.Add("Grand Champion II");
            RankNames.Add("Grand Champion III");
            RankNames.Add("SuperSonic Legend");

            foreach (string r in RankNames)
            {
                Ranks[r] = new Rank(r);
            }
        }

        public static Rank getRankFromText(string rank_text)
        {
            return Ranks[rank_text];
        }

        public static bool rankExists(string rank_text)
        {
            if (Ranks.Keys.Contains(rank_text))
                return true;
            return false;
        }

    }
}
