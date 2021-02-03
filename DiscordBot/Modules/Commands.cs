using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TourneyDiscordBotWPF;

namespace TourneyDiscordBot.Modules
{
    
    public class Commands :ModuleBase<SocketCommandContext>
    {
        public CommandService _commands { get; set; }
        public BotConfig _conf { get; set; }
        public DiscordDataService _data { get; set; }
        public TournamentChannelManager _tChannelMgr { get; set; }


        [Command("test")]
        public async Task Test()
        {
            var user = Context.User;
            await ReplyAsync(Context.User.Mention + " eisai ilithios");
        }


        private string textPrepend(string s1, string s2, char sep)
        {
            return s2 + sep + s1;
        }

        private void getParentGroupName(ref string s, char splitter, ModuleInfo m)
        {
            if (m.Group != null)
                s = textPrepend(s, m.Group, splitter);
            if (m.Parent != null)
                getParentGroupName(ref s, splitter, m.Parent);
        }

        private string getCommandName(CommandInfo cmd, char splitter)
        {
            string name = cmd.Name;
            string groupName = "";
            getParentGroupName(ref groupName, splitter, cmd.Module);
            if (groupName != "")
                return string.Join(splitter, new string[2] { groupName, name });
            return name;
        }
        
        [Command("help")]
        private async Task Help()
        {
            List<CommandInfo> commands = _commands.Commands.ToList();
            EmbedBuilder embedBuilder = new EmbedBuilder();

            foreach (CommandInfo command in commands)
            {
                // Get the command Summary attribute information
                string embedFieldText = command.Summary ?? "No description available\n";

                embedBuilder.AddField(_conf.Prefix + getCommandName(command, ' '), embedFieldText);
            }

            await ReplyAsync("Here's a list of commands and their description: ", false, embedBuilder.Build());
        }

        /*
        [Command("join")]
        public async Task Join(string rank)
        {
            await (new TournamentModule(_tourney, _tChannelMgr)).Join(rank);
            //await ReplyAsync(Context.User.Mention + " Did you mean !tourn join?");
        }
        */

        //[Group("tourn")]
        public class TournamentModule : ModuleBase<SocketCommandContext>
        {

            public DiscordDataService _data { get; set; }
            public TournamentChannelManager _tChannelMgr { get; set; }

            [Command("leave")]
            public async Task Unjoin()
            {
                var user = Context.User;
                await ReplyAsync(string.Format("User {0} left the tournament", Context.User.Username));
            }
            
            
            [Command("join")]
            public async Task Join(string rank)
            {
                if (!_data._tourney.RegistrationsEnabled)
                {
                    await ReplyAsync(string.Format("Sorry {0} Registrations are Closed!", Context.User.Mention));
                    return;
                }

                var user = Context.User;
                if (!rank.StartsWith("<:"))
                {
                    await ReplyAsync(user.Mention + "Unknown Command. Please use an emote to input your rank.");
                    return;
                }
                string rank_emote = rank.Split(':')[1];
                string rank_text = "";
                switch (rank_emote)
                {
                    case "b1":
                        rank_text = "Bronze I";
                        break;
                    case "b2":
                        rank_text = "Bronze II";
                        break;
                    case "b3":
                        rank_text = "Bronze III";
                        break;
                    case "s1":
                        rank_text = "Silver I";
                        break;
                    case "s2":
                        rank_text = "Silver II";
                        break;
                    case "s3":
                        rank_text = "Silver III";
                        break;
                    case "g1":
                        rank_text = "Gold I";
                        break;
                    case "g2":
                        rank_text = "Gold II";
                        break;
                    case "g3":
                        rank_text = "Gold III";
                        break;
                    case "p1":
                        rank_text = "Platinum I";
                        break;
                    case "p2":
                        rank_text = "Platinum II";
                        break;
                    case "p3":
                        rank_text = "Platinum III";
                        break;
                    case "d1":
                        rank_text = "Diamond I";
                        break;
                    case "d2":
                        rank_text = "Diamond II";
                        break;
                    case "d3":
                        rank_text = "Diamond III";
                        break;
                    case "c1":
                        rank_text = "Champion I";
                        break;
                    case "c2":
                        rank_text = "Champion II";
                        break;
                    case "c3":
                        rank_text = "Champion III";
                        break;
                    case "gc1":
                        rank_text = "Grand Champion I";
                        break;
                    case "gc2":
                        rank_text = "Grand Champion II";
                        break;
                    case "gc3":
                        rank_text = "Grand Champion III";
                        break;
                    case "ssl":
                        rank_text = "SuperSonic Legend";
                        break;
                    default:
                        rank_text = "None";
                        break;
                }

                //Check if player exists
                Player p = _data._tourney.getPlayerbyDiscordID(Context.User.Id);
                if ( p != null)
                {
                    var msg = await ReplyAsync(user.Mention + " You have already joined!");
                    await Task.Delay(500);
                    await msg.DeleteAsync();
                    return;
                }

                bool status = _data._tourney.CreatePlayer(Context.User.Username, rank_text, user.Id);

                if (!status)
                    await ReplyAsync(string.Format("Input rank {0} not found", rank));
                else
                {
                    //Assign Tournament Role to user
                    var u = user as SocketGuildUser;
                    await u.AddRoleAsync(Context.Guild.GetRole(_tChannelMgr.RoleID));
                    await ReplyAsync(string.Format("{0} has successfully joined the tournament", user.Mention));
                }

                await Task.Delay(500);
                await Context.Message.DeleteAsync();
            }

