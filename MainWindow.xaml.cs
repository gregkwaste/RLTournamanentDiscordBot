using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Threading;
using TourneyDiscordBot;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace TourneyDiscordBotWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class EmoteSettings
    {
        public string RL_RANK_BRONZE_I;
        public string RL_RANK_BRONZE_II;
        public string RL_RANK_BRONZE_III;
        public string RL_RANK_SILVER_I;
        public string RL_RANK_SILVER_II;
        public string RL_RANK_SILVER_III;
        public string RL_RANK_GOLD_I;
        public string RL_RANK_GOLD_II;
        public string RL_RANK_GOLD_III;
        public string RL_RANK_PLATINUM_I;
        public string RL_RANK_PLATINUM_II;
        public string RL_RANK_PLATINUM_III;
        public string RL_RANK_DIAMOND_I;
        public string RL_RANK_DIAMOND_II;
        public string RL_RANK_DIAMOND_III;
        public string RL_RANK_CHAMPION_I;
        public string RL_RANK_CHAMPION_II;
        public string RL_RANK_CHAMPION_III;
        public string RL_RANK_GRAND_CHAMPION_I;
        public string RL_RANK_GRAND_CHAMPION_II;
        public string RL_RANK_GRAND_CHAMPION_III;
        public string RL_RANK_SUPER_SONIC_LEGEND;
    }

    public class TextSettings
    {
        public string desc_1s_start;
        public string desc_2s_start;
        public string desc_3s_start;
        public string thumbnail_URL;
        public string embed_footer;
        public string tournRoleName;
        public string fixeddesc;
        public string goodnbaddesc;
    }

    public class Settings
    {
        public string AppToken;
        public ulong GuildID;
        public EmoteSettings emoteSettings = new EmoteSettings();
        public TextSettings textSettings = new TextSettings();
    }
    
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Tournament _tourney = new Tournament();
        private Thread disc_thread;
        private Bot _bot;
        private bool _botStatus;
        private System.Timers.Timer UI_updateTimer;
        private Settings settings = new Settings();
        public event PropertyChangedEventHandler PropertyChanged;


        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ClearPlayersEvent(object sender, RoutedEventArgs e)
        {
            _tourney.ClearPlayers();
        }

        private void AddPLayerEvent(object sender, RoutedEventArgs e)
        {
            Window1 w = new Window1();
            w.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            w.Title = "Add a new Player";
            w.SetTournament(_tourney);
            w.Show();
            w.Focus();
        }

        private void FormClose(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Populate Common Structs
            Common.Common.Populate();

            //DEBUG Add some players
#if DEBUG
            _tourney.AddRandomPlayers();
#endif

            PlayerList.ItemsSource = _tourney.Players;

            //Set Discord Output Callback
            Common.Common.loggerFunc = discordLog;
            //Delete old Log
            File.Delete("discord.out");

            //Load Settings File
            string jsonstring = File.ReadAllText("settings.json");
            settings = JsonConvert.DeserializeObject<Settings>(jsonstring);

            DiscordDataService _data = new DiscordDataService();
            _data.setSettings(settings);
            _data.setTournament(_tourney);

            //Start Discord Bot
            _bot = new Bot(settings.AppToken, _data, settings.GuildID);
            startDiscBot();
            
            //Start UI update Timer
            UI_updateTimer = new System.Timers.Timer();
            //UI_updateTimer.Interval = 500;
            UI_updateTimer.Elapsed += timerHandler;
            UI_updateTimer.Start();
    }

        //Timer Update Callback
        private void timerHandler(object sender, System.Timers.ElapsedEventArgs args)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                PlayerList.ItemsSource = _tourney.Players;
                TeamList.ItemsSource = _tourney.Teams;

                if (_tourney.bracket != null)
                {
                    //Generate Bracket In View
                    StackPanel sp = new StackPanel(); //Main container
                    sp.SetValue(Grid.IsSharedSizeScopeProperty, true);
                    sp.Orientation = Orientation.Vertical;

                    Grid gr_h = new Grid(); //Header Grid

                    //Populate Bracket Window
                    Grid gr = new Grid();

                    sp.Children.Add(gr_h);
                    sp.Children.Add(gr);

                    for (int i = 0; i < _tourney.bracket.Rounds.Count; i++)
                    {
                        ColumnDefinition cd = new ColumnDefinition();
                        ColumnDefinition cd_h = new ColumnDefinition();
                        gr.ColumnDefinitions.Add(cd);
                        gr_h.ColumnDefinitions.Add(cd_h);

                        //Create textblock for header
                        TextBlock hdr = new TextBlock();
                        hdr.Text = "Round " + i;
                        hdr.SetValue(Grid.ColumnProperty, i);
                        hdr.HorizontalAlignment = HorizontalAlignment.Center;
                        gr_h.Children.Add(hdr);


                        //Create LisView for round
                        Round r = _tourney.bracket.Rounds[i];
                        ListView lv = new ListView();
                        lv.SetValue(Grid.ColumnProperty, i);
                        lv.HorizontalAlignment = HorizontalAlignment.Stretch;
                        lv.ItemsSource = r.Matchups;
                        lv.ItemTemplate = (DataTemplate)Resources["MatchupTemplate"];

                        //Add Matchups to View
                        lv.ItemsSource = r.Matchups;
                        gr.Children.Add(lv);

                        /*
                        ListView lv = new ListView();
                        lv.SetValue(Grid.ColumnProperty, i);
                        lv.HorizontalAlignment = HorizontalAlignment.Stretch;
                        GridView gv = new GridView();

                        Round r = bracket.Rounds[i];
                        GridViewColumn gvc = new GridViewColumn();
                        gvc.Header = "Round " + i;
                        gv.Columns.Add(gvc);

                        //Add Matchups to View
                        gvc.CellTemplate = (DataTemplate) Resources["MatchupTemplate"];
                        lv.ItemsSource = r.Matchups;
                        lv.View = gv;
                        gr.Children.Add(lv);
                        */
                    }

                    BracketView.Content = sp;
                }

                BotStatus = _bot.getStatus;
            }));
        }
        
        //Custom Discrord logger Function
        public void discordLog(string msg)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                    StreamWriter tx = new StreamWriter("discord.out", true);
                    tx.WriteLine(msg);
                    tx.Close();
                    Discord_Output.Text += msg + "\n";
                }));
            }
        }

        //Bot Methods
        private void startDiscBot()
        {
            _bot.MainAsync(); //Start Bot Async
            //disc_thread = new Thread(discBotLoop);
            //disc_thread.IsBackground = true;
            //disc_thread.Start();
            //BotStatus = _bot.getStatus;
        }

        private void stopDiscBot()
        {
            if (disc_thread.IsAlive)
                disc_thread.Abort();
        }

        public bool BotStatus
        {
            get
            {
                return (_bot != null) ? _bot.getStatus : false;
            }

            set
            {
                if (!value)
                {
                    BotRequest req = new BotRequest();
                    req.Type = BotRequestType.DISCONNECT;
                    _bot.SendRequest(req);
                }
                _botStatus = value;
                OnPropertyChanged("BotStatus");
            }
        }

        private void discBotLoop()
        {
            _bot.MainAsync().GetAwaiter();
        }

        private void CreateTeams(object sender, RoutedEventArgs e)
        {
            _tourney.CreateTeams();
            //Bind to App
            TeamList.ItemsSource = _tourney.Teams;
            MessageBox.Show("Teams Successfully Generated", "All good", MessageBoxButton.OK);
        }

        private void ExportBracket(object sender, RoutedEventArgs e)
        {
            _tourney.ExportBracket();
        }

        private void CreateBracket(object sender, RoutedEventArgs e)
        {
            _tourney.CreateBracket();
            MessageBox.Show("Bracket Successfully Generated", "All good", MessageBoxButton.OK);
        }

        private void DockPanel_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu ctx = new ContextMenu();
            MenuItem mt = new MenuItem();
            mt.Header = "Set Winner";
            mt.Click += SetWinner;
            ctx.Items.Add(mt);

            ((DockPanel)sender).ContextMenu = ctx;
        }

        private void SetWinner(object sender, RoutedEventArgs e)
        {
            Matchup match = (Matchup)((MenuItem)sender).DataContext;
            WinnerSelectWindow win = new WinnerSelectWindow();

            win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            win.Title = "Set Match Winner";
            win.Content = match;
            win.Show();

        }

        private void RemovePlayer(object sender, RoutedEventArgs e)
        {
            //Fetch selected item from listview

            if (PlayerList.SelectedItem != null)
            {
                Player p = (Player)PlayerList.SelectedItem;
                _tourney.RemovePlayer(p);
            }
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                //Console.WriteLine("gotcha");
                MatchupWindow win = new MatchupWindow();
                win.MatchupContent.Content = ((DockPanel)sender).DataContext;
                win.Show();

            }
        }

        private void ImportPlayersEvent(object sender, RoutedEventArgs e)
        {
            //Clear Players
            _tourney.ClearPlayers();
            
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "CSV Files (*.csv)|*.csv";
            bool? res = ofd.ShowDialog();

            if (res == false)
                return;

            _tourney.ImportPlayersFromCSV(ofd.FileName);
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            BotRequest req = new BotRequest();
            req.Type = BotRequestType.ANNOUNCE_ALL;
            req.args.Add("Hello!!!!");
            _bot.SendRequest(req);
        }

        private void BaseWindow_Closed(object sender, EventArgs e)
        {
            
        }

        private void BaseWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_bot.getStatus)
            {
                BotRequest req = new BotRequest();
                req.Type = BotRequestType.DISCONNECT;
                _bot.SendRequest(req);
            }

            UI_updateTimer.Stop();
        }
    }
}

