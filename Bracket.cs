using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Svg;

namespace TourneyDiscordBotWPF
{
    public class Bracket
    {
        public Matchup MatchupTree = new Matchup();
        public List<Matchup> Matchups = new List<Matchup>();
        public List<Round> Rounds = new List<Round>();
        public int BracketRequiredTeams = 0;

        public Bracket()
        {

        }

        public void GenerateSVG()
        {
            SvgDocument doc = new SvgDocument();
            doc.FontSize = 16;

            SvgColourServer blackPainter = new SvgColourServer(Color.Black);
            SvgColourServer whitePainter = new SvgColourServer(Color.White);
            SvgColourServer greenPainter = new SvgColourServer(Color.LightGreen);

            
            int round_gap = 50;
            int matchup_width = 400;
            int matchup_height = 150;
            int matchup_team_x_offset = 20;
            int matchup_team_y_offset = 10;
            int matchup_team_font_size = 36;

            int matchup_offset_y = 0;
            int matchup_gap_y = 10;
            
            for (int r_i = 0; r_i < Rounds.Count; r_i++)
            {
                Round r = Rounds[r_i];
                Console.WriteLine("Round {0} Matches: ", r.ID);

                //Calculate matchup gap
                int round_x_offset = r_i * (matchup_width + round_gap);

                for (int m_i = 0; m_i < r.Matchups.Count; m_i++)
                {
                    Matchup m = r.Matchups[m_i];

                    int round_y_offset = m_i * (matchup_height + matchup_gap_y) + matchup_offset_y;
                    
                    //Draw Rectangle per Matchup

                    SvgRectangle rec = new SvgRectangle();
                    rec.X = round_x_offset;
                    rec.Y = round_y_offset;
                    rec.Width = matchup_width;
                    rec.Height = matchup_height;


                    if (!m.IsFinished)
                        rec.Fill = whitePainter;
                    else
                        rec.Fill = greenPainter;

                    string team_text = "TBD";
                    if (m.Team1 != null)
                        team_text = "Team " + (m.Team1 != null ? m.Team1.ID : -1).ToString();

                    //Add Text
                    SvgText t1 = new SvgText(team_text);
                    t1.FontSize = matchup_team_font_size;
                    t1.X.Add(round_x_offset + 50 + matchup_team_x_offset);
                    t1.Y.Add(round_y_offset + 36 + matchup_team_y_offset);

                    SvgText t3 = new SvgText("vs");
                    t3.FontSize = matchup_team_font_size;
                    t3.X.Add(round_x_offset + matchup_team_x_offset + 10);
                    t3.Y.Add(round_y_offset + 72 + matchup_team_y_offset);

                    team_text = "TBD";
                    if (m.Team2 != null)
                        team_text = "Team " + (m.Team2 != null ? m.Team2.ID : -1).ToString();

                    SvgText t2 = new SvgText(team_text);
                    t2.FontSize = matchup_team_font_size;
                    t2.X.Add(round_x_offset + 50 + matchup_team_x_offset);
                    t2.Y.Add(round_y_offset + 108 + matchup_team_y_offset);
                    
                    doc.Children.Add(rec);
                    doc.Children.Add(t1);
                    doc.Children.Add(t3);
                    doc.Children.Add(t2);
                }

                //Update Matchup gaps and offsets
                matchup_offset_y += (matchup_height + matchup_gap_y) / 2;
                matchup_gap_y = 2 * (matchup_gap_y + matchup_height) - matchup_height;

            }

            Bitmap img = doc.Draw();
            img.Save("bracket.png");
        
        }

        public void GenerateBracket(int teamCount)
        {
            //Find max team count
            BracketRequiredTeams = 2;

            while (BracketRequiredTeams < teamCount)
                BracketRequiredTeams *= 2;

            int round_num = (int)Math.Log(BracketRequiredTeams, 2);

            //Populate Rounds
            for (int i = 0; i < round_num; i++)
            {
                Round r = new Round();
                r.ID = i;
                Rounds.Add(r);
            }

            //CreateFinalsMachup
            MatchupTree.RoundID = round_num;
            Rounds[round_num - 1].Matchups.Add(MatchupTree);

            GenerateMatchups(MatchupTree, round_num - 2);
        }

        public void Populate(List<Team> teams)
        {
            //Check if there are enough teams for the bracket
            List<Team> teamsTemp = new List<Team>();
            foreach (Team t in teams)
                teamsTemp.Add(t);


            while (teamsTemp.Count < BracketRequiredTeams)
            {
                //Add random Teams
                if (teams[0] is Team2s)
                {
                    Team2s t = new Team2s();
                    t.ID = -1;
                    t.IsDummy = true;
                    teamsTemp.Add(t);
                }
                else
                {
                    Console.WriteLine("Not Supported");
                }
            }

            Round first_round = Rounds[0];
            Random rnd = new Random();
            List<Team> randTeamList = teamsTemp.Select(x => new { value = x, order = rnd.Next() })
            .OrderBy(x => x.order).Select(x => x.value).ToList();

            int team_index = 0;
            foreach (Matchup match in first_round.Matchups)
            {
                match.Team1 = randTeamList[team_index];
                match.Team2 = randTeamList[team_index + 1];
                team_index += 2;
            }

            FindEarlyWinners();
        }

        private void FindEarlyWinners()
        {
            while (true)
            {
                int matches_updated = 0;
                foreach (Matchup match in Matchups)
                {
                    if (!match.IsValid)
                        continue;

                    if (match.Winner != null)
                        continue;
                    match.ResolveDummyness();

                    if (match.Winner != null)
                    {
                        matches_updated++;

                        if (match.Next != null)
                        {
                            if (match.Next.Team1 != null)
                                match.Next.Team2 = match.Winner;
                            else
                                match.Next.Team1 = match.Winner;
                        }
                    }


                }

                if (matches_updated == 0)
                    break;

            }

        }

        public void GenerateMatchups(Matchup matchup, int round_num)
        {
            if (round_num < 0)
                return;

            //Generate 2 matchups per matchup 

            Matchup m1 = new Matchup()
            {
                ID = Matchups.Count,
                Team1 = null,
                Team2 = null,
                Winner = null,
                Next = matchup,
                IsFinished = false,
                RoundID = round_num
            };

            Matchup m2 = new Matchup()
            {
                ID = Matchups.Count + 1,
                Team1 = null,
                Team2 = null,
                Winner = null,
                IsFinished = false,
                Next = matchup,
                RoundID = round_num
            };

            //Save Matchups
            Matchups.Add(m1);
            Matchups.Add(m2);

            //Add Matchups to round
            Rounds[round_num].Matchups.Add(m1);
            Rounds[round_num].Matchups.Add(m2);

            //Recursively generate previous rounds
            GenerateMatchups(m1, round_num - 1);
            GenerateMatchups(m2, round_num - 1);
        }

        public void Report()
        {
            foreach (Round r in Rounds)
            {
                Console.WriteLine("Round {0} Matches: ", r.ID);

                foreach (Matchup m in r.Matchups)
                {
                    m.Report();
                }

            }
        }

    }
}
