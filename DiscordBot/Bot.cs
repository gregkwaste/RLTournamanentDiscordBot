using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using Discord;
using System.Reflection;
using TourneyDiscordBotWPF;

namespace TourneyDiscordBot
{
    public enum BotRequestType
    {
        SEND_MESSAGE,
        ANNOUNCE,
        ANNOUNCE_ALL,
        SEND_DM,
        DISCONNECT
    }

    public class BotRequest
    {
        public BotRequestType Type;
        public List<object> args = new List<object>();
    }

    public class BotConfig
    {
        public char Prefix { get; set; }
    }

    public class TournamentChannelManager
    {
        public ulong GuildID { get; set; }
        public ulong RegistationChannelID { get; set; }
        public ulong ManagementChannelID { get; set; }
        public ulong ScoreReportChannelID { get; set; }
        public ulong AnnouncementChannelID { get; set; }
        public ulong CategoryChannelID { get; set; }

        public ulong RoleID { get; set; }
    }

    public class GuildData
    {
        public Tournament _tourney { get; set; }
    }

    public class GuildManager
    {
        public Dictionary<ulong, GuildData> Data {get;set;}
    }

    class Bot
    {
        private DiscordSocketClient _client;
        private string _token;
        // Keep the CommandService and DI container around for use with commands.
        // These two types require you install the Discord.Net.Commands package.
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly Queue<BotRequest> _requests;
        
        //Data
        private Tournament _tourney;

        //Bot Config
        private BotConfig _conf;

        //Tournament Channel Config
        private TournamentChannelManager _tchannelMgr;

        public Bot(string token, Tournament t, ulong guildID)
        {
            //Bind Data
            _token = token;
            _tourney = t;
            
            //Generate Config
            _conf = new BotConfig();
            _conf.Prefix = '!'; //Set Bot Prefix

            _tchannelMgr = new TournamentChannelManager();
            _tchannelMgr.GuildID = guildID;

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                //WebSocketProvider = Discord.Providers.,
                //UdpSocketProvider = UDPClientProvider.Instance,
                LogLevel = LogSeverity.Verbose
            });

            _commands = new CommandService(new CommandServiceConfig
            {
                // Again, log level:
                LogLevel = LogSeverity.Info,
                // There's a few more properties you can set,
                // for example, case-insensitive commands.
                CaseSensitiveCommands = false,
            });