            public static void advance(SocketCommandContext _ctx, TournamentChannelManager channelMgr, Tournament t)
            {

                //This command should report to the accouncment channel
                var _channel = _ctx.Guild.GetChannel(channelMgr.AnnouncementChannelID) as ISocketMessageChannel;
                var _mgmtChannel = _ctx.Guild.GetChannel(channelMgr.ManagementChannelID) as ISocketMessageChannel;

                //Iterate in tournament rounds
                for (int i = 0; i < t.bracket.Rounds.Count; i++)
                {
                    Round r = t.bracket.Rounds[i];
                    int active_matchups = 0;
                    foreach (Matchup match in r.Matchups)
                    {
                        if (match.Winner != null)
                        {
                            continue; //Match is concluded continue;
                        }

                        if (match.IsDummy && match.Winner != null)
                        {
                            continue; //Match is dummy but the winner status is already resolved. Nothing to do here
                        }

                        if (match.InProgress)
                        {
                            active_matchups++; //Match has already started and we are waiting for a result
                            continue;
                        }

                        if (!match.IsValid)
                        {
                            active_matchups++; //Match is pending to be populated probably from previous round
                            continue;
                        }
                        else
                        {
                            //Send Announcement
                            bool t1_hasDisc = PlayerHasDiscord(match.Team1.Captain);
                            bool t2_hasDisc = PlayerHasDiscord(match.Team2.Captain);

                            string t1_captain = getPlayerDiscName(match.Team1.Captain, _ctx);
                            string t2_captain = getPlayerDiscName(match.Team2.Captain, _ctx);

                            if (t1_hasDisc && t2_hasDisc)
                            {
                                //Generate RL Lobby
                                Random rand_gen = new Random();
                                match.Lobby = new LobbyInfo();
                                match.Lobby.Name = "rlfriday" + rand_gen.Next(1, 200);
                                match.Lobby.Pass = rand_gen.Next(1000, 9999).ToString();

                                //Send DM to t1 captain
                                _ctx.Guild.GetUser(match.Team1.Captain.DiscordID).SendMessageAsync(string.Format("Create a Lobby with name: {0} and pass {1}. " +
                                    "Reply with !ready when you have created the lobby and I will send the credentials to the opposing team. Good luck!", 
                                    match.Lobby.Name, match.Lobby.Pass));
                                _ctx.Guild.GetUser(match.Team2.Captain.DiscordID).SendMessageAsync(string.Format("Your opponents are responsible for creating a lobby this match!" +
                                     " When the lobby is ready you will receive the lobby credentials." +
                                    " If anything goes wrong, you can contact the opposing team captain here {0}. Good luck!", t1_captain));

                                _channel.SendMessageAsync(string.Format("Round {0} - Match {1} | {2} vs {3} | Captains {4}, {5} check your dms",
                                                                        i, match.ID, match.Team1.Name, match.Team2.Name, t1_captain, t2_captain));
                            }
                            else
                            {
                                _channel.SendMessageAsync(string.Format("Round {0} - Match {1} | {2} vs {3} | Captains {4}, {5} discord comms not supported",
                                                                        i, match.ID, match.Team1.Name, match.Team2.Name, t1_captain, t2_captain));
                            }

                            match.InProgress = true;
                        }

                    }
                    if (active_matchups > 0) break;
                }

                //Check if tournament has finished
                Matchup final = t.bracket.Rounds.Last().Matchups.Last();
                if (final.Winner != null)
                {
                    var _role = _ctx.Guild.GetRole(channelMgr.RoleID);
                    _channel.SendMessageAsync(string.Format("{1}{0} Is the tournament champion! Thank you for participating and using the RLTourneys bot!",
                                                                        final.Winner.Name, _role.Mention));
                }
            }
            
            [Command("advance")]
            [Summary("Checks tournament progress")]
            public async Task Advance()
            {
                //This command should report to the accouncment channel
                var _mgmtChannel = Context.Guild.GetChannel(_tChannelMgr.ManagementChannelID) as ISocketMessageChannel;
                
                if (_data._tourney.bracket == null)
                {
                    await _mgmtChannel.SendMessageAsync(string.Format("Bracked not generated."));
                    return;
                }

                if (!_data._tourney.IsStarted)
                {
                    await _mgmtChannel.SendMessageAsync(string.Format("Tournament Not Started Yet."));
                    return;
                }

                TournamentModule.advance(Context, _tChannelMgr, _data._tourney);
            }

            public static bool checkIfContextUserAdmin(SocketCommandContext _ctx)
            {
                var u = _ctx.User as SocketGuildUser;

                if (u.GuildPermissions.Administrator)
                    return true;

                var msg = _ctx.Channel.SendMessageAsync("Admin Only Command").GetAwaiter().GetResult();
                System.Threading.Thread.Sleep(2000);
                msg.DeleteAsync().GetAwaiter().GetResult();
                return false;
            }

            public static string getPlayerDiscName(Player p, SocketCommandContext _ctx)
            {
                if (PlayerHasDiscord(p))
                    return _ctx.Guild.GetUser(p.DiscordID).Mention;
                return p.Name;
            }

