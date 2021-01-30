using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TourneyDiscordBotWPF
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 

    public class RankItem
    {
        public BitmapSource _img;
        public string _name;

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public BitmapSource Img
        {
            get
            {
                return _img;
            }
        }



    }

    public partial class Window1 : Window
    {
        private Tournament _tourney;   

        public Window1()
        {
            InitializeComponent();
        }

        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Populate Comboboxes
            addRankToCombobox("Bronze I");
            addRankToCombobox("Bronze II");
            addRankToCombobox("Bronze III");
            addRankToCombobox("Silver I");
            addRankToCombobox("Silver II");
            addRankToCombobox("Silver III");
            addRankToCombobox("Gold I");
            addRankToCombobox("Gold II");
            addRankToCombobox("Gold III");
            addRankToCombobox("Platinum I");
            addRankToCombobox("Platinum II");
            addRankToCombobox("Platinum III");
            addRankToCombobox("Diamond I");
            addRankToCombobox("Diamond II");
            addRankToCombobox("Diamond III");
            addRankToCombobox("Champion I");
            addRankToCombobox("Champion II");
            addRankToCombobox("Champion III");
            addRankToCombobox("Grand Champion I");
            addRankToCombobox("Grand Champion II");
            addRankToCombobox("Grand Champion III");
            addRankToCombobox("SuperSonic Legend");
        }

        public void SetTournament(Tournament t)
        {
            _tourney = t;
        }

        private void addRankToCombobox(string Name)
        {
            RankItem rk = new RankItem();
            rk._name = Name;
            rk._img = Common.Common.getRankFromText(Name).ImageSrc;
            

            rankList.Items.Add(rk);
        }

        private void Button_Add_Player(object sender, RoutedEventArgs e)
        {
            //Player is supposed to be saved here
            //Fetch player info

            //Fetch name
            string name = form_player_name.Text;

            //Fetch Rank
            RankItem rk = (RankItem) rankList.SelectedItem;

            if (rk != null)
            {
                _tourney.CreatePlayer(name, rk.Name);
            }
            
            Close();
        }
    }
}
