using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TourneyDiscordBotWPF.Common;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;

namespace TourneyDiscordBotWPF
{
    public class Rank
    {
        public Bitmap Image { get; set; }
        public BitmapSource ImageSrc { get; }
        public string Name { get; set; }
        public RL_RANK _rank;


        public Rank(string rank_name)
        {
            Name = rank_name;
            
            _rank = getRankFromText(rank_name);

            //Set Image

            switch (_rank)
            {
                case RL_RANK.BRONZE_I:
                    Image = Properties.Resources.Bronze1_rank_icon;
                    break;
                case RL_RANK.BRONZE_II:
                    Image = Properties.Resources.Bronze2_rank_icon_;
                    break;
                case RL_RANK.BRONZE_III:
                    Image = Properties.Resources.Bronze3_rank_icon_;
                    break;
                case RL_RANK.SILVER_I:
                    Image = Properties.Resources.Silver1_rank_icon;
                    break;
                case RL_RANK.SILVER_II:
                    Image = Properties.Resources.Silver2_rank_icon;
                    break;
                case RL_RANK.SILVER_III:
                    Image = Properties.Resources.Silver3_rank_icon;
                    break;
                case RL_RANK.GOLD_I:
                    Image = Properties.Resources.Gold1_rank_icon;
                    break;
                case RL_RANK.GOLD_II:
                    Image = Properties.Resources.Gold2_rank_icon;
                    break;
                case RL_RANK.GOLD_III:
                    Image = Properties.Resources.Gold3_rank_icon;
                    break;
                case RL_RANK.PLATINUM_I:
                    Image = Properties.Resources.Platinum1_rank_icon;
                    break;
                case RL_RANK.PLATINUM_II:
                    Image = Properties.Resources.Platinum2_rank_icon;
                    break;
                case RL_RANK.PLATINUM_III:
                    Image = Properties.Resources.Platinum3_rank_icon;
                    break;
                case RL_RANK.DIAMOND_I:
                    Image = Properties.Resources.Diamond1_rank_icon;
                    break;
                case RL_RANK.DIAMOND_II:
                    Image = Properties.Resources.Diamond2_rank_icon;
                    break;
                case RL_RANK.DIAMOND_III:
                    Image = Properties.Resources.Diamond3_rank_icon;
                    break;
                case RL_RANK.CHAMPION_I:
                    Image = Properties.Resources.Champion1_rank_icon;
                    break;
                case RL_RANK.CHAMPION_II:
                    Image = Properties.Resources.Champion2_rank_icon;
                    break;
                case RL_RANK.CHAMPION_III:
                    Image = Properties.Resources.Champion3_rank_icon;
                    break;
                case RL_RANK.GRAND_CHAMPION_I:
                    Image = Properties.Resources.Grand_champion1_rank_icon;
                    break;
                case RL_RANK.GRAND_CHAMPION_II:
                    Image = Properties.Resources.Grand_champion2_rank_icon_1;
                    break;
                case RL_RANK.GRAND_CHAMPION_III:
                    Image = Properties.Resources.Grand_champion3_rank_icon;
                    break;
                case RL_RANK.SUPERSONIC_LEGEND:
                    Image = Properties.Resources.Supersonic_Legend_rank_icon;
                    break;
                default:
                    Image = null;
                    break;
            }

            if (Image != null)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    Image.Save(stream, ImageFormat.Png);

                    stream.Position = 0;
                    BitmapImage result = new BitmapImage();
                    result.BeginInit();
                    // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                    // Force the bitmap to load right now so we can dispose the stream.
                    result.CacheOption = BitmapCacheOption.OnLoad;
                    result.StreamSource = stream;
                    result.EndInit();
                    result.Freeze();
                    ImageSrc = result;
                }
            }

        }

        public override string ToString()
        {
            return base.ToString();
        }

        public static RL_RANK getRankFromText(string rank_text)
        {
            //Convert string
            string[] sp = rank_text.ToUpper().Split(' ');
            string rk_str = string.Join("_", sp);

            try
            {
                return (RL_RANK)Enum.Parse(typeof(RL_RANK), rk_str);
            }
            catch
            {
                return RL_RANK.NONE;
            }
        }

    }
}