            public static bool PlayerHasDiscord(Player p)
            {
                if (p.DiscordID != 0)
                    return true;
                return false;
            }

            
            private void reportResult(string status, string username, ulong disc_id)
            {
                //This command should report to the scorereport channel
                var _scoreChannel = Context.Guild.GetChannel(_tChannelMgr.ScoreReportChannelID) as ISocketMessageChannel;
                var _mgmtChannel = Context.Guild.GetChannel(_tChannelMgr.ManagementChannelID) as ISocketMessageChannel;

                //Find the Authors active matchup
                bool matchup_found = false;
                foreach (Round round in _data._tourney.bracket.Rounds)
                {
                    foreach (Matchup match in round.Matchups)
                    {
                        if (match.InProgress)
                        {
                            //Search for player discord ID. First message is sent always to the captain of the first team
                            if ((match.Team1.Captain.DiscordID == disc_id && disc_id != 0) || match.Team1.Captain.Name == username)
                            {
                                matchup_found = true;

                                if (status == "W")
                                    match.Team1ReportedWinner = match.Team1;
                                else
                                    match.Team1ReportedWinner = match.Team2;

                                //Notify Admins
                                _mgmtChannel.SendMessageAsync(string.Format("{0} reported {1} for Match {2} of Round {3}.",
                                                             getPlayerDiscName(match.Team1.Captain, Context), status, match.ID, round.ID));
                                
                                if (match.Team2ReportedWinner == null)
                                {
                                    _scoreChannel.SendMessageAsync(string.Format("{0} reported {1} for Match {2} of Round {3}. Waiting {4} to report result",
                                                             getPlayerDiscName(match.Team1.Captain, Context), status, match.ID, round.ID, getPlayerDiscName(match.Team2.Captain, Context)));
                                }

                            } else if ((match.Team2.Captain.DiscordID == disc_id && disc_id != 0) || match.Team2.Captain.Name == username)
                            {
                                matchup_found = true;

                                if (status == "W")
                                    match.Team2ReportedWinner = match.Team2;
                                else
                                    match.Team2ReportedWinner = match.Team1;

                                //Notify Admins
                                _mgmtChannel.SendMessageAsync(string.Format("{0} reported {1} for Match {2} of Round {3}.",
                                                             getPlayerDiscName(match.Team2.Captain, Context), status, match.ID, round.ID));

                                if (match.Team1ReportedWinner == null)
                                {
                                    _scoreChannel.SendMessageAsync(string.Format("{0} reported {1} for Match {2} of Round {3}. Waiting {4} to report result",
                                                             getPlayerDiscName(match.Team2.Captain, Context), status, match.ID, round.ID, getPlayerDiscName(match.Team1.Captain, Context)));
                                }
                            }

                            //Try to conclude match
                            if (match.Team2ReportedWinner != null && match.Team2ReportedWinner == match.Team1ReportedWinner && match.Winner == null)
                            {
                                match.Winner = match.Team1ReportedWinner;
                                _scoreChannel.SendMessageAsync(string.Format("Round {0} - Match {1} - {2} Won. Congratulations!",
                                                                 round.ID, match.ID, match.Winner.Name));
                                _mgmtChannel.SendMessageAsync(string.Format("Round {0} - Match {1} - {2} Won. Congratulations!",
                                                                 round.ID, match.ID, match.Winner.Name));
                                advance(Context, _tChannelMgr, _data._tourney); //Automatically advance tourney
                                return;

                            } else if(match.Team1ReportedWinner != null && match.Team2ReportedWinner!=null && match.Team2ReportedWinner != match.Team1ReportedWinner)
                            {
                                _scoreChannel.SendMessageAsync(string.Format("Round {0} - Match {1} - Different result reports detected. Tournament Manager has been notified to resolve the issue.",
                                                                 round.ID, match.ID));
                                _mgmtChannel.SendMessageAsync(string.Format("Round {0} - Match {1} : {2} vs {3} Wrong reports. Set the result manually",
                                                                 round.ID, match.ID, match.Team1.Name, match.Team2.Name));
                                advance(Context, _tChannelMgr, _data._tourney);//Automatically advance tourney
                                return;
                            }
                        }
                    }
                }

                if (!matchup_found)
                    _scoreChannel.SendMessageAsync(string.Format("No active match found for {0}", Context.User.Mention));


            }

