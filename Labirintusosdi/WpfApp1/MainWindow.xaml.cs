using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfApp1.Models;
using WpfApp1.Utils;
using System.Timers;
using System.Windows.Threading;
using System.Diagnostics;


namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        Labyrinth labyrinth;
        System.Timers.Timer aTimer = new System.Timers.Timer();
        DispatcherTimer gameTimer;
        TimeSpan remainingTime;

        LANG lang = LANG.ENG;

        Rectangle player;

        int playerX = 1;
        int playerY = 1;

        const int TILESIZE = 25;

        bool isLoaded = false;

        bool canExitDungeon = false;

        bool fogOfWarEnabled = false;
        bool progressiveFogOfWar = false; // New mode

        char[,] currentMap;
        int mapWidth;
        int mapHeight;

        int allRooms = 0;
        int roomsFound = 0;

        bool gameIsLoaded = false;

        List<(int, int)> entranceList;

        HashSet<(int, int)> visitedRooms = new HashSet<(int, int)>();

        HashSet<(int, int)> discoveredTiles = new HashSet<(int, int)>();

        public MainWindow()
        {
            InitializeComponent();

            update_language();

            isLoaded = true;

            this.Loaded += (s, e) => Keyboard.Focus(this);
            rbEng.IsChecked = true;
            this.KeyDown += MainWindow_KeyDown;

            lbDirectionsFromCurrent.Content =
                (lang == LANG.HUN)
                ? "Ezekbe az irányokba indulhat: "
                : "You can go towards: ";
        }

        (int, int) setup_game(List<(int, int)> entrances)
        {
            Random rnd = new Random();

            return entrances[rnd.Next(0, entrances.Count)];
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

            visitedRooms.Clear();
            discoveredTiles.Clear();

            roomsFound = 0;
            allRooms = 0;

            canExitDungeon = false;

            allRooms = labyrinth.get_room_number();

            lbFoundRooms.Content =
                (lang == LANG.HUN)
                ? $"{roomsFound} / {allRooms} megtalálva!"
                : $"Found: {roomsFound} / {allRooms}!";

            entranceList = locate_entrances(labyrinth.Map);

            var startPos = setup_game(entranceList);

            playerX = startPos.Item1;
            playerY = startPos.Item2;


            discoveredTiles.Clear();
            discover_current_tile();

            render_map(currentMap, mapWidth, mapHeight);

            create_player();

            StartGameTimer();

            update_directions(labyrinth.Map[playerY, playerX]);
        }

        void StartGameTimer()
        {
            try
            {
                StopGameTimer();

                remainingTime = TimeSpan.FromMinutes(1);

                gameTimer = new DispatcherTimer();
                gameTimer.Interval = TimeSpan.FromSeconds(1);
                gameTimer.Tick += (s, e) =>
                {
                    remainingTime = remainingTime - TimeSpan.FromSeconds(1);
                    lbTimer.Content = (lang == LANG.HUN)
                        ? $"Idő: {remainingTime}"
                        : $"Time: {remainingTime}";

                    if (remainingTime <= TimeSpan.Zero)
                    {
                        StopGameTimer();
                        OnTimerElapsed();
                    }
                };

                lbTimer.Content = (lang == LANG.HUN) ? $"Idő: {remainingTime.ToString(@"mm\:ss")}" : $"Time: {remainingTime.ToString(@"mm\:ss")}";
                gameTimer.Start();
            }
            catch { }
        }

        void StopGameTimer()
        {
            if (gameTimer != null)
            {
                gameTimer.Stop();
                gameTimer = null;
            }
        }

        void OnTimerElapsed()
        {
            MessageBox.Show(
                (lang == LANG.HUN) ? "Lejárt az idő. Vesztettél!" : "Time's up. You lost!");

            btnRead.IsEnabled = true;

            labyrinth = null;

            try { gameCanvas.Children.Clear(); } catch { }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (labyrinth == null)
                return;

            int newX = playerX;
            int newY = playerY;

            char currentTile = labyrinth.Map[playerY, playerX];

            add_room();

            update_directions(currentTile);

            if (!can_move(currentTile, e.Key))
                return;

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

                default:
                    return;
            }

            if (newX < 0 || newY < 0 ||
                newX >= mapWidth || newY >= mapHeight)
            {
                if (is_entrance(playerX, playerY))
                {
                    MessageBoxResult result;

                    if (!canExitDungeon)
                    {
                        result = MessageBox.Show(
                            (lang == LANG.HUN)
                            ? "Még nem találtál szobát. Biztosan ki akarsz lépni?"
                            : "You have not found any rooms yet. Are you sure you want to leave?",
                            "Exit",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                    }
                    else
                    {
                        result = MessageBox.Show(
                            (lang == LANG.HUN)
                            ? "Biztosan ki akarsz lépni a labirintusból?"
                            : "Are you sure you want to leave the labyrinth?",
                            "Exit",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                    }

                    if (result == MessageBoxResult.Yes)
                    {
                        MessageBox.Show(
                            (lang == LANG.HUN)
                            ? "Sikeresen kijutottál a labirintusból!"
                            : "You escaped the labyrinth!");

                        Close();
                    }
                }

                return;
            }

            char targetTile = labyrinth.Map[newY, newX];

            if (!can_enter(targetTile, e.Key))
                return;

            move_player(newX, newY);
        }

        bool can_move(char tile, Key direction)
        {
            if (tile == '█')
                return true;

            switch (direction)
            {
                case Key.W:
                    return tile == '║' ||
                           tile == '╬' ||
                           tile == '╩' ||
                           tile == '╣' ||
                           tile == '╠' ||
                           tile == '╝' ||
                           tile == '╚';

                case Key.S:
                    return tile == '║' ||
                           tile == '╬' ||
                           tile == '╦' ||
                           tile == '╣' ||
                           tile == '╠' ||
                           tile == '╗' ||
                           tile == '╔';

                case Key.A:
                    return tile == '═' ||
                           tile == '╬' ||
                           tile == '╩' ||
                           tile == '╦' ||
                           tile == '╣' ||
                           tile == '╗' ||
                           tile == '╝';

                case Key.D:
                    return tile == '═' ||
                           tile == '╬' ||
                           tile == '╩' ||
                           tile == '╦' ||
                           tile == '╠' ||
                           tile == '╔' ||
                           tile == '╚';
            }

            return false;
        }

        bool can_enter(char tile, Key direction)
        {
            if (tile == '█')
                return true;

            switch (direction)
            {
                case Key.W:
                    return can_move(tile, Key.S);

                case Key.S:
                    return can_move(tile, Key.W);

                case Key.A:
                    return can_move(tile, Key.D);

                case Key.D:
                    return can_move(tile, Key.A);
            }

            return false;
        }

        void add_room()
        {
            if (currentMap[playerY, playerX] != '█')
                return;

            if (visitedRooms.Contains((playerX, playerY)))
                return;

            visitedRooms.Add((playerX, playerY));

            roomsFound++;

            if (roomsFound >= 1)
                canExitDungeon = true;

            lbFoundRooms.Content =
                (lang == LANG.HUN)
                ? $"{roomsFound} / {allRooms} megtalálva!"
                : $"Found: {roomsFound} / {allRooms}!";
        }

        void update_directions(char tile)
        {
            List<string> dirs = new List<string>();

            if (can_move(tile, Key.W))
            {
                dirs.Add(
                    (lang == LANG.HUN)
                    ? "Észak (fel)"
                    : "North (up)");
            }

            if (can_move(tile, Key.S))
            {
                dirs.Add(
                    (lang == LANG.HUN)
                    ? "Dél (le)"
                    : "South (down)");
            }

            if (can_move(tile, Key.A))
            {
                dirs.Add(
                    (lang == LANG.HUN)
                    ? "Nyugat (bal)"
                    : "West (left)");
            }

            if (can_move(tile, Key.D))
            {
                dirs.Add(
                    (lang == LANG.HUN)
                    ? "Kelet (jobb)"
                    : "East (right)");
            }

            if (dirs.Count == 0)
            {
                lbDirectionsFromCurrent.Content =
                    (lang == LANG.HUN)
                    ? "Nincs elérhető irány"
                    : "No available directions";

                return;
            }

            string text = string.Join(", ", dirs);

            lbDirectionsFromCurrent.Content =
                (lang == LANG.HUN)
                ? $"Ezekbe az irányokba indulhat: {text}"
                : $"You can go towards: {text}";
        }

        bool is_entrance(int x, int y)
        {
            return entranceList.Contains((x, y));
        }

        void move_player(int newX, int newY)
        {
            playerX = newX;
            playerY = newY;

            discover_current_tile();

            if (progressiveFogOfWar)
            {
                discover_surrounding_tiles(newX, newY);
            }

            render_map(currentMap, mapWidth, mapHeight);

            update_player();

            add_room();

            lbMoves.Items.Add($"({playerY},{playerX})");

            update_directions(labyrinth.Map[playerY, playerX]);
        }

        void discover_surrounding_tiles(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int newX = x + dx;
                    int newY = y + dy;


                    if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight)
                    {
                        discoveredTiles.Add((newX, newY));
                    }
                }
            }
        }

        void update_player()
        {
            try
            {
                Canvas.SetLeft(player, playerX * TILESIZE);
                Canvas.SetTop(player, playerY * TILESIZE);
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show((lang == LANG.HUN) ? "Először olvass be térképet!" : "Read a map first");
                return;
            }

        }

        void create_player()
        {
            if (player != null)
                gameCanvas.Children.Remove(player);

            player = new Rectangle
            {
                Width = TILESIZE,
                Height = TILESIZE,
                Fill = Brushes.LightCoral,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };

            Canvas.SetLeft(player, playerX * TILESIZE);
            Canvas.SetTop(player, playerY * TILESIZE);

            gameCanvas.Children.Add(player);
        }

        void discover_current_tile()
        {
            discoveredTiles.Add((playerX, playerY));
        }

        void render_map(char[,] map, int width, int height)
        {
            gameCanvas.Children.Clear();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool visible = !fogOfWarEnabled || discoveredTiles.Contains((x, y));

                    TextBlock tile = new TextBlock
                    {
                        Text = visible ? map[y, x].ToString() : " ",
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 20,
                        Foreground = visible ? Brushes.White : Brushes.Gray,
                        Background = visible ? Brushes.Transparent : Brushes.Black
                    };

                    Canvas.SetLeft(tile, x * TILESIZE);
                    Canvas.SetTop(tile, y * TILESIZE);

                    gameCanvas.Children.Add(tile);
                }
            }
            if (player != null)
            {
                if (!gameCanvas.Children.Contains(player))
                    gameCanvas.Children.Add(player);

                Canvas.SetLeft(player, playerX * TILESIZE);
                Canvas.SetTop(player, playerY * TILESIZE);
            }

            if (player != null && !gameIsLoaded)
            {
                Canvas.SetLeft(player, playerX * TILESIZE);
                Canvas.SetTop(player, playerY * TILESIZE);
                gameCanvas.Children.Add(player);
            }
        }
        private void load_game()
        {
            gameIsLoaded = true;
            var res = load_file();
            if(res != null)
            {
                playerX = res.Value.posX;
                playerY = res.Value.posY;

                currentMap = res.Value.map;
                mapWidth = res.Value.width;
                mapHeight = res.Value.height;

                labyrinth = new Labyrinth(mapWidth, mapHeight, currentMap);
                entranceList = locate_entrances(currentMap);
                allRooms = labyrinth.get_room_number();


                discoveredTiles.Clear();
                visitedRooms.Clear();
                discover_current_tile();

                render_map(currentMap, mapWidth, mapHeight);

                create_player();

                lbFoundRooms.Content = (lang == LANG.HUN) ? $"{roomsFound} / {allRooms} megtalálva!" : $"Found: {roomsFound} / {allRooms}!";

                try { gameCanvas.Focus(); Keyboard.Focus(gameCanvas); } catch { }
                StartGameTimer();
            }
            else
            {
                MessageBox.Show((lang == LANG.HUN) ? "Hiba történt" : "Error");
                return;
            }
        }

        private (char[,] map, int width, int height, int posX, int posY)? load_file()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                RestoreDirectory = true,
                Filter = "SAV files (*.SAV)|*.SAV|All files (*.*)|*.*"
            };

            if (ofd.ShowDialog() != true)
                return null;

            string[] lines;

            try
            {
                lines = File.ReadAllLines(ofd.FileName, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show((lang == LANG.HUN) ? $"Hiba a fájl olvasásakor: {ex.Message}" : $"Error reading file: {ex.Message}");
                return null;
            }

            if (lines.Length < 3)
            {
                MessageBox.Show((lang == LANG.HUN) ? "Érvénytelen mentési fájl (túl rövid)" : "Invalid save file (too short)");
                return null;
            }

            int height = lines.Length - 2;
            int width = 0;

            for (int i = 0; i < height; i++)
                if (lines[i].Length > width) width = lines[i].Length;

            char[,] map = new char[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    map[y, x] = (x < lines[y].Length) ? lines[y][x] : ' ';
                }
            }

            string posLine = lines[lines.Length - 2].Trim();
            var parts = posLine.Split(',');
            if (parts.Length != 2) return null;

            if (!int.TryParse(parts[0], out int posX)) return null;
            if (!int.TryParse(parts[1], out int posY)) return null;

            if (!int.TryParse(lines[lines.Length - 1].Trim(), out int loadedRoomsFound))
                loadedRoomsFound = 0;

            roomsFound = loadedRoomsFound;

            return (map, width, height, posX, posY);
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
            {
                if (line.Length > width)
                    width = line.Length;
            }

            char[,] map = new char[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    map[y, x] =
                        (x < lines[y].Length)
                        ? lines[y][x]
                        : ' ';
                }
            }

            btnRead.IsEnabled = false;

            return (map, width, height);
        }

        List<(int, int)> locate_entrances(char[,] map)
        {
            List<(int, int)> returnList = new List<(int, int)>();

            for (int x = 0; x < mapWidth; x++)
            {
                if (map[0, x] == '║')
                    returnList.Add((x, 0));

                if (map[mapHeight - 1, x] == '║')
                    returnList.Add((x, mapHeight - 1));
            }

            for (int y = 0; y < mapHeight; y++)
            {
                if (map[y, 0] == '═')
                    returnList.Add((0, y));

                if (map[y, mapWidth - 1] == '═')
                    returnList.Add((mapWidth - 1, y));
            }

            return returnList;
        }

        void update_language()
        {
            lbRead.Content =
                (lang == LANG.HUN)
                ? "Fájl beolvasása"
                : "Read file";

            btnRead.Content =
                (lang == LANG.HUN)
                ? "Olvasás"
                : "Read";

            lbLanguage.Content =
                (lang == LANG.HUN)
                ? "Nyelvezet"
                : "Language";

            rbEng.Content =
                (lang == LANG.HUN)
                ? "Angol"
                : "English";

            rbHun.Content =
                (lang == LANG.HUN)
                ? "Magyar"
                : "Hungarian";

            lbFoundRooms.Content =
                (lang == LANG.HUN)
                ? $"{roomsFound} / {allRooms} megtalálva!"
                : $"Found: {roomsFound} / {allRooms}!";

            lbInfo.Content = (lang == LANG.HUN)
                ? $"Lépések: "
                : $"Moves: ";

            if (gameTimer != null)
            {
                lbTimer.Content = (lang == LANG.HUN)
                    ? $"Idő: {remainingTime.ToString(@"mm\:ss")}"
                    : $"Time: {remainingTime.ToString(@"mm\:ss")}";
            }
            else
            {
                lbTimer.Content = (lang == LANG.HUN) ? "Idő: 01:00" : "Time: 01:00";
            }

            btnProgressiveFog.Content = (lang == LANG.HUN)
                ? $"Fokozatos felfedezés"
                : $"Progressive discovery";


            btnSave.Content = (lang == LANG.HUN)
                ? $"Mentéss"
                : $"Save";

            btnLoadGame.Content = (lang == LANG.HUN) ?
                "JÁTÉK BETÖLTÉSE"
                : "LOAD";

            update_directions(
                labyrinth != null
                ? labyrinth.Map[playerY, playerX]
                : ' ');
        }

        private void rbEng_Checked(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
                return;

            lang = LANG.ENG;

            update_language();
        }

        private void rbHun_Checked(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
                return;

            lang = LANG.HUN;

            update_language();
        }

        private void btnFogOfWar_Checked(object sender, RoutedEventArgs e)
        {
            fogOfWarEnabled = true;
            progressiveFogOfWar = false;

            discoveredTiles.Clear();
            discover_current_tile();

            render_map(currentMap, mapWidth, mapHeight);
            update_player();
        }

        private void btnFogOfWar_Unchecked(object sender, RoutedEventArgs e)
        {
            fogOfWarEnabled = false;
            progressiveFogOfWar = false;

            render_map(currentMap, mapWidth, mapHeight);
            update_player();
        }

        private void btnProgressiveFog_Checked(object sender, RoutedEventArgs e)
        {
            fogOfWarEnabled = true;
            progressiveFogOfWar = true;

            discoveredTiles.Clear();
            discover_current_tile();
            discover_surrounding_tiles(playerX, playerY);

            render_map(currentMap, mapWidth, mapHeight);
            update_player();
        }

        private void btnProgressiveFog_Unchecked(object sender, RoutedEventArgs e)
        {
            progressiveFogOfWar = false;

            if (!fogOfWarEnabled)
            {
                render_map(currentMap, mapWidth, mapHeight);
                update_player();
            }
        }
        private int save_game(char[,] map)
        {
            SaveFileDialog svd = new SaveFileDialog();
            if(svd.ShowDialog() != true)
            {
                return 1;
            }
            svd.AddExtension = true;
            svd.DefaultExt = ".SAV";
            using (StreamWriter sr = new StreamWriter(svd.FileName))
            {
                for(int row = 0; row < labyrinth.Height; row++)
                {
                    for(int col = 0; col < labyrinth.Width; col++)
                    {
                        sr.Write(map[row, col]);
                    }
                    sr.WriteLine();
                }

                sr.WriteLine($"{playerX},{playerY}");
                sr.WriteLine(roomsFound.ToString());
            }
            return 0;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if(labyrinth == null)
            {
                MessageBox.Show((lang == LANG.HUN) ? "Nem menthetsz megnyitás előtt" : "Cant save without opening");
                return;
            }
            if (save_game(labyrinth.Map) == 1)
            {
                MessageBox.Show((lang == LANG.HUN) ? "Error a file kinyitása során" : "Error during saving");
            }
            else
            {
                MessageBox.Show((lang == LANG.HUN) ? "Elmentve" : "Saved");

            }
        }

        private void btnLoadGame_Click(object sender, RoutedEventArgs e)
        {
            load_game();
        }
    }
}