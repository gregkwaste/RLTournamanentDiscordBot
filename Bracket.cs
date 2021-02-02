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
            SvgColourServer grayPainter = new SvgColourServer(Color.Gray);
            SvgColourServer greenPainter = new SvgColourServer(Color.LightGreen);

            
            int round_gap = 50;
            int matchup_width = 400;
            int matchup_height = 150;
            int matchup_team_x_offset = 20;
            int matchup_team_y_offset = 50;
            int matchup_team_font_size = 36;
            int matchup_vs_font_size = 24;

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

                    doc.Children.Add(rec);

                    //Team 1 Text
                    string team_text = "TBD";
                    string team_id = "";
                    if (m.Team1 != null)
                    {
                        if (m.Team1.IsDummy)
                            team_text = "-";
                        else
                        {
                            team_text = m.Team1.Name;
                            team_id = m.Team1.ID.ToString("00");
                        }
                            
                    }

                    //Add Text
                    //Τeam ID
                    SvgText t1 = new SvgText(team_id);
                    t1.FontSize = matchup_team_font_size;
                    t1.X.Add(round_x_offset + matchup_team_x_offset - 10);
                    t1.Y.Add(round_y_offset + matchup_team_y_offset);
                    doc.Children.Add(t1);

                    //Τeam Name
                    t1 = new SvgText(team_text);
                    t1.FontSize = matchup_team_font_size;
                    t1.X.Add(round_x_offset + 50 + matchup_team_x_offset);
                    t1.Y.Add(round_y_offset + matchup_team_y_offset);
                    doc.Children.Add(t1);

                    SvgText t3 = new SvgText("vs");
                    t3.FontSize = matchup_vs_font_size;
                    t3.X.Add(round_x_offset + matchup_team_x_offset);
                    t3.Y.Add(round_y_offset + 5 + matchup_height / 2.0f);
                    doc.Children.Add(t3);
                    
                    //Horizontal Matchup Splitter Line Part 1
                    //Add horizontal line
                    SvgLine hl1 = new SvgLine();
                    hl1.Stroke = blackPainter;
                    hl1.StrokeWidth = 5;
                    hl1.StrokeOpacity = 1.0f;
                    hl1.FillOpacity = 1.0f;
                    hl1.StartX = round_x_offset + matchup_team_x_offset + 40;
                    hl1.StartY = round_y_offset + matchup_height / 2.0f;
                    hl1.EndX = round_x_offset + matchup_width;
                    hl1.EndY = hl1.StartY;
                    doc.Children.Add(hl1);


                    //Vertical Line on the splitter
                    SvgLine vl1 = new SvgLine();
                    vl1.Stroke = blackPainter;
                    vl1.StrokeWidth = 5;
                    vl1.StrokeOpacity = 1.0f;
                    vl1.FillOpacity = 1.0f;
                    vl1.StartX = hl1.StartX;
                    vl1.StartY = hl1.StartY;
                    vl1.EndX = vl1.StartX;
                    vl1.EndY = hl1.StartY - matchup_height / 2.0f;
                    doc.Children.Add(vl1);

                    SvgLine vl2 = new SvgLine();
                    vl2.Stroke = blackPainter;
                    vl2.StrokeWidth = 5;
                    vl2.StrokeOpacity = 1.0f;
                    vl2.FillOpacity = 1.0f;
                    vl2.StartX = hl1.StartX;
                    vl2.StartY = hl1.StartY;
                    vl2.EndX = vl1.StartX;
                    vl2.EndY = hl1.StartY + matchup_height / 2.0f;
                    doc.Children.Add(vl2);


                    //Team 2 Text
                    team_text = "TBD";
                    team_id = "";
                    if (m.Team2 != null)
                    {
                        if (m.Team2.IsDummy)
                            team_text = "-";
                        else
                        {
                            team_text = m.Team2.Name;
                            team_id = m.Team2.ID.ToString("00");
                        }
                            
                    }

                    //Τeam ID
                    SvgText t2 = new SvgText(team_id);
                    t2.FontSize = matchup_team_font_size;
                    t2.X.Add(round_x_offset + matchup_team_x_offset - 10);
                    t2.Y.Add(round_y_offset + matchup_height / 2.0f + matchup_team_y_offset);
                    doc.Children.Add(t2);

                    t2 = new SvgText(team_text);
                    t2.FontSize = matchup_team_font_size;
                    t2.X.Add(round_x_offset + 50 + matchup_team_x_offset);
                    t2.Y.Add(round_y_offset + matchup_height / 2.0f + matchup_team_y_offset);
                    doc.Children.Add(t2);

                    //Draw Matchup Connector to next Matchup
                    if (r_i != Rounds.Count - 1)
                    {
                        
                        //Add horizontal line
                        SvgLine l = new SvgLine();
                        l.Stroke = grayPainter;
                        l.StrokeWidth = 5;
                        l.StrokeOpacity = 1.0f;
                        l.FillOpacity = 1.0f;
                        l.StartX = round_x_offset + matchup_width;
                        l.StartY = round_y_offset + matchup_height / 2.0f;
                        l.EndX = l.StartX + round_gap / 2.0f;
                        l.EndY = l.StartY;
                        doc.Children.Add(l);

                        //Vertical Line
                        SvgLine l1 = new SvgLine();
                        l1.Stroke = grayPainter;
                        l1.StrokeWidth = 5;
                        l1.StrokeOpacity = 1.0f;
                        l1.FillOpacity = 1.0f;
                        l1.StartX = l.EndX;
                        l1.StartY = l.EndY;
                        l1.EndX = l.EndX;

                        if (m_i % 2 == 0)
                        {
                            l1.EndY = l.StartY + (matchup_height + matchup_gap_y) / 2.0f;
                            
                        } else
                        {
                            l1.EndY = l.StartY - (matchup_height + matchup_gap_y) / 2.0f;
                        }
                        doc.Children.Add(l1);

                        //Last horizontal line
                        SvgLine l2 = new SvgLine();
                        l2.Stroke = grayPainter;
                        l2.StrokeWidth = 5;
                        l2.StrokeOpacity = 1.0f;
                        l2.FillOpacity = 1.0f;
                        l2.StartX = l1.EndX;
                        l2.StartY = l1.EndY;
                        l2.EndX = l1.EndX + round_gap / 2.0f;
                        l2.EndY = l1.EndY;
                        doc.Children.Add(l2);

                    }
                    
                    
                    
                    
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
                else if (teams[0] is Team1s)
                {
                    Team1s t = new Team1s();
                    t.ID = -1;
                    t.IsDummy = true;
                    teamsTemp.Add(t);
                }
                else if (teams[0] is Team3s)
                {
                    Team3s t = new Team3s();
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
                        matches_updated++;
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
