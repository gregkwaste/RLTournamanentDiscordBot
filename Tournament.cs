using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace TourneyDiscordBotWPF
{
    public class Tournament
    {
        public List<Player> _players = new List<Player>();
        public List<Team> _teams = new List<Team>();
        public Bracket bracket;

        public ObservableCollection<Player> Players
        {
            get
            {
                return new ObservableCollection<Player>(_players);
            }
        }

        public ObservableCollection<Team> Teams
        {
            get
            {
                return new ObservableCollection<Team>(_teams);
            }
        }

        public bool RegistrationsEnabled { get; set; } = false;

        public bool IsStarted { get; set; } = false;

        public bool IsFinished { get; set; } = false;


        public void ClearPlayers()
        {
            _players.Clear();
        }

        public void ClearTeams()
        {
            _teams.Clear();
        }

        public void Clear()
        {
            bracket = null;
            ClearTeams();
            ClearPlayers();
        }

        public bool CreatePlayer(string name, string rank_name, ulong discord_id=0xFFFFFFFFFFFF)
        {
            if (!Common.Common.rankExists(rank_name))
                return false;

            Player p = new Player();
            p.ID = _players.Count;
            p.Name = name;
            p.DiscordID = discord_id;
            p.setRankFromText(rank_name);
            _players.Add(p);

            return true;
        }

        public void AddRandomPlayers()
        {
            for (int i = 0; i < 32; i++)
            {
                CreatePlayer("test", "Bronze I");
            }
        }

        public void CreateTeams()
        {
            //By default we're generating teams for 2s

            List<Player> tempPlayers = new List<Player>();
            foreach (Player p in _players)
                tempPlayers.Add(p);
            tempPlayers.Sort((a, b) => a.Rank._rank.CompareTo(b.Rank._rank));

            _teams.Clear();

            if (tempPlayers.Count % 2 > 0)
            {
                MessageBox.Show("ZIMA BALE ALLON ENAN XUMA PAIXTH KAI KSANAPATA");
                return;
            }


            while (tempPlayers.Count > 0)
            {
                Team2s t = new Team2s();
                t.ID = _teams.Count;
                t.Player1 = tempPlayers.First();
                t.Player2 = tempPlayers.Last();
                t.Captain = t.Player2; //Always set captain to player 2

                //Remove the entires
                tempPlayers.RemoveAt(0);
                tempPlayers.RemoveAt(tempPlayers.Count - 1);

                _teams.Add(t);
            }

        }

        public void ExportBracket()
        {
            bracket.GenerateSVG();
        }

        public void CreateBracket()
        {
            bracket = new Bracket();
            bracket.GenerateBracket(_teams.Count);
            bracket.Populate(_teams.ToList());
            bracket.Report();
        }

        public void RemovePlayer(Player p)
        {
            _players.Remove(p);
        }

        public void ImportPlayersFromCSV(string filename)
        {
            //Parse CSV
            string text = System.IO.File.ReadAllText(filename);
            //Console.WriteLine(text);

            string[] lines = text.Split('\n');

            for (int i = 1; i < lines.Length; i++)
            {
                string l = lines[i];

                //Format is as follows
                //Column 0: Date
                //Column 1: Player Twitch Name
                //Column 2: Player Steam Name
                //Column 3: Rank Text

                string[] _l = l.Trim().Split(',');

                Player p = new Player();
                p.ID = _players.Count;
                p.Name = _l[1];

                //Fix ranktext

                string _rank_curated_name = _l[3];
                _rank_curated_name = _rank_curated_name.Replace("1", "I");
                _rank_curated_name = _rank_curated_name.Replace("2", "II");
                _rank_curated_name = _rank_curated_name.Replace("3", "III");

                p.Rank = Common.Common.getRankFromText(_rank_curated_name);
                _players.Add(p);
            }
        }



    }
}
