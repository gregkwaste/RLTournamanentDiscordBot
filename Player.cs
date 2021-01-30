using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TourneyDiscordBotWPF.Common;


namespace TourneyDiscordBotWPF
{
    public class Player
    {
        public int ID { get; set; }

        public ulong DiscordID { get; set; }
        public string Name { get; set; }

        public Rank Rank { get; set; }

        public void setRankFromText(string rank_text)
        {
            //TODO: CHeck if parse failed
            Rank = Common.Common.getRankFromText(rank_text);
        }

    }

    public abstract class Team
    {
        public int ID { get; set; }
        public bool IsDummy { get; set; } = false;

        public Player Captain { get; set; }

        public virtual List<Player> Players
        {
            get { return new List<Player>() { }; }
        }

        public string Repr
        {
            get
            {
                List<string> Names = new List<string>();
                foreach (Player p in Players)
                    Names.Add(p.Name);
                return string.Join("\n", Names);
            }
        }
    }

    public class Team2s : Team
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }

        public override List<Player> Players
        {
            get { return new List<Player>() { Player1, Player2 }; }
        }

    }

    public class Team3s : Team
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }
        public Player Player3 { get; set; }

        public override List<Player> Players
        {
            get { return new List<Player>() { Player1, Player2, Player3 }; }
        }
    }

    public class LobbyInfo
    {
        public string Name { get; set; }
        public string Pass { get; set; }
    }

    public class Matchup : INotifyPropertyChanged
    {
        public int ID { get; set; }

        public LobbyInfo Lobby;

        private Team _team1;
        private Team _team2;
        public Team Team1
        {
            get
            {
                return _team1;
            }

            set
            {
                _team1 = value;
                NotifyPropertyChanged("Team1");
                NotifyPropertyChanged("IsValid");
                NotifyPropertyChanged("Color");
            }
        }
        public Team Team2
        {
            get
            {
                return _team2;
            }

            set
            {
                _team2 = value;
                NotifyPropertyChanged("Team2");
                NotifyPropertyChanged("IsValid");
                NotifyPropertyChanged("Color");
            }
        }

        public bool _isFinished = false;
        public bool IsFinished
        {

            get
            {
                return _isFinished;
            }

            set
            {
                _isFinished = value;
            }
        }

        public bool _inProgress = false;
        public bool InProgress
        {

            get
            {
                return _inProgress;
            }

            set
            {
                _inProgress = value;
            }
        }

        private Team _winner;
        public Team Winner
        {
            get
            {
                return _winner;
            }

            set
            {
                _winner = value;
                _isFinished = (value is null) ? false : true;
                InProgress = false;
                NotifyPropertyChanged("Winner");
                NotifyPropertyChanged("Color");
            }
        }

        public Team Team1ReportedWinner { get; set; } = null;

        public Team Team2ReportedWinner { get; set; } = null;

        public int RoundID { get; set; }

        public Matchup Next { get; set; }


        
        public bool IsDummy
        {
            get
            {
                if (!IsValid)
                    return false;
                if (Team1.IsDummy && Team2.IsDummy)
                    return true;
                else
                    return false;
            }
        }


        public bool IsValid
        {
            get
            {
                return (_team1 != null) && (_team2 != null);
            }
        }

        public void ResolveDummyness()
        {
            //Decide early winners
            if (Team1.IsDummy && !Team2.IsDummy)
                Winner = Team2;
            else if (!Team1.IsDummy && Team2.IsDummy)
                Winner = Team1;
            else if (Team1.IsDummy && Team2.IsDummy)
            {
                //Decide a random Winner
                float rand = (new Random()).Next() % 100 / 100.0f;
                Winner = (rand > 0.5f) ? Team1 : Team2;
            }
        }

        public System.Windows.Media.Brush Color
        {
            get
            {
                if (!IsValid)
                {
                    return System.Windows.Media.Brushes.LightYellow;
                }
                else if (IsFinished)
                {
                    return System.Windows.Media.Brushes.LightGreen;
                }
                else
                    return System.Windows.Media.Brushes.White;
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        public void Report()
        {
            Console.WriteLine("\t {0} vs {1} ", Team1?.ID, Team2?.ID);
        }

    }
}
