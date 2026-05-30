using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace LabyrinthEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // A nyelvválasztó listájának egy eleme: a nyelvkód és a megjelenített név.
        private record LanguageOption(string Code, string Name);

        public MainWindow()
        {
            InitializeComponent();

            TileSelector.SelectedTileTypeChanged += OnSelectedTileTypeChanged;
            TileSelector.SelectedTileType = MapCanvas.SelectedTileType;

            LanguageSelector.ItemsSource = Localization.AvailableLanguages
                .Select(code => new LanguageOption(code, Localization.DisplayName(code)))
                .ToList();
            LanguageSelector.DisplayMemberPath = nameof(LanguageOption.Name);
            LanguageSelector.SelectedValuePath = nameof(LanguageOption.Code);
            LanguageSelector.SelectedValue = Localization.CurrentLanguage;

            Localization.LanguageChanged += ApplyLanguage;
            ApplyLanguage();
        }

        // Felülírja a felület szövegeit az aktuális nyelvnek megfelelően.
        void ApplyLanguage()
        {
            Title = Localization.Get("window.title");
            ExportButton.Content = Localization.Get("button.export");
            LanguageLabel.Text = Localization.Get("label.language");
        }

        void OnLanguageSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LanguageSelector.SelectedValue is string code)
            {
                Localization.CurrentLanguage = code;
            }
        }

        void OnSelectedTileTypeChanged(object? sender, TileType? tileType)
        {
            MapCanvas.SelectedTileType = tileType;
        }

        void OnExportClick(object sender, RoutedEventArgs e)
        {
            string? content = MapCanvas.BuildExportContent(out string? error);

            if (content == null)
            {
                MessageBox.Show(error ?? Localization.Get("error.unknown"), Localization.Get("export.title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = Localization.Get("export.filter"),
                DefaultExt = ".SAV",
                FileName = "palya.SAV",
                Title = Localization.Get("export.dialogTitle"),
                RestoreDirectory = true,
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                // BOM nélküli UTF-8, hogy a játék ReadAllLines hívása helyesen olvassa be.
                File.WriteAllText(dlg.FileName, content, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.Get("export.saveError", ex.Message), Localization.Get("export.title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public record struct Position
    {
        public int x;
        public int y;

        public Position(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Position NeighourAt(Direction dir)
        {
            return (dir) switch
            {
                Direction.North => new Position(x, y-1),
                Direction.East  => new Position(x+1, y),
                Direction.South => new Position(x, y+1),
                Direction.West  => new Position(x-1, y),
                _ => throw new ArgumentException()
            };
        }

        public Direction? DirectionTo(Position other)
        {
            int dx = other.x - x;
            int dy = other.y - y;

            return (dx, dy) switch
            {
                ( 0,-1) => Direction.North,
                ( 1, 0) => Direction.East,
                ( 0, 1) => Direction.South,
                (-1, 0) => Direction.West,
                _ => null
            };
        }
    }

    public enum Direction
    {
        North = 0b1000,
        East  = 0b0100,
        South = 0b0010,
        West  = 0b0001,
    }
    
    public static class DirectionExtensions
    {
        public static Direction Opposite(this Direction dir) => dir switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            _ => throw new ArgumentException()
        };
    }

    public record struct Tile
    {
        public TileType type;
        public byte? directions;

        public Tile(TileType type, byte? directions)
        {
            this.type = type;
            this.directions = directions;
        }
    }

    public enum TileType
    {
        Wall,
        Path,
        Room,
        Player,
    }

    public static class TileTypeExtensions
    {
        public static bool IsDirected(this TileType type) => type switch
        {
            TileType.Wall => false,
            TileType.Path => true,
            TileType.Room => true,
            TileType.Player => true,
            _ => false,
        };
    }
}