            private void forceResult(string status, string username, ulong disc_id)
            {
                //This command should report to the scorereport channel
                var _scoreChannel = Context.Guild.GetChannel(_tChannelMgr.ScoreReportChannelID) as ISocketMessageChannel;
                var _mgmtChannel = Context.Guild.GetChannel(_tChannelMgr.ManagementChannelID) as ISocketMessageChannel;

                //Find the Authors active matchup
                bool matchup_found = false;
                foreach (Round round in _data._tourney.bracket.Rounds)
                {
                    foreach (Matchup match in round.Matchups)
                    {
                        if (match.InProgress)
                        {
                            //Search for player discord ID. First message is sent always to the captain of the first team
                            if ((match.Team1.Captain.DiscordID == disc_id && disc_id != 0) || match.Team1.Captain.Name == username)
                            {
                                matchup_found = true;

                                if (status == "W")
                                    match.Winner = match.Team1;
                                else
                                    match.Winner = match.Team2;

                                match.Team1ReportedWinner = match.Winner;
                                match.Team2ReportedWinner = match.Winner;
                                
                            } else if ((match.Team2.Captain.DiscordID == disc_id && disc_id != 0) || match.Team2.Captain.Name == username)
                            {
                                matchup_found = true;

                                if (status == "W")
                                    match.Winner = match.Team2;
                                else
                                    match.Winner = match.Team1;

                                match.Team1ReportedWinner = match.Winner;
                                match.Team2ReportedWinner = match.Winner;
                            }

                            if (matchup_found)
                            {
                                //If matchup found the winner has been set
                                _scoreChannel.SendMessageAsync(string.Format("Round {0} - Match {1} - Forced win for {2}.",
                                                                 round.ID, match.ID, match.Winner.Name));
                                _mgmtChannel.SendMessageAsync(string.Format("Round {0} - Match {1} - Forced win for {2}.",
                                                                 round.ID, match.ID, match.Winner.Name));
                                advance(Context, _tChannelMgr, _data._tourney); //Automatically advance tourney
                                return;
                            }
                        }
                    }
                }

                if (!matchup_found)
                    _scoreChannel.SendMessageAsync(string.Format("No active match found for {0}", Context.User.Mention));

            }



            [Command("report")]
            [Summary("Report Score Result")]
            public async Task Report(string status)
            {
                //This command should report to the scorereport channel
                var _channel = Context.Guild.GetChannel(_tChannelMgr.ScoreReportChannelID) as ISocketMessageChannel;

                if (status != "W" && status != "L")
                {
                    await _channel.SendMessageAsync(string.Format("{0} Wrong Match Report Status. Report **W** if you won or **L** if you lost", Context.User.Mention));
                    return;
                }

                reportResult(status, Context.User.Username, Context.User.Id);
            }

            [Command("report")]
            [Summary("Report Score Result")]
            public async Task Report(string status, string name)
            {
                //This command should report to the scorereport channel
                var _channel = Context.Guild.GetChannel(_tChannelMgr.ScoreReportChannelID) as ISocketMessageChannel;

                if (status != "W" && status != "L")
                {
                    await _channel.SendMessageAsync(string.Format("{0} Wrong Match Report Status. Report **W** if you won or **L** if you lost", Context.User.Mention));
                    return;
                }

                reportResult(status, name, 0);
            }

            [Command("forcereport")]
            [Summary("Force Report Score Result. (Admin Only)")]
            public async Task ForceReport(string status, string name)
            {

                if (!checkIfContextUserAdmin(Context))
                    return;

                if (status != "W" && status != "L")
                {
                    await Context.Channel.SendMessageAsync(string.Format("{0} Wrong Match Report Status. Report **W** if you won or **L** if you lost", Context.User.Mention));
                    return;
                }

                forceResult(status, name, 0);
            }

            [Command("forcereport")]
            [Summary("Force Report Score Result. (Admin Only)")]
            public async Task ForceReport(string status, IUser user)
            {

                if (!checkIfContextUserAdmin(Context))
                    return;

                if (status != "W" && status != "L")
                {
                    await Context.Channel.SendMessageAsync(string.Format("{0} Wrong Match Report Status. Report **W** if you won or **L** if you lost", Context.User.Mention));
                    return;
                }

                forceResult(status, user.Username, 0);
            }

            [Command("ready")]
            [Summary("Finalize Lobby Generation")]
            public async Task Ready()
            {
                
                foreach (Round round in _data._tourney.bracket.Rounds)
                {
                    foreach (Matchup match in round.Matchups)
                    {
                        if (match.InProgress)
                        {
                            //Search for player discord ID. First message is sent always to the captain of the first team
                            if (match.Team1.Captain.DiscordID == Context.User.Id)
                            {
                                //Send DM to t2 captain
                                await Context.Client.GetUser(match.Team2.Captain.DiscordID).SendMessageAsync(string.Format("Lobby has been created by {0} with name: {1} and pass {2}. Good Luck!",
                                                                                    Context.User.Mention, match.Lobby.Name, match.Lobby.Pass));
                                //Send DM to t1 captain
                                await Context.User.SendMessageAsync(string.Format("Good Luck!"));
                            }

                        }
                    }
                }

                
            }

            [Command("players")]
            [Summary("List Tournament Players")]
            public async Task Players()
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("Registered Players");

                foreach (Player p in _data._tourney.Players)
                {
                    string name = p.Name;
                    if (PlayerHasDiscord(p))
                        name = Context.Guild.GetUser(p.DiscordID).Mention;
                        builder.AddField("Player " + p.ID.ToString(),
                            name);    // true - for inline
                }

                builder.WithColor(Color.Red);
                
                //This command should be sent to the registration channel
                var _channel = Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID) as ISocketMessageChannel;
                await _channel.SendMessageAsync("", false, builder.Build());
            
            }

            private string getPlayerText(Player p)
            {
                return string.Format("**Player ID: ** {0} **Player Name: ** {1} **Rank:** {2}", p.ID, p.Name, p.Rank.Name);
            }

            [Command("player")]
            [Summary("Fetch Player Info")]
            public async Task Player(int id)
            {

                try
                {
                    Player p = _data._tourney.Players[id];
                    await ReplyAsync(getPlayerText(p));
                }
                catch (Exception e)
                {
                    await ReplyAsync(string.Format("Player {0} not found", id));
                }

            }

