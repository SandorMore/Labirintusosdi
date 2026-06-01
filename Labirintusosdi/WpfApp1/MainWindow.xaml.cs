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
using System.Windows.Threading;
using WpfApp1.Models;
using WpfApp1.Utils;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        Labyrinth labyrinth;
        char[,] currentMap;
        int mapWidth, mapHeight;

        Rectangle player;
        int playerX = 1;
        int playerY = 1;

        const int TILESIZE = 25;

        int allRooms = 0;
        int roomsFound = 0;
        bool canExitDungeon = false;

        List<(int, int)> entranceList = new List<(int, int)>();
        HashSet<(int, int)> visitedRooms = new HashSet<(int, int)>();
        HashSet<(int, int)> discoveredTiles = new HashSet<(int, int)>();

        bool fogOfWarEnabled = false;
        bool progressiveFogOfWar = false;

        DispatcherTimer gameTimer;
        TimeSpan remainingTime;

        LANG lang = LANG.ENG;
        bool isLoaded = false;

        public MainWindow()
        {
            InitializeComponent();

            isLoaded = true;

            this.Loaded += (s, e) => Keyboard.Focus(this);
            this.KeyDown += MainWindow_KeyDown;

            rbEng.IsChecked = true;

            lbDirectionsFromCurrent.Content = Loc("You can go towards: ", "Ezekbe az irányokba indulhat: ");
        }

        string Loc(string eng, string hun) => lang == LANG.HUN ? hun : eng;

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (labyrinth == null) return;

            char currentTile = labyrinth.Map[playerY, playerX];

            add_room();
            update_directions(currentTile);

            if (!can_move(currentTile, e.Key)) return;

            int newX = playerX;
            int newY = playerY;

            switch (e.Key)
            {
                case Key.W: newY--; break;
                case Key.S: newY++; break;
                case Key.A: newX--; break;
                case Key.D: newX++; break;
                default: return;
            }

            if (newX < 0 || newY < 0 || newX >= mapWidth || newY >= mapHeight)
            {
                try_exit_dungeon();
                return;
            }

            char targetTile = labyrinth.Map[newY, newX];
            if (!can_enter(targetTile, e.Key)) return;

            move_player(newX, newY);
        }

        void try_exit_dungeon()
        {
            if (!is_entrance(playerX, playerY)) return;

            string question = (!canExitDungeon)
                ? Loc("You have not found any rooms yet. Are you sure you want to leave?",
                      "Még nem találtál szobát. Biztosan ki akarsz lépni?")
                : Loc("Are you sure you want to leave the labyrinth?",
                      "Biztosan ki akarsz lépni a labirintusból?");

            var result = MessageBox.Show(question, "Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            MessageBox.Show(Loc("You escaped the labyrinth!", "Sikeresen kijutottál a labirintusból!"));
            Close();
        }

        bool can_move(char tile, Key direction)
        {
            if (tile == '█') return true;

            return direction switch
            {
                Key.W => "║╬╩╣╠╝╚".Contains(tile),

                Key.S => "║╬╦╣╠╗╔".Contains(tile),

                Key.A => "═╬╩╦╣╗╝".Contains(tile),

                Key.D => "═╬╩╦╠╔╚".Contains(tile),
                _ => false,
            };
        }

        bool can_enter(char tile, Key direction)
        {
            if (tile == '█') return true;

            return direction switch
            {
                Key.W => can_move(tile, Key.S),
                Key.S => can_move(tile, Key.W),
                Key.A => can_move(tile, Key.D),
                Key.D => can_move(tile, Key.A),
                _ => false,
            };
        }

        bool is_entrance(int x, int y) => entranceList.Contains((x, y));

        void move_player(int newX, int newY)
        {
            playerX = newX;
            playerY = newY;

            discover_current_tile();
            if (progressiveFogOfWar)
                discover_surrounding_tiles(newX, newY);

            render_map(currentMap, mapWidth, mapHeight);
            update_player();
            add_room();

            lbMoves.Items.Add($"({playerY},{playerX})");
            update_directions(labyrinth.Map[playerY, playerX]);
        }

        void add_room()
        {
            if (currentMap[playerY, playerX] != '█') return;
            if (visitedRooms.Contains((playerX, playerY))) return;

            visitedRooms.Add((playerX, playerY));
            roomsFound++;

            if (roomsFound >= 1)
                canExitDungeon = true;

            lbFoundRooms.Content = Loc($"Found: {roomsFound} / {allRooms}!", $"{roomsFound} / {allRooms} megtalálva!");
        }

        void update_directions(char tile)
        {
            var dirs = new List<string>();

            if (can_move(tile, Key.W)) dirs.Add(Loc("North (up)", "Észak (fel)"));
            if (can_move(tile, Key.S)) dirs.Add(Loc("South (down)", "Dél (le)"));
            if (can_move(tile, Key.A)) dirs.Add(Loc("West (left)", "Nyugat (bal)"));
            if (can_move(tile, Key.D)) dirs.Add(Loc("East (right)", "Kelet (jobb)"));

            lbDirectionsFromCurrent.Content = dirs.Count == 0
                ? Loc("No available directions", "Nincs elérhető irány")
                : Loc($"You can go towards: {string.Join(", ", dirs)}",
                      $"Ezekbe az irányokba indulhat: {string.Join(", ", dirs)}");
        }

        void discover_current_tile() =>
            discoveredTiles.Add((playerX, playerY));

        void discover_surrounding_tiles(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight)
                        discoveredTiles.Add((nx, ny));
                }
        }

        void render_map(char[,] map, int width, int height)
        {
            gameCanvas.Children.Clear();

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool visible = !fogOfWarEnabled || discoveredTiles.Contains((x, y));

                    var tile = new TextBlock
                    {
                        Text = visible ? map[y, x].ToString() : " ",
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 20,
                        Foreground = visible ? Brushes.White : Brushes.Gray,
                        Background = visible ? Brushes.Transparent : Brushes.Black,
                    };

                    Canvas.SetLeft(tile, x * TILESIZE);
                    Canvas.SetTop(tile, y * TILESIZE);
                    gameCanvas.Children.Add(tile);
                }

            if (player != null)
            {
                if (!gameCanvas.Children.Contains(player))
                    gameCanvas.Children.Add(player);

                Canvas.SetLeft(player, playerX * TILESIZE);
                Canvas.SetTop(player, playerY * TILESIZE);
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
                StrokeThickness = 1,
            };

            Canvas.SetLeft(player, playerX * TILESIZE);
            Canvas.SetTop(player, playerY * TILESIZE);
            gameCanvas.Children.Add(player);
        }

        void update_player()
        {
            if (player == null) return;
            Canvas.SetLeft(player, playerX * TILESIZE);
            Canvas.SetTop(player, playerY * TILESIZE);
        }

        void start_game_timer()
        {
            stop_game_timer();
            remainingTime = TimeSpan.FromMinutes(1);

            gameTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            gameTimer.Tick += (s, e) =>
            {
                remainingTime -= TimeSpan.FromSeconds(1);
                lbTimer.Content = Loc($"Time: {remainingTime:mm\\:ss}", $"Idő: {remainingTime:mm\\:ss}");

                if (remainingTime <= TimeSpan.Zero)
                {
                    stop_game_timer();
                    on_timer_elapsed();
                }
            };

            lbTimer.Content = Loc($"Time: {remainingTime:mm\\:ss}", $"Idő: {remainingTime:mm\\:ss}");
            gameTimer.Start();
        }

        void stop_game_timer()
        {
            gameTimer?.Stop();
            gameTimer = null;
        }

        void on_timer_elapsed()
        {
            MessageBox.Show(Loc("Time's up. You lost!", "Lejárt az idő. Vesztettél!"));
            btnRead.IsEnabled = true;
            labyrinth = null;
            gameCanvas.Children.Clear();
        }


        (char[,] map, int width, int height)? read_map()
        {
            var ofd = new OpenFileDialog
            {
                RestoreDirectory = true,
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
            };

            if (ofd.ShowDialog() != true) return null;

            string[] lines;
            try { lines = File.ReadAllLines(ofd.FileName, Encoding.UTF8); }
            catch { return null; }

            if (lines.Length == 0) return null;

            int height = lines.Length;
            int width = lines.Max(l => l.Length);
            var map = new char[height, width];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    map[y, x] = x < lines[y].Length ? lines[y][x] : ' ';

            return (map, width, height);
        }

        List<(int, int)> locate_entrances(char[,] map)
        {
            var entrances = new HashSet<(int, int)>();

            for (int x = 0; x < mapWidth; x++)
            {
                char top = map[0, x];
                char bottom = map[mapHeight - 1, x];

                if (top != '.' && top != '╦' && top != '╔' && top != '╗' && top != '═')
                    entrances.Add((x, 0));
                if (bottom != '.' && bottom != '╦' && bottom != '╔' && bottom != '╗' && bottom != '═')
                    entrances.Add((x, mapHeight - 1));
            }

            for (int y = 0; y < mapHeight; y++)
            {
                if (map[y, 0] != '.' && map[y, 0] != '╔' && map[y, 0] != '╠')
                    entrances.Add((0, y));
                if (map[y, mapWidth - 1] != '.' && map[y, mapWidth - 1] != '╗' && map[y, mapWidth - 1] != '╣')
                    entrances.Add((mapWidth - 1, y));
            }

            return entrances.ToList();
        }

        (int x, int y) setup_game(List<(int, int)> entrances)
        {
            var rnd = new Random();
            return entrances[rnd.Next(entrances.Count)];
        }

        void initialise_map(char[,] map, int width, int height, int startX, int startY)
        {
            currentMap = map;
            mapWidth = width;
            mapHeight = height;

            labyrinth = new Labyrinth(mapWidth, mapHeight, currentMap);

            visitedRooms.Clear();
            discoveredTiles.Clear();
            roomsFound = 0;
            canExitDungeon = false;

            allRooms = labyrinth.get_room_number();
            playerX = startX;
            playerY = startY;

            entranceList = locate_entrances(currentMap);

            discover_current_tile();
            if (progressiveFogOfWar)
                discover_surrounding_tiles(playerX, playerY);

            render_map(currentMap, mapWidth, mapHeight);
            create_player();

            lbFoundRooms.Content = Loc($"Found: {roomsFound} / {allRooms}!", $"{roomsFound} / {allRooms} megtalálva!");

            try { gameCanvas.Focus(); Keyboard.Focus(gameCanvas); } catch { }
        }

        bool save_game(char[,] map)
        {
            var svd = new SaveFileDialog
            {
                Filter = "SAV files (*.SAV)|*.SAV",
                DefaultExt = ".SAV",
                AddExtension = true,
                RestoreDirectory = true,
                Title = Loc("Save (.SAV)", "Mentés (.SAV)"),
            };

            if (svd.ShowDialog() != true) return false;

            string fileName = svd.FileName;
            if (!fileName.EndsWith(".SAV", StringComparison.OrdinalIgnoreCase))
                fileName += ".SAV";

            using var sw = new StreamWriter(fileName, false, Encoding.UTF8);

            for (int row = 0; row < labyrinth.Height; row++)
            {
                for (int col = 0; col < labyrinth.Width; col++)
                    sw.Write(map[row, col]);
                sw.WriteLine();
            }
            sw.WriteLine($"POS:{playerX},{playerY}");

            sw.WriteLine($"ROOMS:{roomsFound}");

            var visitedParts = visitedRooms.Select(r => $"{r.Item1},{r.Item2}");
            sw.WriteLine($"VISITED:{string.Join(";", visitedParts)}");

            return true;
        }

        bool load_game()
        {
            var ofd = new OpenFileDialog
            {
                RestoreDirectory = true,
                Filter = "SAV files (*.SAV)|*.SAV",
                Title = Loc("Open (.SAV)", "Megnyitás (.SAV)"),
            };

            if (ofd.ShowDialog() != true) return false;

            if (!ofd.FileName.EndsWith(".SAV", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(Loc("Please select a .SAV file", "Kérlek .SAV fájlt válassz"));
                return false;
            }

            string[] lines;
            try { lines = File.ReadAllLines(ofd.FileName, Encoding.UTF8); }
            catch (Exception ex)
            {
                MessageBox.Show(Loc($"Error reading file: {ex.Message}", $"Hiba a fájl olvasásakor: {ex.Message}"));
                return false;
            }

            if (lines.Length < 3)
            {
                MessageBox.Show(Loc("Invalid save file (too short)", "Érvénytelen mentési fájl (túl rövid)"));
                return false;
            }

            string visitedLine = lines[lines.Length - 1].Trim();
            string roomsLine = lines[lines.Length - 2].Trim();
            string posLine = lines[lines.Length - 3].Trim();

            if (!posLine.StartsWith("POS:") ||
                !TryParsePair(posLine.Substring(4), out int posX, out int posY))
            {
                MessageBox.Show(Loc("Invalid position in save file", "Érvénytelen pozíció a mentési fájlban"));
                return false;
            }

            int loadedRoomsFound = 0;
            if (roomsLine.StartsWith("ROOMS:"))
                int.TryParse(roomsLine.Substring(6), out loadedRoomsFound);

            var loadedVisited = new HashSet<(int, int)>();
            if (visitedLine.StartsWith("VISITED:"))
            {
                string payload = visitedLine.Substring(8);
                if (!string.IsNullOrWhiteSpace(payload))
                {
                    foreach (string part in payload.Split(';'))
                    {
                        if (TryParsePair(part, out int vx, out int vy))
                            loadedVisited.Add((vx, vy));
                    }
                }
            }
            int mapLineCount = lines.Length - 3;
            if (mapLineCount <= 0)
            {
                MessageBox.Show(Loc("Invalid save file (no map data)", "Érvénytelen mentési fájl (nincs térképadat)"));
                return false;
            }

            int height = mapLineCount;
            int width = 0;
            for (int i = 0; i < height; i++)
                if (lines[i].Length > width) width = lines[i].Length;

            var map = new char[height, width];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    map[y, x] = x < lines[y].Length ? lines[y][x] : ' ';

            initialise_map(map, width, height, posX, posY);

            visitedRooms = loadedVisited;
            roomsFound = loadedRoomsFound;
            canExitDungeon = roomsFound >= 1;

            lbFoundRooms.Content = Loc($"Found: {roomsFound} / {allRooms}!", $"{roomsFound} / {allRooms} megtalálva!");

            start_game_timer();
            return true;
        }

        static bool TryParsePair(string s, out int a, out int b)
        {
            a = b = 0;
            var parts = s.Split(',');
            return parts.Length >= 2 &&
                   int.TryParse(parts[0].Trim(), out a) &&
                   int.TryParse(parts[1].Trim(), out b);
        }

        void update_language()
        {
            lbRead.Content = Loc("Read file", "Fájl beolvasása");
            btnRead.Content = Loc("Read", "Olvasás");
            lbLanguage.Content = Loc("Language", "Nyelvezet");
            rbEng.Content = Loc("English", "Angol");
            rbHun.Content = Loc("Hungarian", "Magyar");

            lbFoundRooms.Content = Loc($"Found: {roomsFound} / {allRooms}!", $"{roomsFound} / {allRooms} megtalálva!");
            lbInfo.Content = Loc("Moves: ", "Lépések: ");

            lbTimer.Content = gameTimer != null
                ? Loc($"Time: {remainingTime:mm\\:ss}", $"Idő: {remainingTime:mm\\:ss}")
                : Loc("Time: 01:00", "Idő: 01:00");

            btnProgressiveFog.Content = Loc("Progressive discovery", "Fokozatos felfedezés");
            btnSave.Content = Loc("Save", "Mentés");
            btnLoadGame.Content = Loc("Load", "JÁTÉK BETÖLTÉSE");

            update_directions(labyrinth != null ? labyrinth.Map[playerY, playerX] : ' ');
        }

        private void Button_Click(object sender, RoutedEventArgs e)         
        {
            var res = read_map();
            if (res == null) return;

            var startPos = setup_game(locate_entrances_from(res.Value.map, res.Value.width, res.Value.height));
            initialise_map(res.Value.map, res.Value.width, res.Value.height, startPos.x, startPos.y);
            btnRead.IsEnabled = false;
            start_game_timer();
            update_directions(labyrinth.Map[playerY, playerX]);
        }

        List<(int, int)> locate_entrances_from(char[,] map, int w, int h)
        {
            int oldW = mapWidth, oldH = mapHeight;
            mapWidth = w; mapHeight = h;
            var result = locate_entrances(map);
            mapWidth = oldW; mapHeight = oldH;
            return result;
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
            fogOfWarEnabled = false;
            progressiveFogOfWar = false;
            render_map(currentMap, mapWidth, mapHeight);
            update_player();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (labyrinth == null)
            {
                MessageBox.Show(Loc("Can't save without opening a map", "Nem menthetsz megnyitás előtt"));
                return;
            }

            bool ok = save_game(labyrinth.Map);
            MessageBox.Show(ok
                ? Loc("Saved", "Elmentve")
                : Loc("Error during saving", "Error a file mentése során"));
        }

        private void btnLoadGame_Click(object sender, RoutedEventArgs e)
        {
            if (!load_game())
                MessageBox.Show(Loc("Error", "Hiba történt"));
        }
    }
}