using System;
using System.Collections.Generic;
using System.Text;

namespace TourneyDiscordBotWPF
{
    public class Round
    {
        public List<Matchup> Matchups = new List<Matchup>();
        public int ID { get; set; }
    }
}