            private void createVCs()
            {
                foreach (Team t in _data._tourney.Teams)
                {
                    Context.Guild.CreateVoiceChannelAsync("TOURNAMENT TEAM " + t.ID);
                }
            }

            private void deleteVCs()
            {
                foreach (Team t in _data._tourney.Teams)
                {
                    foreach (var vc in Context.Guild.VoiceChannels)
                    {
                        if (vc.Name == "TOURNAMENT TEAM " + t.ID || vc.Name == "TOURNAMENT PLAYER POOL")
                        {
                            vc.DeleteAsync();
                        }
                    }
                }
            }
            
            private void deletePlayerRoles()
            {
                Context.Guild.GetRole(_tChannelMgr.RoleID).DeleteAsync();
                _tChannelMgr.RoleID = 0;
            }

            private void deleteTextChannels()
            {
                Context.Guild.GetChannel(_tChannelMgr.ManagementChannelID).DeleteAsync();
                
                if (_tChannelMgr.AnnouncementChannelID > 0)
                    Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID).DeleteAsync();
                if (_tChannelMgr.ScoreReportChannelID > 0)
                    Context.Guild.GetChannel(_tChannelMgr.ScoreReportChannelID).DeleteAsync();
                if (_tChannelMgr.RegistationChannelID > 0)
                    Context.Guild.GetChannel(_tChannelMgr.RegistationChannelID).DeleteAsync();
                Context.Guild.GetCategoryChannel(_tChannelMgr.CategoryChannelID).DeleteAsync();

