using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
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

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;

            pathBitmaps = MakePathBitmaps();

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

                        drawingContext.DrawRectangle(Brushes.Gray, null, new Rect(croppedLeft, croppedTop, croppedWidth, croppedHeight));
                    }

                    else if (Map[cellPos].type == TileType.Path)
                    {
                        BitmapSource bmp = pathBitmaps[Map[cellPos].directions ?? 0];

                        drawingContext.PushClip(new RectangleGeometry(new Rect(croppedLeft, croppedTop, croppedWidth, croppedHeight)));
                        drawingContext.DrawImage(bmp, new Rect(cellLeft, cellTop, actualCellSize, actualCellSize));
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

            BitmapPalette palette = new BitmapPalette([Colors.Black, Colors.Gray]);

            for (int i = 0; i < 16; ++i)
            {
                bool n = (i & (byte)Direction.North) != 0;
                bool e = (i & (byte)Direction.East ) != 0;
                bool s = (i & (byte)Direction.South) != 0;
                bool w = (i & (byte)Direction.West ) != 0;

                WriteableBitmap bmp = new WriteableBitmap(8, 8, 96, 96, PixelFormats.Indexed1, palette);

                byte[] bytes = new byte[8]
                {
                    0b00000000,
                    0b00000000,
                    0b00111100,
                    0b00111100,
                    0b00111100,
                    0b00111100,
                    0b00000000,
                    0b00000000,
                };

                if (n) {
                    bytes[0] |= 0b00111100;
                    bytes[1] |= 0b00111100;
                }
                if (e) {
                    bytes[2] |= 0b00000011;
                    bytes[3] |= 0b00000011;
                    bytes[4] |= 0b00000011;
                    bytes[5] |= 0b00000011;
                }
                if (s) {
                    bytes[6] |= 0b00111100;
                    bytes[7] |= 0b00111100;
                }
                if (w) {
                    bytes[2] |= 0b11000000;
                    bytes[3] |= 0b11000000;
                    bytes[4] |= 0b11000000;
                    bytes[5] |= 0b11000000;
                }

                bmp.WritePixels(new Int32Rect(0, 0, 8, 8), bytes, 1, 0);
                bmp.Freeze();

                bitmaps[i] = bmp;
            }

            return bitmaps;
        }

        Position ScreenToGrid(Point p)
        {
            double actualCellSize = CELL_SIZE * zoomScale;

            return new Position(
                (int)Math.Floor((p.X - transformX) / actualCellSize),
                (int)Math.Floor((p.Y - transformY) / actualCellSize)
            );
        }

        bool PlaceTile(TileType? tileType, Position position, byte? directions)
        {
            bool changedTile = false;

            if (tileType == null)
            {
                changedTile = Map.Remove(position);
            }
            else
            {
                Tile newTile = new Tile(tileType.Value, directions);
                changedTile = !Map.ContainsKey(position) || Map[position] != newTile;
                Map[position] = newTile;
            }

            if (changedTile)
            {
                InvalidateVisual();
            }

            return changedTile;
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

                if (!Map.ContainsKey(currentCellPos)  || Map[currentCellPos].type != SelectedTileType)
                    PlaceTile(SelectedTileType, currentCellPos, null);
            }

            else if (e.ChangedButton == MouseButton.Right)
            {
                MessageBox.Show($"Test");
            }

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

                if (cameFrom != null && Map.ContainsKey(lastCellPos) && Map[lastCellPos].type == SelectedTileType)
                {
                    if (Map.ContainsKey(currentCellPos))
                    {
                        PlaceTile(SelectedTileType, currentCellPos, (byte)((Map[currentCellPos].directions ?? 0) | (byte)DirectionExtensions.Opposite(cameFrom.Value)));
                    } else
                    {
                        PlaceTile(SelectedTileType, currentCellPos, (byte)DirectionExtensions.Opposite(cameFrom.Value));
                    }

                    PlaceTile(SelectedTileType, lastCellPos, (byte)((Map[lastCellPos].directions ?? 0) | (byte)cameFrom.Value));
                }
                else
                {
                    PlaceTile(SelectedTileType, lastCellPos, Map.ContainsKey(currentCellPos) ? Map[currentCellPos].directions : null);
                }

                lastMousePos = mousePos;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if ((e.ChangedButton == MouseButton.Middle && currentAction == Action.Pan) || (e.ChangedButton == MouseButton.Left && currentAction == Action.Draw))
            {
                currentAction = Action.None;
                ReleaseMouseCapture();
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = e.Delta > 0 ? 1.1 : 1.0 / 1.1;

            Point mousePos = e.GetPosition(this);

            transformX = ((transformX - mousePos.X) * factor) + mousePos.X;
            transformY = ((transformY - mousePos.Y) * factor) + mousePos.Y;
            zoomScale = Math.Clamp(zoomScale * factor, 0.1, 20.0);

            InvalidateVisual();
        }
    }
}
