using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for WinnerSelectWindow.xaml
    /// </summary>
    public partial class WinnerSelectWindow : Window
    {
        public WinnerSelectWindow()
        {
            InitializeComponent();
        }


        private void SetWinnerToMatchup (Matchup match, Team winner)
        {
            if (winner == match.Team1)
                match.Winner = match.Team1;
            else
                match.Winner = match.Team2;

            //Progress winner to the next round
            if (match.Next != null)
            {
                if (match.Next.Team1 != null)
                    match.Next.Team2 = match.Winner;
                else
                    match.Next.Team1 = match.Winner;
            }
            else
            {
                //This was probably the final
                string message = "CONGRATS TO ";

                if (match.Winner is Team2s)
                {
                    message += ((Team2s)match.Winner).Player1.Name;
                    message += " and " + ((Team2s)match.Winner).Player2.Name;
                }
                message += " FOR WINNING THE TOURNAMENT!!!!";
                MessageBox.Show(message, "TOURNEY WINNER", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Team1_Won(object sender, RoutedEventArgs e)
        {
            Matchup match = (Matchup) ((Button) sender).DataContext;
            Team t = match.Team1;
            SetWinnerToMatchup(match, t);
            Close();
        }

        private void Team2_Won(object sender, RoutedEventArgs e)
        {
            Matchup match = (Matchup)((Button)sender).DataContext;
            Team t = match.Team2;
            SetWinnerToMatchup(match, t);
            Close();
        }
    }
}