                _tChannelMgr.ManagementChannelID = 0;
                _tChannelMgr.AnnouncementChannelID = 0;
                _tChannelMgr.ScoreReportChannelID = 0;
                _tChannelMgr.CategoryChannelID = 0;
            }

            [Command("create")]
            [Summary("Initialize Tournament")]
            public async Task CreateTourney(string type, string team_gen_method)
            {
                var u = Context.User as SocketGuildUser;

                if (!checkIfContextUserAdmin(Context))
                    return;
                
                //Dirty check to see if a tourney has already been created
                if (_tChannelMgr.AnnouncementChannelID != 0 && _tChannelMgr.AnnouncementChannelID != 0)
                {
                    await ReplyAsync(string.Format("Unable to create tournament (another tournament has already been created.)"));
                    return;
                }

                TournamentType _type;
                string generation;
                string start_text = "";
                string tour_format = "";
                switch (type)
                {
                    case "1s":
                        _type = TournamentType.SOLO;
                        start_text = _data._settings.textSettings.desc_1s_start;
                        tour_format = "1v1";
                        break;
                    case "2s":
                        _type = TournamentType.DOUBLES;
                        start_text = _data._settings.textSettings.desc_2s_start;
                        tour_format = "2v2";
                        break;
                    case "3s":
                        _type = TournamentType.TRIPLES;
                        start_text = _data._settings.textSettings.desc_3s_start;
                        tour_format = "3v3";
                        break;
                    default:
                        _type = TournamentType.DOUBLES; //Default choice
                        break;
                }
                string TeamGenDesc = "";
                string TeamMethodText = "";
                TournamentTeamGenMethod _method;
                switch (team_gen_method)
                {
                    case "random":
                        _method = TournamentTeamGenMethod.RANDOM;
                        TeamGenDesc = _data._settings.textSettings.goodnbaddesc;
                        TeamMethodText = "Good and Bad";
                        break;
                    case "register":
                        _method = TournamentTeamGenMethod.REGISTER;
                        TeamGenDesc = _data._settings.textSettings.fixeddesc;
                        TeamMethodText = "Fixed Teams";
                        break;
                    default:
                        _method = TournamentTeamGenMethod.RANDOM; //Default choice
                        break;
                }

                //Setup Tourney
                _data._tourney.Clear();
                _data._tourney.setTournamentType(_type);
                _data._tourney.setTournamentTeamGenMethod(_method);
                //await Context.Guild.CreateVoiceChannelAsync("TOURNAMENT PLAYER POOL");

                //Create Channel Category
                var channel_cat = await Context.Guild.CreateCategoryChannelAsync("RLFridays");

                //Create Text Channels
                var mgmt_channel = await Context.Guild.CreateTextChannelAsync("TOURNAMENT_MANAGEMENT");
                var reg_channel = await Context.Guild.CreateTextChannelAsync("TOURNAMENT_REGISTRATION");
                var ann_channel = await Context.Guild.CreateTextChannelAsync("TOURNAMENT_ANNOUNCEMENTS");

                await mgmt_channel.ModifyAsync(prop => prop.CategoryId = channel_cat.Id);
                await reg_channel.ModifyAsync(prop => prop.CategoryId = channel_cat.Id);
                await ann_channel.ModifyAsync(prop => prop.CategoryId = channel_cat.Id);

                //Set Channel Permissions
                await mgmt_channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole,
                        OverwritePermissions.DenyAll(mgmt_channel));

                await ann_channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole,
                        OverwritePermissions.DenyAll(ann_channel).Modify(viewChannel: PermValue.Allow, readMessageHistory: PermValue.Allow));

                //Store Channel IDs
                _tChannelMgr.AnnouncementChannelID = ann_channel.Id;
                _tChannelMgr.RegistationChannelID = reg_channel.Id;
                _tChannelMgr.ManagementChannelID = mgmt_channel.Id;
                _tChannelMgr.CategoryChannelID = channel_cat.Id;

                //mgmt_channel.PermissionOverwrites(prop=>)
                //mgmt_channel.ModifyAsync(prop=>prop.)

                //Create Tournament Role

                var permissions = new Discord.GuildPermissions();
                permissions.Modify(false, false, false, false, false, false, true,
                    false, true, true, true, true, false, false, true, false, false, true,
                    true, true, false, false, false, false, false, false, false, false, false, false);

                //permissions.Add(Discord.GuildPermission.ReadMessages);
                var role = await Context.Guild.CreateRoleAsync(_data._settings.textSettings.tournRoleName, permissions, Color.Blue, false, null);
                _tChannelMgr.RoleID = role.Id;

                EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                footerBuilder.Text = _data._settings.textSettings.embed_footer;
                
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("RLFriday " + DateTime.Now);
                builder.Description = start_text;
                builder.AddField("Tournament format", "***" + tour_format + "***");
                builder.AddField("Team generation", "***" + TeamMethodText + "***");
                builder.AddField("Πως φτίαχνονται οι ομάδες",  TeamGenDesc);
                builder.WithThumbnailUrl(_data._settings.textSettings.thumbnail_URL);
                builder.WithColor(Color.Blue);
                builder.Footer = footerBuilder;
                var msg = await ann_channel.SendMessageAsync("", false, builder.Build());

                
            }

            [Command("start")]
            [Summary("Create Team Voice Channels")]
            public async Task StartTourney()
            {
                var u = Context.User as SocketGuildUser;
                if (!checkIfContextUserAdmin(Context))
                    return;

                if (_data._tourney.bracket == null)
                {
                    await Context.Channel.SendMessageAsync(string.Format("Bracked not generated."));
                    return;
                }
                //Delete Registration Channel
                await Context.Guild.GetChannel(_tChannelMgr.RegistationChannelID).DeleteAsync();
                _tChannelMgr.RegistationChannelID = 0;

                //Create Score Report Channel
                var score_chl = await Context.Guild.CreateTextChannelAsync("TOURNAMENT_SCORE_REPORT");
                await score_chl.ModifyAsync(prop => prop.CategoryId = _tChannelMgr.CategoryChannelID);
                _tChannelMgr.ScoreReportChannelID = score_chl.Id;
                
                //createVCs(); TODO
                var ann_chl = Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID) as SocketTextChannel;
                var role = Context.Guild.GetRole(_tChannelMgr.RoleID);
                await ann_chl.SendMessageAsync(string.Format("{0} Tournament Has Officially Started!. Use {1} to report your match results",
                                               role.Mention, score_chl.Mention));

                lock (_data._tourney)
                {
                    _data._tourney.IsStarted = true;
                }

                advance(Context, _tChannelMgr, _data._tourney); //Automatically advance tourney
                if (_data._tourney.Type == TournamentType.SOLO)
                {
                    return;
                }

                foreach (Team t in _data._tourney.Teams)
                {
                    string name = t.Name;
                    var vc = await Context.Guild.CreateVoiceChannelAsync(name);
                }

            }

            [Command("end")]
            [Summary("End Tournament")]
            public async Task EndTourney()
            {
                if (!checkIfContextUserAdmin(Context))
                {
                    var msg1 = await ReplyAsync(string.Format("You have no permission to execute this command."));
                    System.Threading.Thread.Sleep(5000);
                    await Context.Channel.DeleteMessageAsync(msg1.Id);
                    return;
                }
                
                deleteTextChannels();
                deleteVCs();
                deletePlayerRoles();
                await ReplyAsync("Tournament Ended *GGs*");
            }

            [Group("teams")]
            public class TeamsModule : ModuleBase<SocketCommandContext>
            {

                public DiscordDataService _data { get; set; }
                public TournamentChannelManager _tChannelMgr { get; set; }

                [Command("generate")]
                [Summary("Generate Teams")]
                public async Task Generate()
                {
                    if (_data._tourney._players.Count == 0)
                    {
                        await ReplyAsync("No Players Found.");
                        return;
                    }

                    if (_data._tourney.RegistrationsEnabled)
                    {
                        await ReplyAsync("Cannot Generate Teams, registrations are still open!");
                        return;
                    }

                    if (_data._tourney.TeamGenMethod != TournamentTeamGenMethod.RANDOM)
                    {
                        await ReplyAsync("Cannot Generate Teams, tournament is set to team registration mode");
                        return;
                    }

                    _data._tourney.CreateTeams();
                    await ReplyAsync("Teams Successfully Generated");
                    await Post();
                }
                

                public static bool registerTeam(SocketCommandContext _ctx, Tournament _tourney, Player p, string name)
                {
                    if (_tourney.Type == TournamentType.SOLO)
                    {
                        Team1s t = new Team1s();
                        t.Name = name;
                        t.addPlayer(p);
                        _tourney._teams.Add(t);
                        return true;
                    }

                    if (_tourney.Type == TournamentType.DOUBLES)
                    {
                        Team2s t = new Team2s();
                        t.Name = name;
                        t.addPlayer(p);
                        _tourney._teams.Add(t);
                        return true;
                    }

                    if (_tourney.Type == TournamentType.TRIPLES)
                    {
                        Team3s t = new Team3s();
                        t.Name = name;
                        t.addPlayer(p);
                        _tourney._teams.Add(t);
                        return true;
                    }

                    return false;
                }

                [Command("register")]
                [Summary("Register Team")]
                public async Task Register(string name)
                {
                    if (!_data._tourney.RegistrationsEnabled)
                    {
                        await ReplyAsync("Registrations are closed. Wait for an announcement!");
                        return;
                    }

                    //Make sure players exist
                    Player p = _data._tourney.getPlayerbyDiscordID(Context.User.Id);
                    
                    if (p == null)
                    {
                        await ReplyAsync("Players do not exist, make sure to join the tourney first!");
                        return;
                    }

                    if (p.team != null)
                    {
                        await ReplyAsync("You are already a member of a team. You cannot register a new team");
                        return;
                    }

                    if (registerTeam(Context, _data._tourney, p, name))
                    {
                        await ReplyAsync("Team Successfully Generated");
                    }
                }

                [Command("invite")]
                [Summary("Invite Player to team")]
                public async Task Invite(IUser u)
                {
                    if (!_data._tourney.RegistrationsEnabled)
                    {
                        await ReplyAsync("Registrations are closed. Wait for an announcement!");
                        return;
                    }

                    //Make sure players exist
                    Player p = _data._tourney.getPlayerbyDiscordID(Context.User.Id);


                    //Make sure players exist
                    Player p_inv = _data._tourney.getPlayerbyDiscordID(u.Id);

                    if (p_inv == null)
                    {
                        await ReplyAsync("Player does not exist, make sure to invite players that have joined the tournament!");
                        return;
                    }

                    TeamInvitation teamInvite = new TeamInvitation();
                    teamInvite.team = p.team;


                    int invitationId = p_inv.Invitations.Count;
                    p_inv.Invitations.Add(teamInvite);

                    //Send invitation
                    await u.SendMessageAsync(string.Format("You have been invited by {0} to join team {1}. If you want to accept this invitation reply with ```!teams accept {2}```. If you want to reject this invitation reply with ```!teams reject {2}```",
                        Context.User.Mention, p.team.Name, invitationId));
                }

                [Command("accept")]
                [Summary("Accept Team Invitation")]
                public async Task accept(int invitationID)
                {
                    //Make sure players exist

                    Player p = _data._tourney.getPlayerbyDiscordID(Context.User.Id);

                    if (p == null)
                    {
                        await ReplyAsync("You have not joined the tournament yet. Make sure to join first!");
                        return;
                    }

                    TeamInvitation teamInvite = p.Invitations[invitationID];
                    p.acceptInvitation(teamInvite);

                    //Send invitation
                    await Context.User.SendMessageAsync(string.Format("You have joined team \"{0}\".", teamInvite.team.Name));
                }

                [Command("reject")]
                [Summary("Reject Team Invitation")]
                public async Task reject(int invitationID)
                {
                    //Make sure players exist

                    Player p = _data._tourney.getPlayerbyDiscordID(Context.User.Id);

                    if (p == null)
                    {
                        await ReplyAsync("You have not joined the tournament yet. Make sure to join first!");
                        return;
                    }

                    TeamInvitation teamInvite = p.Invitations[invitationID];
                    p.rejectInvitation(teamInvite);

                    //Send invitation
                    await Context.User.SendMessageAsync(string.Format("You have rejected the invitation from team {0}.", teamInvite.team.Name));
                }

                [Command("post")]
                [Summary("Announce Teams")]
                public async Task Post()
                {
                    EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                    footerBuilder.Text = "EGG Gang - Zimarulis - RL Fridays";

                    EmbedBuilder builder = new EmbedBuilder();
                    builder.WithTitle("Registered Teams");
                    builder.WithDescription("Επικοινωνήστε με τους συμπαίκτες σας και βρείτε τους in game.");
                    builder.WithThumbnailUrl(_data._settings.textSettings.thumbnail_URL);

                    foreach (Team t in _data._tourney._teams)
                    {
                        List<string> mentions = new List<string>();
                        for (int i = 0; i < t.Players.Count; i++)
                        {
                            Player p = t.Players[i];
                            if (p == null)
                                mentions.Add("EMPTY_SLOT");
                            else
                                mentions.Add(getPlayerDiscName(p, Context) + _data.emoteMap(p.Rank._rank));
                        }
                        builder.AddField(t.Name, string.Join(" | ", mentions),false);
                    }

                    builder.WithColor(Color.Red);

                    //This command should be sent to the registration channel
                    var _channel = Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID) as ISocketMessageChannel;
                    await _channel.SendMessageAsync("", false, builder.Build());
                }

                [Command("info")]
                [Summary("Fetch Team Info")]
                public async Task Info(int id)
                {
                    try
                    {
                        Team t = _data._tourney.Teams[id];
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithTitle(t.Name);
                        
                        for (int i = 0; i < t.Players.Count; i++)
                        {
                            Player p = t.Players[i];
                            if (i == 0)
                            {
                                builder.AddField("Player " + i, TournamentModule.getPlayerDiscName(p, Context), true);
                            } else
                                builder.AddField("Player " + i, TournamentModule.getPlayerDiscName(p, Context), true);
                        }

                        builder.WithColor(Color.Red);

                        //This command should be sent to the registration channel
                        await Context.Channel.SendMessageAsync("", false, builder.Build());

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync(string.Format("Team {0} not found", id));
                    }
                }
            }
            
            [Group("bracket")]
            public class BracketModule : ModuleBase<SocketCommandContext>
            {

                public DiscordDataService _data { get; set; }
                public TournamentChannelManager _tChannelMgr { get; set; }

                [Command("generate")]
                [Summary("Generate Bracket")]
                public async Task Generate()
                {
                    if (_data._tourney._teams.Count == 0)
                    {
                        await ReplyAsync("Please Generate Teams first");
                        return;
                    }
                    _data._tourney.CreateBracket();
                    await ReplyAsync("Bracket Successfully Generated");
                    await Show();
                }

                [Command("post")]
                [Summary("Post Bracket to Discord")]
                public async Task Show()
                {
                    _data._tourney.bracket.GenerateSVG();
                    var _channel = Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID) as SocketTextChannel;
                    EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                    footerBuilder.Text = _data._settings.textSettings.embed_footer;

                    var picture = await Context.Channel.SendFileAsync(@"bracket.png");

                    string imgurl = picture.Attachments.First().Url;
                    _channel = Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID) as SocketTextChannel;


                    EmbedBuilder BracketMessage = new EmbedBuilder();
                    BracketMessage.WithAuthor("RL Fridays");
                    BracketMessage.WithTitle("Bracket");
                    BracketMessage.Description = "Το bracket δημιουργήθηκε. Σύντομα θα ξεκινήσει ο επόμενος γύρος!";
                    BracketMessage.WithImageUrl(imgurl);
                    BracketMessage.WithThumbnailUrl("https://cdn.discordapp.com/avatars/454232504182898689/ed55a0711ba007be7cfb11b1fa3e2075.png?size=128");
                    BracketMessage.WithColor(Color.Blue);
                    BracketMessage.Footer = footerBuilder;
                    var msg = await _channel.SendMessageAsync("", false, BracketMessage.Build());
                }
            }

            [Group("registration")]
            public class RegistrationModule : ModuleBase<SocketCommandContext>
            {
                public DiscordDataService _data { get; set; }
                public TournamentChannelManager _tChannelMgr { get; set; }

                [Command("open")]
                [Summary("Enables Registrations")]
                public async Task Open()
                {
                    if (!checkIfContextUserAdmin(Context))
                        return;

                    _data._tourney.RegistrationsEnabled = true;
                    //Make announcement
                    //This command should be sent to the registration channel
                    EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                    footerBuilder.Text = "EGG Gang - Zimarulis - RL Fridays";
                    EmbedBuilder RegMessage = new EmbedBuilder();
                    RegMessage.WithTitle("Οι εγγραφές άνοιξαν!");
                    RegMessage.Description = "Οι εγγραφές άνοιξαν και θα παραμείνουν ανοιχτές μέχρι την έναρξη του τουρνουά στις 14:30! Θα ενημερωθείς με αντίστοιχο μύνημα όταν οι ομάδες φτιαχτούν.";
                    RegMessage.AddField("Πως δηλώνω συμμετοχή", "Δήλωσε συμμετοχή γράφοντας !join (emote του rank) όπως στην παρακάτω εικόνα", false);    // true - for inline
                    RegMessage.WithImageUrl("https://cdn.discordapp.com/attachments/805516317837885460/805516628505919488/unknown.png");
                    RegMessage.WithThumbnailUrl("https://cdn.discordapp.com/avatars/454232504182898689/ed55a0711ba007be7cfb11b1fa3e2075.png?size=128");
                    //builder.AddField("AOE", "63", true);
                    //builder.WithThumbnailUrl("https://static.wikia.nocookie.net/rocketleague/images/7/7a/Halo_topper_icon.png/revision/latest/scale-to-width-down/256?cb=20200422210226");
                    RegMessage.WithColor(Color.Blue);
                    RegMessage.Footer = footerBuilder;
                    var _channel = Context.Guild.GetChannel(_tChannelMgr.RegistationChannelID) as ISocketMessageChannel;
                    var msg = await _channel.SendMessageAsync("", false, RegMessage.Build());

                    //await _channel.SendMessageAsync("Tournament Registrations are Open!");
                }

                [Command("close")]
                [Summary("Close Registrations")]
                public async Task Close()
                {

                    _data._tourney.RegistrationsEnabled = false;
                    //Make announcement
                    //This command should be sent to the registration channel
                    var _channel = Context.Guild.GetChannel(_tChannelMgr.RegistationChannelID) as ISocketMessageChannel;
                    EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                    footerBuilder.Text = _data._settings.textSettings.embed_footer;
                    EmbedBuilder RegClosed = new EmbedBuilder();
                    RegClosed.WithTitle("RLFriday " + DateTime.Now);
                    RegClosed.Description = "Registrations are closed! The bracket is going to be generated soon!";
                    string names = "";
                    foreach (Player p in _data._tourney.Players)
                    {
                        string name = p.Name;
                        if (PlayerHasDiscord(p))
                        name = Context.Guild.GetUser(p.DiscordID).Mention;
                        names = names + " " + name;
                    }
                    RegClosed.AddField("Registered players", names );
                    RegClosed.WithThumbnailUrl(_data._settings.textSettings.thumbnail_URL);
                    RegClosed.WithColor(Color.Blue);
                    RegClosed.Footer = footerBuilder;
                    var msg = await _channel.SendMessageAsync("", false, RegClosed.Build());
                }

            }


        }


    }
}