            //Add Tournament as a service
            _services = new ServiceCollection().AddSingleton(_tourney).
                                                AddSingleton(_commands).
                                                AddSingleton(_conf).
                                                AddSingleton(_tchannelMgr).
                                                BuildServiceProvider();
            //Init Requests List
            _requests = new Queue<BotRequest>();

        }


        private async Task InitCommands()
        {
            // Either search the program and add all Module classes that can be found.
            // Module classes MUST be marked 'public' or they will be ignored.
            // You also need to pass your 'IServiceProvider' instance now,
            // so make sure that's done before you get here.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            
            // Or add Modules manually if you prefer to be a little more explicit:
            //await _commands.AddModuleAsync<SomeModule>(_services);
            // Note that the first one is 'Modules' (plural) and the second is 'Module' (singular).

            //Register Handlers

            //Subscribe logging handler to the services
            _client.Log += Log;
            _commands.Log += Log;

            // Subscribe a handler to see if a message invokes a command.
            _client.MessageReceived += HandleCommandAsync;
            _client.Connected += HandleConnectedAsync;
            _client.Disconnected += HandleDisConnectedAsync;

        }


        public async Task MainAsync()
        {
            await InitCommands();

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            //await Task.Delay(-1);
        }

        public void SendMessage(ulong _cid, string msg)
        {
            if (_cid == 0)
            {
                Log(new LogMessage(LogSeverity.Error, "", "Channel does not exist yet"));
                return;
            }   
            
            var _channel = _client.GetChannel(_cid) as ISocketMessageChannel;
            _channel.SendMessageAsync(msg);
        }

        public async void SendRequest(BotRequest req)
        {
            switch (req.Type)
            {
                case BotRequestType.DISCONNECT:
                    {
                        try
                        {
                            await Close();
                        } catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        
                        break;
                    }
                case BotRequestType.SEND_MESSAGE:
                    {
                        //Fetch channel_id
                        ulong _cid = (ulong)req.args[0];
                        string msg = (string)req.args[1];
                        SendMessage(_cid, msg);
                        break;
                    }
                case BotRequestType.ANNOUNCE:
                    {
                        //Fetch channel_id
                        ulong _cid = _tchannelMgr.AnnouncementChannelID;
                        string msg = (string)req.args[0];
                        SendMessage(_cid, msg);
                        break;
                    }
                case BotRequestType.ANNOUNCE_ALL:
                    {
                        string msg = (string)req.args[0];
                        foreach (var _guild in _client.Guilds)
                        {
                            foreach (var _channel in _guild.TextChannels)
                            {
                                SendMessage(_channel.Id, msg);
                            }
                        }
                        break;
                    }
            }
        }

        public bool getStatus
        {
            get
            {
                return (_client.Status == UserStatus.Online);
            }
        }

        private void deleteChannel(SocketGuild _g, ulong channelID)
        {
            if (channelID != 0xFFFFFFFFFFFFFFFF && channelID != 0)
                _g.GetChannel(channelID).DeleteAsync();
            System.Threading.Thread.Sleep(200);
        }

        private void deleteRole(SocketGuild _g, ulong roleID)
        {
            if (roleID != 0xFFFFFFFFFFFFFFFF && roleID != 0)
                _g.GetRole(roleID).DeleteAsync();
            System.Threading.Thread.Sleep(200);
        }

        private void deleteCategoryChannel(SocketGuild _g, ulong catID)
        {
            if (catID != 0xFFFFFFFFFFFFFFFF &&  catID != 0)
                _g.GetCategoryChannel(catID).DeleteAsync();
            System.Threading.Thread.Sleep(200);
        }

        public async Task Close()
        {
            SocketGuild sg = _client.GetGuild(_tchannelMgr.GuildID);

            deleteChannel(sg, _tchannelMgr.ManagementChannelID);
            deleteChannel(sg, _tchannelMgr.AnnouncementChannelID);
            deleteChannel(sg, _tchannelMgr.RegistationChannelID);
            deleteRole(sg, _tchannelMgr.RoleID);
            deleteCategoryChannel(sg, _tchannelMgr.CategoryChannelID);

            await sg.DefaultChannel.SendMessageAsync("GAMW TH MANA SOU");
            await _client.LogoutAsync();
            return;
            _client.Dispose();
        }

        private Task Log(LogMessage msg)
        {
            try
            {
                if (msg.Message != null)
                    TourneyDiscordBotWPF.Common.Common.loggerFunc(msg.Message.ToString());
                else
                    TourneyDiscordBotWPF.Common.Common.loggerFunc(msg.ToString());
            }
            catch (Exception e)
            {
                TourneyDiscordBotWPF.Common.Common.loggerFunc(e.Message.ToString());
            }

            return Task.CompletedTask;
        }

        private async Task HandleConnectedAsync()
        {
            SocketGuild sg = _client.GetGuild(_tchannelMgr.GuildID);
            try
            {
                if (sg.SystemChannel != null)
                    await sg.SystemChannel.SendMessageAsync("Tourney Bot Just Connected to server!");
            }
            catch (NullReferenceException ex)
            {
                //Do nothing
            }
        }

        private async Task HandleDisConnectedAsync(Exception e)
        {
            SocketGuild sg = _client.GetGuild(_tchannelMgr.GuildID);
            try
            {
                if (sg.SystemChannel != null)
                    await sg.SystemChannel.SendMessageAsync("Tourney Bot says good bye!");
            }
            catch (NullReferenceException ex)
            {
                //Do nothing
            }

        }
        
        private async Task HandleCommandAsync(SocketMessage arg)
        {
            // Bail out if it's a System Message.
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            // We don't want the bot to respond to itself or other bots.
            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;

            // Create a number to track where the prefix ends and the command begins
            int pos = 0;
            // Replace the '!' with whatever character
            // you want to prefix your commands with.
            // Uncomment the second half if you also want
            // commands to be invoked by mentioning the bot instead.
            if (msg.HasCharPrefix(_conf.Prefix, ref pos) /* || msg.HasMentionPrefix(_client.CurrentUser, ref pos) */)
            {
                // Create a Command Context.
                var context = new SocketCommandContext(_client, msg);

                // Execute the command. (result does not indicate a return value, 
                // rather an object stating if the command executed successfully).
                var result = await _commands.ExecuteAsync(context, pos, _services);
                
                // Uncomment the following lines if you want the bot
                // to send a message if it failed.
                // This does not catch errors from commands with 'RunMode.Async',
                // subscribe a handler for '_commands.CommandExecuted' to see those.
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    await msg.Channel.SendMessageAsync(result.ErrorReason);
            } 
        }

    }
}
