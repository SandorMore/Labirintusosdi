using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfApp1.Models;
using WpfApp1.Utils;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        Labyrinth labyrinth;

        LANG lang = LANG.ENG;

        Rectangle player;

        int playerX = 1;
        int playerY = 1;

        const int TILESIZE = 25;

        bool isLoaded = false;

        char[,] currentMap;
        int mapWidth;
        int mapHeight;

        public MainWindow()
        {
            InitializeComponent();

            update_language();

            isLoaded = true;

            this.Loaded += (s, e) => Keyboard.Focus(this);

            this.KeyDown += MainWindow_KeyDown;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var res = read_map();

            if (res == null)
                return;

            currentMap = res.Value.map;
            mapWidth = res.Value.width;
            mapHeight = res.Value.height;

            labyrinth = new Labyrinth(mapWidth, mapHeight, currentMap);

            render_map(currentMap, mapWidth, mapHeight);

            create_player();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            int newX = playerX;
            int newY = playerY;

            switch (e.Key)
            {
                case Key.W:
                    newY--;
                    break;

                case Key.S:
                    newY++;
                    break;

                case Key.A:
                    newX--;
                    break;

                case Key.D:
                    newX++;
                    break;
            }

            move_player(newX, newY);
        }

        void move_player(int newX, int newY)
        {
            if (currentMap == null)
                return;

            if (newX < 0 || newY < 0 ||
                newX >= mapWidth || newY >= mapHeight)
                return;

            playerX = newX;
            playerY = newY;

            update_player();
        }

        void update_player()
        {
            Canvas.SetLeft(player, playerX * TILESIZE);
            Canvas.SetTop(player, playerY * TILESIZE);
        }

        void create_player()
        {
            if (player != null)
                gameCanvas.Children.Remove(player);

            player = new Rectangle
            {
                Width = TILESIZE,
                Height = TILESIZE,
                Fill = Brushes.LightCoral
            };

            Canvas.SetLeft(player, playerX * TILESIZE);
            Canvas.SetTop(player, playerY * TILESIZE);

            gameCanvas.Children.Add(player);
        }

        void render_map(char[,] map, int width, int height)
        {
            gameCanvas.Children.Clear();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    TextBlock tile = new TextBlock
                    {
                        Text = map[y, x].ToString(),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 20,
                        Foreground = Brushes.White
                    };

                    Canvas.SetLeft(tile, x * TILESIZE);
                    Canvas.SetTop(tile, y * TILESIZE);

                    gameCanvas.Children.Add(tile);
                }
            }
        }

        (char[,] map, int width, int height)? read_map()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                RestoreDirectory = true,
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (ofd.ShowDialog() != true)
                return null;

            string[] lines;

            try
            {
                lines = File.ReadAllLines(ofd.FileName, Encoding.UTF8);
            }
            catch
            {
                return null;
            }

            if (lines.Length == 0)
                return null;

            int height = lines.Length;
            int width = 0;

            foreach (var line in lines)
                if (line.Length > width)
                    width = line.Length;

            char[,] map = new char[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    map[y, x] = (x < lines[y].Length) ? lines[y][x] : ' ';
                }
            }

            return (map, width, height);
        }

        void update_language()
        {
            lbRead.Content = (lang == LANG.HUN) ? "Fájl beolvasása" : "Read file";
            btnRead.Content = (lang == LANG.HUN) ? "Olvasás" : "Read";
            lbLanguage.Content = (lang == LANG.HUN) ? "Nyelvezet" : "Language";

            rbEng.Content = (lang == LANG.HUN) ? "Angol" : "English";
            rbHun.Content = (lang == LANG.HUN) ? "Magyar" : "Hungarian";
        }

        private void rbEng_Checked(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            lang = LANG.ENG;
            update_language();
        }

        private void rbHun_Checked(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            lang = LANG.HUN;
            update_language();
        }
    }
}