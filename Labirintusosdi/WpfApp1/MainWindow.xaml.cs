using Microsoft.Win32;
using System.CodeDom;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp1.Models;
using WpfApp1.Utils;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        Labyrinth labyrinth;
        LANG lang = LANG.HUN;
        Rectangle player;

        int playerX = 1;
        int playerY = 1;
        const int TILESIZE = 20;
        public MainWindow()
        {
            InitializeComponent();


            lbRead.Content = (lang == LANG.HUN) ? "Fájl beolvasása" : "Read file";
            btnRead.Content = (lang == LANG.HUN) ? "Olvasás" : "Read";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var res = read_map();
            if (res != null)
            {
                labyrinth = new Labyrinth(res.Item2, res.Item3, res.Item1);
            }
            labyrinth.print_labyrinth();
            if(res != null) 
                render_map(res.Item1, res.Item2, res.Item3);
            
            create_player();
        }
        Tuple<char[,], int, int>? read_map()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.RestoreDirectory = true;
            ofd.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";

            if (ofd.ShowDialog() != true)
            {
                MessageBox.Show((lang == LANG.HUN) ? "Nem lehetett beolvasni a fájlt!" : "Couldn't open the file!");
                return null;
            }

            string mapPath = ofd.FileName;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(mapPath, Encoding.UTF8);
                MessageBox.Show((lang == LANG.HUN) ? "A fájlt sikeresen beolvasta!" : "The file has benn read sucessfully");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show((lang == LANG.HUN) ? $"Hiba a fájl beolvasása közben: {ex.Message}" : $"Error reading file: {ex.Message}");
                return null;
            }

            if (lines.Length == 0)
            {
                MessageBox.Show((lang == LANG.HUN) ? "A fájl üres." : "The file is empty.");
                return null;
            }

            int height = lines.Length;
            int width = 0;
            foreach (var l in lines)
                if (l.Length > width) width = l.Length;

            var map = new char[height, width];

            for (int y = 0; y < height; y++)
            {
                var line = lines[y];
                for (int x = 0; x < width; x++)
                {
                    map[y, x] = (x < line.Length) ? line[x] : ' ';
                }
            }

            return Tuple.Create(map, width, height);
        }
        void render_map(char[,] map, int width, int height)
        {
            gameCanvas.Children.Clear();

            StringBuilder sb = new StringBuilder();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    sb.Append(map[y, x]);
                }

                sb.AppendLine();
            }

            TextBlock tb = new TextBlock();

            tb.Text = sb.ToString();

            tb.FontFamily = new FontFamily("Consolas");


            if (width <= 25 && height <= 25)
                tb.FontSize = 45;
            else if (width <= 40 && height <= 40)
                tb.FontSize = 30;
            else
                tb.FontSize = 20;

            tb.Foreground = Brushes.White;

            Canvas.SetLeft(tb, 0);
            Canvas.SetTop(tb, 0);

            gameCanvas.Children.Add(tb);
        }
        void create_player()
        {
            player = new Rectangle();

            player.Width = TILESIZE;
            player.Height = TILESIZE;

            player.Fill = Brushes.Green;

            Canvas.SetLeft(player, playerX * TILESIZE);
            Canvas.SetTop(player, playerY * TILESIZE);

            gameCanvas.Children.Add(player);
        }

    }
}