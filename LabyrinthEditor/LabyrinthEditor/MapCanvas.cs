using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabyrinthEditor
{
    public class MapCanvas : FrameworkElement
    {
        const int CELL_SIZE = 20;
        const int GAP_SIZE = 2;

        public TileType? SelectedTileType { get; set; }
        public Dictionary<Position, Tile> Map { get; private set; }

        double zoomScale = 1;
        double transformX = 0;
        double transformY = 0;

        private Point lastMousePos;
        private Action currentAction;

        private BitmapSource[] pathBitmaps;
        private BitmapSource[] roomBitmaps;
        private BitmapSource missingTextureBitmap;

        private Color wallColor;
        private Color pathColor;
        private Color roomColor;

        enum Action
        {
            None,
            Pan,
            Draw,
        }

        public MapCanvas()
        {
            Map = new();
            SelectedTileType = TileType.Path;

            wallColor = Color.FromRgb(50, 50, 50);
            pathColor = Colors.Gray;
            roomColor = Colors.Yellow;

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;

            pathBitmaps = MakePathBitmaps();
            roomBitmaps = MakeRoomBitmaps();
            missingTextureBitmap = MakeMissingTextureBitmap();

            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight)); // Background

            double actualCellSize = CELL_SIZE * zoomScale;

            int firstVisibleCellX = (int)Math.Floor(-transformX / actualCellSize);
            int firstVisibleCellY = (int)Math.Floor(-transformY / actualCellSize);

            int lastVisibleCellX = (int)Math.Ceiling((ActualWidth - transformX) / actualCellSize);
            int lastVisibleCellY = (int)Math.Ceiling((ActualHeight - transformY) / actualCellSize);

            for (int x = firstVisibleCellX; x < lastVisibleCellX; ++x)
            {
                for (int y = firstVisibleCellY; y < lastVisibleCellY; ++y)
                {
                    Position cellPos = new Position(x, y);

                    if (!Map.ContainsKey(cellPos)) continue;

                    double cellLeft = transformX + x * actualCellSize;
                    double cellTop = transformY + y * actualCellSize;

                    double croppedLeft = Math.Max(0, cellLeft);
                    double croppedTop = Math.Max(0, cellTop);
                    double croppedRight = Math.Min(ActualWidth, cellLeft+actualCellSize);
                    double croppedBottom = Math.Min(ActualHeight, cellTop+actualCellSize);

                    double croppedWidth = Math.Max(0, croppedRight-croppedLeft);
                    double croppedHeight = Math.Max(0, croppedBottom-croppedTop);

                    if (Map[cellPos].type == TileType.Wall)
                    {
                        //drawingContext.DrawRectangle(Brushes.Gray, null, new Rect(Math.Max(0, transformX + x * CELL_SIZE * zoomScale), Math.Max(0, transformY + y * CELL_SIZE * zoomScale), Math.Clamp(Math.Min(Math.Abs(transformX+(x+1)*CELL_SIZE*zoomScale), Math.Abs(transformX-ActualWidth+x*CELL_SIZE*zoomScale)), 0, CELL_SIZE*zoomScale), Math.Clamp(Math.Min(Math.Abs(transformY+(y+1)*CELL_SIZE*zoomScale), Math.Abs(transformY-ActualHeight+y*CELL_SIZE*zoomScale)), 0, CELL_SIZE*zoomScale)));

                        drawingContext.DrawRectangle(new SolidColorBrush(wallColor), null, new Rect(croppedLeft, croppedTop, croppedWidth, croppedHeight));
                    }

                    else if (Map[cellPos].type == TileType.Path || Map[cellPos].type == TileType.Room)
                    {
                        BitmapSource[] bitmaps = Map[cellPos].type switch
                        {
                            TileType.Path => pathBitmaps,
                            TileType.Room => roomBitmaps,
                            _ => throw new ArgumentException()
                        };

                        BitmapSource bmp = bitmaps[Map[cellPos].directions ?? 0];

                        drawingContext.PushClip(new RectangleGeometry(new Rect(croppedLeft, croppedTop, croppedWidth, croppedHeight)));
                        drawingContext.DrawImage(bmp, new Rect(cellLeft, cellTop, actualCellSize, actualCellSize));
                        drawingContext.Pop();
                    }

                    else // Unknown tile
                    {
                        drawingContext.PushClip(new RectangleGeometry(new Rect(croppedLeft, croppedTop, croppedWidth, croppedHeight)));
                        drawingContext.DrawImage(missingTextureBitmap, new Rect(cellLeft, cellTop, actualCellSize, actualCellSize));
                        drawingContext.Pop();
                    }
                }
            }

            drawingContext.DrawRectangle(Brushes.Red, null, new Rect(transformX, transformY, 10, 10));

            DrawGridLines(drawingContext);
        }

        void DrawGridLines(DrawingContext dc)
        {
            Pen pen = new Pen(Brushes.Black, GAP_SIZE);
            pen.Freeze();

            double actualCellSize = CELL_SIZE * zoomScale;

            double PosMod(double a, double b) => ((a % b) + b) % b;

            for (double x = PosMod(transformX, actualCellSize); x < ActualWidth; x += actualCellSize)
            {
                dc.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
            }

            for (double y = PosMod(transformY, actualCellSize); y < ActualHeight; y += actualCellSize)
            {
                dc.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
            }
        }

        BitmapSource[] MakePathBitmaps()
        {
            BitmapSource[] bitmaps = new BitmapSource[16];

            BitmapPalette palette = new BitmapPalette([wallColor, pathColor]);

            for (int i = 0; i < 16; ++i)
            {
                bool n = (i & (byte)Direction.North) != 0;
                bool e = (i & (byte)Direction.East ) != 0;
                bool s = (i & (byte)Direction.South) != 0;
                bool w = (i & (byte)Direction.West ) != 0;

                WriteableBitmap bmp = new WriteableBitmap(4, 4, 96, 96, PixelFormats.Indexed1, palette);

                byte[] bytes = new byte[4]
                {
                    0b00000000,
                    0b01100000,
                    0b01100000,
                    0b00000000,
                };

                if (n) {
                    bytes[0] |= 0b01100000;
                }
                if (e) {
                    bytes[1] |= 0b00010000;
                    bytes[2] |= 0b00010000;
                }
                if (s) {
                    bytes[3] |= 0b01100000;
                }
                if (w) {
                    bytes[1] |= 0b10000000;
                    bytes[2] |= 0b10000000;
                }

                bmp.WritePixels(new Int32Rect(0, 0, 4, 4), bytes, 1, 0);
                bmp.Freeze();

                bitmaps[i] = bmp;
            }

            return bitmaps;
        }

        BitmapSource[] MakeRoomBitmaps()
        {
            BitmapSource[] bitmaps = new BitmapSource[16];

            BitmapPalette palette = new BitmapPalette([wallColor, pathColor, roomColor]);

            for (int i = 0; i < 16; ++i)
            {
                bool n = (i & (byte)Direction.North) != 0;
                bool e = (i & (byte)Direction.East) != 0;
                bool s = (i & (byte)Direction.South) != 0;
                bool w = (i & (byte)Direction.West) != 0;

                WriteableBitmap bmp = new WriteableBitmap(8, 8, 96, 96, PixelFormats.Indexed2, palette);

                byte[] bytes = new byte[16]
                {
                    0b00000000,0b00000000,
                    0b00000000,0b00000000,
                    0b00000101,0b01010000,
                    0b00000110,0b10010000,
                    0b00000110,0b10010000,
                    0b00000101,0b01010000,
                    0b00000000,0b00000000,
                    0b00000000,0b00000000,
                };

                if (n)
                {
                    bytes[0] |= 0b00000101; bytes[1] |= 0b01010000;
                    bytes[2] |= 0b00000101; bytes[3] |= 0b01010000;
                }
                if (e)
                {
                    bytes[5]  |= 0b00000101;
                    bytes[7]  |= 0b00000101;
                    bytes[9]  |= 0b00000101;
                    bytes[11] |= 0b00000101;
                }
                if (s)
                {
                    bytes[12] |= 0b00000101; bytes[13] |= 0b01010000;
                    bytes[14] |= 0b00000101; bytes[15] |= 0b01010000;
                }
                if (w)
                {
                    bytes[4]  |= 0b01010000;
                    bytes[6]  |= 0b01010000;
                    bytes[8]  |= 0b01010000;
                    bytes[10] |= 0b01010000;
                }

                bmp.WritePixels(new Int32Rect(0, 0, 8, 8), bytes, 2, 0);
                bmp.Freeze();

                bitmaps[i] = bmp;
            }

            return bitmaps;
        }

        BitmapSource MakeMissingTextureBitmap()
        {
            WriteableBitmap bmp = new WriteableBitmap(2, 2, 96, 96, PixelFormats.Indexed1, new BitmapPalette([Colors.Black, Colors.Magenta]));

            byte[] bytes = new byte[2]
            {
                0b01000000,
                0b10000000,
            };

            bmp.WritePixels(new Int32Rect(0, 0, 2, 2), bytes, 1, 0);
            bmp.Freeze();

            return bmp;
        }

        Position ScreenToGrid(Point p)
        {
            double actualCellSize = CELL_SIZE * zoomScale;

            return new Position(
                (int)Math.Floor((p.X - transformX) / actualCellSize),
                (int)Math.Floor((p.Y - transformY) / actualCellSize)
            );
        }

        bool PlaceTileRaw(TileType? tileType, Position position, byte? directions)
        {
            bool changedTile = false;

            if (tileType == null)
            {
                changedTile = Map.Remove(position);
            }
            else
            {
                Tile newTile = new Tile(tileType.Value, tileType.Value.IsDirected() ? directions : null);
                changedTile = !Map.ContainsKey(position) || Map[position] != newTile;
                Map[position] = newTile;
            }

            if (changedTile)
            {
                InvalidateVisual();
            }

            return changedTile;
        }

        bool PlaceTile(TileType? tileType, Position position, byte? directions)
        {
            bool changedTile = PlaceTileRaw(tileType, position, directions);
            bool changedSurroundingTiles = false;

            if (tileType == null || !tileType.Value.IsDirected()) directions = null;

            foreach (Direction dir in Enum.GetValues(typeof(Direction)))
            {
                Position nPos = position.NeighourAt(dir);

                if (Map.ContainsKey(nPos) && Map[nPos].type.IsDirected())
                {
                    byte oldDirections = Map[nPos].directions ?? 0;
                    Map[nPos] = Map[nPos] with { directions = ((directions ?? 0) & (byte)dir) != 0 ? (byte)(oldDirections | (byte)dir.Opposite()) : (byte)(oldDirections & ~(byte)dir.Opposite()) };

                    changedSurroundingTiles = true;
                }
            }

            if (changedSurroundingTiles)
            {
                InvalidateVisual();
            }

            return changedTile || changedSurroundingTiles;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (currentAction != Action.None) return;

            if (e.ChangedButton == MouseButton.Middle)
            {
                currentAction = Action.Pan;
                lastMousePos = e.GetPosition(this);
                CaptureMouse();
            }

            else if (e.ChangedButton == MouseButton.Left)
            {
                currentAction = Action.Draw;
                lastMousePos = e.GetPosition(this);
                CaptureMouse();

                Position currentCellPos = ScreenToGrid(e.GetPosition(this));

                PlaceTile(SelectedTileType, currentCellPos, Map.ContainsKey(currentCellPos) ? Map[currentCellPos].directions : null);
            }

            else if (e.ChangedButton == MouseButton.Right)
            {
                MessageBox.Show($"Test");
            }

            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (currentAction == Action.Pan)
            {
                Point mousePos = e.GetPosition(this);
                transformX += mousePos.X - lastMousePos.X;
                transformY += mousePos.Y - lastMousePos.Y;
                lastMousePos = mousePos;
                InvalidateVisual();
            }

            else if (currentAction == Action.Draw)
            {
                Point mousePos = e.GetPosition(this);

                Position lastCellPos = ScreenToGrid(lastMousePos);
                Position currentCellPos = ScreenToGrid(mousePos);

                Direction? cameFrom = lastCellPos.DirectionTo(currentCellPos);

                if (cameFrom != null && Map.ContainsKey(lastCellPos) && Map[lastCellPos].type.IsDirected())
                {
                    PlaceTile(SelectedTileType, currentCellPos, Map.ContainsKey(currentCellPos) ? (byte)((Map[currentCellPos].directions ?? 0) | (byte)cameFrom.Value.Opposite()) : (byte)cameFrom.Value.Opposite());
                }
                else
                {
                    PlaceTile(SelectedTileType, currentCellPos, Map.ContainsKey(currentCellPos) ? Map[currentCellPos].directions : null);
                }

                lastMousePos = mousePos;
            }

            e.Handled = true;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if ((e.ChangedButton == MouseButton.Middle && currentAction == Action.Pan) || (e.ChangedButton == MouseButton.Left && currentAction == Action.Draw))
            {
                currentAction = Action.None;
                ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = e.Delta > 0 ? 1.1 : 1.0 / 1.1;

            Point mousePos = e.GetPosition(this);

            transformX = ((transformX - mousePos.X) * factor) + mousePos.X;
            transformY = ((transformY - mousePos.Y) * factor) + mousePos.Y;
            zoomScale = Math.Clamp(zoomScale * factor, 0.1, 20.0);

            InvalidateVisual();

            e.Handled = true;
        }
    }
}
