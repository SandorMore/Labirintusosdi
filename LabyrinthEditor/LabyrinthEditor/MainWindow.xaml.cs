using System.Windows;

namespace LabyrinthEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            TileSelector.SelectedTileTypeChanged += OnSelectedTileTypeChanged;
            TileSelector.SelectedTileType = MapCanvas.SelectedTileType;
        }

        void OnSelectedTileTypeChanged(object? sender, TileType? tileType)
        {
            MapCanvas.SelectedTileType = tileType;
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
    }

    public static class TileTypeExtensions
    {
        public static bool IsDirected(this TileType type) => type switch
        {
            TileType.Wall => false,
            TileType.Path => true,
            TileType.Room => true,
            _ => false,
        };
    }
}