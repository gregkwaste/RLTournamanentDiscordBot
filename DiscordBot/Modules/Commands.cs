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
        public Tournament _tourney { get; set; }
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

            public Tournament _tourney { get; set; }
            public TournamentChannelManager _tChannelMgr { get; set; }

            public TournamentModule(Tournament t, TournamentChannelManager tc) : base()
            {
                _tourney = t;
                _tChannelMgr = tc;
            }


            [Command("leave")]
            public async Task Unjoin()
            {
                var user = Context.User;
                await ReplyAsync(string.Format("User {0} left the tournament", Context.User.Username));
            }


            [Command("join")]
            public async Task Join(string rank)
            {
                if (!_tourney.RegistrationsEnabled)
                {
                    await ReplyAsync(string.Format("Sorry {0} Registrations are Closed!", Context.User.Mention));
                    return;
                }

                var user = Context.User;
                if (!rank.StartsWith("<:"))
                {
                    await ReplyAsync(user.Mention + " Pws ta gamhses ola, ksanakane join karamalaka");
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

                bool status = _tourney.CreatePlayer(Context.User.Username, rank_text, user.Id);

                if (!status)
                    await ReplyAsync(string.Format("Input rank {0} not found", rank));
                else
                {
                    //Assign Tournament Role to user
                    var u = user as SocketGuildUser;
                    await u.AddRoleAsync(Context.Guild.GetRole(_tChannelMgr.RoleID));
                    var msg = await ReplyAsync(string.Format("{0} has successfully joined the tournament", user.Mention));
                    await Task.Delay(500);
                    await msg.DeleteAsync();
                    //user.SendMessageAsync("Mpes tournoua na se ksekolliasoume");
                }

            }


            [Command("advance")]
            [Summary("Checks tournament progress")]
            public async Task Advance()
            {
                //This command should report to the accouncment channel
                var _channel = Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID) as ISocketMessageChannel;
                
                if (_tourney.bracket == null)
                {
                    await _channel.SendMessageAsync(string.Format("Bracked not generated."));
                    return;
                }

                if (!_tourney.IsStarted)
                {
                    await _channel.SendMessageAsync(string.Format("Tournament Not Started Yet."));
                    return;
                }

                //Iterate in tournament rounds
                for (int i = 0; i < _tourney.bracket.Rounds.Count; i++){
                    Round r = _tourney.bracket.Rounds[i];
                    int active_matchups = 0;
                    foreach (Matchup match in r.Matchups)
                    {
                        if (match.Winner!= null)
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
                            bool t1_hasDisc = false;
                            bool t2_hasDisc = false;


                            string t1_captain = match.Team1.Captain.Name;
                            string t2_captain = match.Team2.Captain.Name;

                            if (match.Team1.Captain.DiscordID != 0xFFFFFFFFFFFF)
                            {
                                t1_captain = Context.Guild.GetUser(match.Team1.Captain.DiscordID).Mention;
                                t1_hasDisc = true;
                            }
                                
                            if (match.Team2.Captain.DiscordID != 0xFFFFFFFFFFFF)
                            {
                                t2_captain = Context.Guild.GetUser(match.Team2.Captain.DiscordID).Mention;
                                t2_hasDisc = true;
                            }
                            
                            if (t1_hasDisc && t2_hasDisc)
                            {
                                //Generate RL Lobby
                                Random rand_gen = new Random();
                                match.Lobby = new LobbyInfo();
                                match.Lobby.Name = "rlfriday" + rand_gen.Next(1, 200);
                                match.Lobby.Pass = rand_gen.Next(1000, 9999).ToString();

                                //Send DM to t1 captain
                                await Context.Guild.GetUser(match.Team1.Captain.DiscordID).SendMessageAsync(string.Format("The tournament has officially started. Create a Lobby with name: {0} and pass {1}. Reply with !ready when you have created the lobby and I will send the credentials to the opposing team. Good luc!",
                                                                                    match.Lobby.Name, match.Lobby.Pass));
                                await Context.Guild.GetUser(match.Team2.Captain.DiscordID).SendMessageAsync(string.Format("The tournament has officially started. Your opponents are responsible for creating a lobby this match!" +
                                    " When the lobby is ready I will send you the lobby credentials." +
                                    " If anything goes wrong, you can contact the opposing team captain here " + t1_captain + ". Good luck!"));
                                
                                await _channel.SendMessageAsync(string.Format("Round {0} - Match {1} | Team {2} vs Team {3} | Captains {4}, {5} check your dms",
                                                                        i, match.ID, match.Team1.ID, match.Team2.ID, t1_captain, t2_captain));
                            } else
                            {
                                await _channel.SendMessageAsync(string.Format("Round {0} - Match {1} | Team {2} vs Team {3} | Captains {4}, {5} discord comms not supported",
                                                                        i, match.ID, match.Team1.ID, match.Team2.ID, t1_captain, t2_captain));
                            }

                            match.InProgress = true;
                        }
                        
                    }
                    if (active_matchups > 0) break;
                }

                //Check if tournament has finished

                Matchup final = _tourney.bracket.Rounds.Last().Matchups.Last();
                if (final.Winner != null)
                {

                    await _channel.SendMessageAsync(string.Format("DING DING DING WE HAVE A WINNER!!!!! Congrats to Team {0} for winning the tournament!",
                                                                        final.Winner.ID));
                }
               
            }

            public static bool checkIfContextUserAdmin(SocketCommandContext _ctx)
            {
                var u = _ctx.User as SocketGuildUser;

                if (u.GuildPermissions.Administrator)
                    return true;
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
                if (p.DiscordID != 0xFFFFFFFFFFFF)
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
                foreach (Round round in _tourney.bracket.Rounds)
                {
                    foreach (Matchup match in round.Matchups)
                    {
                        if (match.InProgress)
                        {
                            //Search for player discord ID. First message is sent always to the captain of the first team
                            if ((match.Team1.Captain.DiscordID == disc_id && disc_id != 0xFFFFFFFFFFFF) || match.Team1.Captain.Name == username)
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

                            }

                            if ((match.Team2.Captain.DiscordID == disc_id && disc_id != 0xFFFFFFFFFFFF) || match.Team2.Captain.Name == username)
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
                                _scoreChannel.SendMessageAsync(string.Format("Round {0} - Match {1} - Team {2} Won. Congratulations!",
                                                                 round.ID, match.ID, match.Winner.ID));
                                _mgmtChannel.SendMessageAsync(string.Format("Round {0} - Match {1} - Team {2} Won. Congratulations!",
                                                                 round.ID, match.ID, match.Winner.ID));
                            } else if(match.Team1ReportedWinner != null && match.Team2ReportedWinner!=null && match.Team2ReportedWinner != match.Team1ReportedWinner)
                            {
                                _scoreChannel.SendMessageAsync(string.Format("Round {0} - Match {1} - Different result reports detected. Tournament Manager has been notified to resolve the issue.",
                                                                 round.ID, match.ID));
                                _mgmtChannel.SendMessageAsync(string.Format("Round {0} - Match {1} : Team {2} vs Team {3} Wrong reports. Set the result manually",
                                                                 round.ID, match.ID, match.Team1.ID, match.Team2.ID));
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
                foreach (Round round in _tourney.bracket.Rounds)
                {
                    foreach (Matchup match in round.Matchups)
                    {
                        if (match.InProgress)
                        {
                            //Search for player discord ID. First message is sent always to the captain of the first team
                            if ((match.Team1.Captain.DiscordID == disc_id && disc_id != 0xFFFFFFFFFFFF) || match.Team1.Captain.Name == username)
                            {
                                matchup_found = true;

                                if (status == "W")
                                    match.Winner = match.Team1;
                                else
                                    match.Winner = match.Team2;

                                match.Team1ReportedWinner = match.Winner;
                                match.Team2ReportedWinner = match.Winner;
                            }

                            if ((match.Team2.Captain.DiscordID == disc_id && disc_id != 0xFFFFFFFFFFFF) || match.Team2.Captain.Name == username)
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
                                _scoreChannel.SendMessageAsync(string.Format("Round {0} - Match {1} - Forced win for Team {2}.",
                                                                 round.ID, match.ID, match.Winner.ID));
                                _mgmtChannel.SendMessageAsync(string.Format("Round {0} - Match {1} - Forced win for Team {2}.",
                                                                 round.ID, match.ID, match.Winner.ID));
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

                reportResult(status, name, 0xFFFFFFFFFFFF);
            }

            [Command("forcereport")]
            [Summary("Force Report Score Result. (Admin Only)")]
            public async Task ForceReport(string status, string name)
            {

                if (!checkIfContextUserAdmin(Context))
                {
                    var msg = await Context.Channel.SendMessageAsync("Admin Only Command");
                    System.Threading.Thread.Sleep(2000);
                    await msg.DeleteAsync();
                    return;
                }

                if (status != "W" && status != "L")
                {
                    await Context.Channel.SendMessageAsync(string.Format("{0} Wrong Match Report Status. Report **W** if you won or **L** if you lost", Context.User.Mention));
                    return;
                }

                forceResult(status, name, 0xFFFFFFFFFFFF);
            }

            [Command("forcereport")]
            [Summary("Force Report Score Result. (Admin Only)")]
            public async Task ForceReport(string status, IUser user)
            {

                if (!checkIfContextUserAdmin(Context))
                {
                    var msg = await Context.Channel.SendMessageAsync("Admin Only Command");
                    System.Threading.Thread.Sleep(2000);
                    await msg.DeleteAsync();
                    return;
                }

                if (status != "W" && status != "L")
                {
                    await Context.Channel.SendMessageAsync(string.Format("{0} Wrong Match Report Status. Report **W** if you won or **L** if you lost", Context.User.Mention));
                    return;
                }

                forceResult(status, user.Username, 0xFFFFFFFFFFFF);
            }

            [Command("ready")]
            [Summary("Finalize Lobby Generation")]
            public async Task Ready()
            {
                
                foreach (Round round in _tourney.bracket.Rounds)
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

                foreach (Player p in _tourney.Players)
                {
                    string name = p.Name;
                    if (p.DiscordID != 0xFFFFFFFFFFFF)
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
                    Player p = _tourney.Players[id];
                    await ReplyAsync(getPlayerText(p));
                }
                catch (Exception e)
                {
                    await ReplyAsync(string.Format("Player {0} not found", id));
                }

            }

            private void createVCs()
            {
                foreach (Team t in _tourney.Teams)
                {
                    Context.Guild.CreateVoiceChannelAsync("TOURNAMENT TEAM " + t.ID);
                }
            }

            private void deleteVCs()
            {
                foreach (Team t in _tourney.Teams)
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
                _tChannelMgr.RoleID = 0xFFFFFFFFFFFFFFFF;
            }

            private void deleteTextChannels()
            {
                Context.Guild.GetChannel(_tChannelMgr.ManagementChannelID).DeleteAsync();
                
                if (_tChannelMgr.AnnouncementChannelID > 0)
                    Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID).DeleteAsync();
                if (_tChannelMgr.ScoreReportChannelID > 0)
                    Context.Guild.GetChannel(_tChannelMgr.ScoreReportChannelID).DeleteAsync();
                
                Context.Guild.GetCategoryChannel(_tChannelMgr.CategoryChannelID).DeleteAsync();

                _tChannelMgr.ManagementChannelID = 0xFFFFFFFFFFFFFFFF;
                _tChannelMgr.AnnouncementChannelID = 0xFFFFFFFFFFFFFFFF;
                _tChannelMgr.ScoreReportChannelID = 0xFFFFFFFFFFFFFFFF;
                _tChannelMgr.CategoryChannelID = 0xFFFFFFFFFFFFFFFF;
            }

            [Command("create")]
            [Summary("Initialize Tournament")]
            public async Task CreateTourney()
            {
                var u = Context.User as SocketGuildUser;

                if (!checkIfContextUserAdmin(Context))
                {
                    var msg1 = await ReplyAsync(string.Format("Isa mwrh saloufa thes na kaneis kai create"));
                    System.Threading.Thread.Sleep(5000);
                    await Context.Channel.DeleteMessageAsync(msg1.Id);
                    return;
                }
                
                //Dirty check to see if a tourney has already been created
                if (_tChannelMgr.AnnouncementChannelID != 0xFFFFFFFFFFFFFFFF && _tChannelMgr.AnnouncementChannelID != 0)
                {
                    await ReplyAsync(string.Format("Unable to create tournament (another tournament has already been created.)"));
                    return;
                }

                _tourney.Clear();
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
                var role = await Context.Guild.CreateRoleAsync("RLFridays", permissions, Discord.Color.Blue, false, null);
                _tChannelMgr.RoleID = role.Id;

                EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                footerBuilder.Text = "EGG Gang - Zimarulis - RL Fridays";

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("RLFriday " + DateTime.Now);
                builder.Description = "Zimarulis RLFriday 2vs2 tournament. " +
                    "Τουρνουά Rocket League 2v2 με format good and bad. Για να είναι οι ομάδες δίκαιες και να δίνεται η ίδια δυνατότητα σε όλους" +
                    " να κερδίσουν, οι ομάδες φτιάχνονται με βάση το rank των παικτών βάζοντας στην ίδια ομάδα έναν παίκτη υψηλού rank και έναν χαμηλού rank.\n";
                builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/454232504182898689/ed55a0711ba007be7cfb11b1fa3e2075.png?size=128");
                builder.WithColor(Color.Blue);
                builder.Footer = footerBuilder;
                var msg = await ann_channel.SendMessageAsync("", false, builder.Build());

                //Add Rank Reactions

                /*
                await msg.AddReactionAsync(Emote.Parse("<:b1:804811079922090004>"));
                await msg.AddReactionAsync(Emote.Parse("<:b2:804811082748788756>"));
                await msg.AddReactionAsync(Emote.Parse("<:b3:804811087182299146>"));
                await msg.AddReactionAsync(Emote.Parse("<:s1:804811086108688414>"));
                await msg.AddReactionAsync(Emote.Parse("<:s2:804811087181512754>"));
                await msg.AddReactionAsync(Emote.Parse("<:s3:804811087601074229>"));
                await msg.AddReactionAsync(Emote.Parse("<:g1:804811088864477206>"));
                await msg.AddReactionAsync(Emote.Parse("<:g2:804811092206288936>"));
                await msg.AddReactionAsync(Emote.Parse("<:g3:804811090550587414>"));
                await msg.AddReactionAsync(Emote.Parse("<:p1:804811088792780840>"));
                await msg.AddReactionAsync(Emote.Parse("<:p2:804811089748688917>"));
                await msg.AddReactionAsync(Emote.Parse("<:p3:804811089564401746>"));
                await msg.AddReactionAsync(Emote.Parse("<:d1:804811087856795679>"));
                await msg.AddReactionAsync(Emote.Parse("<:d2:804811089434902588>"));
                await msg.AddReactionAsync(Emote.Parse("<:d3:804811090185814026>"));
                await msg.AddReactionAsync(Emote.Parse("<:c1:804811087635415081>"));
                await msg.AddReactionAsync(Emote.Parse("<:c2:804811087756525588>"));
                await msg.AddReactionAsync(Emote.Parse("<:c3:804811089632165968>"));
                await msg.AddReactionAsync(Emote.Parse("<:gc1:804811090495930388>"));
                await msg.AddReactionAsync(Emote.Parse("<:gc2:804811090339954799>"));
                await msg.AddReactionAsync(Emote.Parse("<:gc3:804811090474434571>"));
                await msg.AddReactionAsync(Emote.Parse("<:ssl:804811088591978537>"));
                */
            }

            [Command("start")]
            [Summary("Create Team Voice Channels")]
            public async Task StartTourney()
            {
                var u = Context.User as SocketGuildUser;
                if (!checkIfContextUserAdmin(Context))
                {
                    var msg1 = await ReplyAsync(string.Format("Isa mwrh saloufa thes na kaneis kai start"));
                    System.Threading.Thread.Sleep(5000);
                    await Context.Channel.DeleteMessageAsync(msg1.Id);
                    return;
                }

                if (_tourney.bracket == null)
                {
                    await Context.Channel.SendMessageAsync(string.Format("Bracked not generated."));
                    return;
                }

                //Delete Registration Channel
                await Context.Guild.GetChannel(_tChannelMgr.RegistationChannelID).DeleteAsync();
                _tChannelMgr.RegistationChannelID = 0xFFFFFFFFFFFFFFFF;

                //Create Score Report Channel
                var score_chl = await Context.Guild.CreateTextChannelAsync("TOURNAMENT_SCORE_REPORT");
                await score_chl.ModifyAsync(prop => prop.CategoryId = _tChannelMgr.CategoryChannelID);
                _tChannelMgr.ScoreReportChannelID = score_chl.Id;
                
                //createVCs(); TODO
                var ann_chl = Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID) as SocketTextChannel;
                var role = Context.Guild.GetRole(_tChannelMgr.RoleID);
                await ann_chl.SendMessageAsync(string.Format("{0} Tournament Has Officially Started!. Use {1} to report your match results",
                                               role.Mention, score_chl.Mention));

                lock (_tourney)
                {
                    _tourney.IsStarted = true;
                }
                
            }

            [Command("end")]
            [Summary("End Tournament")]
            public async Task EndTourney()
            {
                if (!checkIfContextUserAdmin(Context))
                {
                    var msg1 = await ReplyAsync(string.Format("Isa mwrh saloufa thes na kaneis kai end"));
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

                public Tournament _tourney { get; set; }
                public TournamentChannelManager _tChannelMgr { get; set; }

                [Command("generate")]
                [Summary("Generate Teams")]
                public async Task Generate()
                {
                    if (_tourney._players.Count == 0)
                    {
                        await ReplyAsync("No Players Found.");
                        return;
                    }

                    if (_tourney.RegistrationsEnabled)
                    {
                        await ReplyAsync("Cannot Generate Teams, registrations are still open!");
                        return;
                    }

                    _tourney.CreateTeams();
                    await ReplyAsync("Teams Successfully Generated");
                    await Post();
                }

                [Command("post")]
                [Summary("Announce Teams")]
                public async Task Post()
                {
                    EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                    footerBuilder.Text = "EGG Gang - Zimarulis - RL Fridays";

                    EmbedBuilder builder = new EmbedBuilder();
                    builder.WithTitle("Registered Teams");
                    builder.WithDescription("Οι ομάδες δημιουργήθηκαν. Επικοινωνήστε με τους συμπαίκτες σας και βρείτε τους in game.");
                    builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/454232504182898689/ed55a0711ba007be7cfb11b1fa3e2075.png?size=128");

                    foreach (Team t in _tourney._teams)
                    {
                        
                        
                        List<string> mentions = new List<string>();
                        for (int i = 0; i < t.Players.Count; i++)
                        {
                            Player p = t.Players[i];
                            mentions.Add(TournamentModule.getPlayerDiscName(p, Context) + "<:gc3:804811090474434571>");
                        }
                        builder.AddField("Team " + t.ID, string.Join(" | ", mentions),false);
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
                        Team t = _tourney.Teams[id];
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithTitle("Team " + t.ID);
                        
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

                public Tournament _tourney { get; set; }
                public TournamentChannelManager _tChannelMgr { get; set; }

                [Command("generate")]
                [Summary("Generate Bracket")]
                public async Task Generate()
                {
                    if (_tourney._teams.Count == 0)
                    {
                        await ReplyAsync("Please Generate Teams first");
                        return;
                    }
                    _tourney.CreateBracket();
                    await ReplyAsync("Bracket Successfully Generated");
                    await Show();
                }

                [Command("post")]
                [Summary("Post Bracket to Discord")]
                public async Task Show()
                {
                    _tourney.bracket.GenerateSVG();
                    var _channel = Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID) as SocketTextChannel;
                    EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                    footerBuilder.Text = "EGG Gang - Zimarulis - RL Fridays";

                    var picture = await Context.Channel.SendFileAsync(@"bracket.png");

                    string imgurl = picture.Attachments.First().Url;
                    _channel = Context.Guild.GetChannel(_tChannelMgr.AnnouncementChannelID) as SocketTextChannel;
                    EmbedBuilder BracketMessage = new EmbedBuilder();
                    BracketMessage.WithAuthor("RL Fridays");
                    BracketMessage.WithTitle("Bracket");
                    BracketMessage.Description = "Το bracket δημιουργήθηκε. Σύντομα θα ξεκινήσει ο πρώτος γύρος!";
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
                public Tournament _tourney { get; set; }
                public TournamentChannelManager _tChannelMgr { get; set; }

                [Command("open")]
                [Summary("Enables Registrations")]
                public async Task Open()
                {
                    _tourney.RegistrationsEnabled = true;
                    //Make announcement
                    //This command should be sent to the registration channel
                    EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                    footerBuilder.Text = "EGG Gang - Zimarulis - RL Fridays";
                    EmbedBuilder RegMessage = new EmbedBuilder();
                    RegMessage.WithTitle("Οι εγγραφές άνοιξαν!");
                    RegMessage.Description = "Οι εγγραφές άνοιξαν και θα παραμείνουν ανοιχτές μέχρι την έναρξη του τουρνουά στις 14:30! Θα ενημερωθείς με αντίστοιχο μύνημα όταν οι ομάδες φτιαχτούν.";
                    RegMessage.AddField("Πως δηλώνω συμμετοχή", "Δηλώσε συμμετοχή γράφοντας !join (emote του rank) όπως στην παρακάτω εικόνα", false);    // true - for inline
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
                    _tourney.RegistrationsEnabled = false;
                    //Make announcement
                    //This command should be sent to the registration channel
                    var _channel = Context.Guild.GetChannel(_tChannelMgr.RegistationChannelID) as ISocketMessageChannel;
                    await _channel.SendMessageAsync("Tournament Registrations are Closed!");
                }

            }


        }


    }
}
