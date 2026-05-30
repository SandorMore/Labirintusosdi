using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabyrinthEditor
{
    public class TileSelector : FrameworkElement
    {
        public TileType? SelectedTileType {
            get => selectedTileIndex < SelectableTileTypes.Length ? SelectableTileTypes[selectedTileIndex] : null;
            set => selectedTileIndex = (uint)Array.FindIndex(SelectableTileTypes, x => x==value);
        }
        public TileType?[] SelectableTileTypes { get; set; }

        public event EventHandler<TileType?>? SelectedTileTypeChanged;

        private uint selectedTileIndex;

        public TileSelector()
        {
            SelectableTileTypes = Enum.GetValues<TileType>().Cast<TileType?>().Append(null).ToArray();

            MouseDown += OnMouseDown;
            MouseWheel += OnMouseWheel;

            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight)); // Background

            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualWidth)));

            //drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 10, 10));

            
            for (int i=0; i<SelectableTileTypes.Length; ++i)
            {
                BitmapSource bmp;

                if (SelectableTileTypes[i] == TileType.Wall)
                {
                    WriteableBitmap wb = new WriteableBitmap(16, 16, 96, 96, PixelFormats.Indexed1, new BitmapPalette([Colors.Gray, Colors.Brown]));

                    byte[] bytes = new byte[32];

                    const int brickWidth = 8;
                    const int brickHeight = 3;
                    const int gap = 1;

                    for (int row=0; row<16; row+=brickHeight+gap)
                    {
                        int rowIndex = row / (brickHeight + gap);
                        int startX = rowIndex % 2 == 1 ? -(brickWidth/2) : 0;

                        for (int col=startX; col<16; col+=brickWidth+gap)
                        {
                            for (int bx=0; bx<brickWidth; ++bx)
                            {
                                for (int by=0; by<brickHeight; ++by)
                                {
                                    int x = col + bx;
                                    int y = row + by;

                                    if (x < 0 || x > 15 || y < 0 || y > 15) continue;

                                    bytes[y * 2 + x / 8] |= (byte)(1 << 7 - x % 8);
                                }
                            }
                        }
                    }

                    wb.WritePixels(new Int32Rect(0, 0, 16, 16), bytes, 2, 0);
                    wb.Freeze();

                    bmp = wb;
                }

                else if (SelectableTileTypes[i] == TileType.Path)
                {
                    WriteableBitmap wb = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Indexed4, new BitmapPalette([Colors.LightBlue, Colors.White, Colors.Gray, Colors.Black, Colors.Green]));

                    byte[] bytes = new byte[32768];

                    const int horizonRow = 80;
                    const int halfRoadMin = 20;
                    const int halfRoadMax = 140;
                    const int edgeWidth = 10;

                    for (int row = horizonRow+1; row < 256; ++row)
                    {
                        int halfRoad = (int)Math.Round(halfRoadMin + (double)(row - horizonRow) / (256 - horizonRow) * (halfRoadMax - halfRoadMin));


                        int leftEdge = 128 - halfRoad;
                        int rightEdge = 129 + halfRoad;

                        for (int col = 0; col < 256; ++col)
                        {
                            byte byteToWrite = 0;

                            if (col <= leftEdge-edgeWidth || col >= rightEdge+edgeWidth)
                                byteToWrite = 4;
                            else if (col <= leftEdge || col >= rightEdge)
                                byteToWrite = 3;
                            else
                                byteToWrite = 2;


                            bytes[row * 128 + col / 2] |= (byte)(byteToWrite << (1 - col % 2) * 4);
                        }
                    }

                    wb.WritePixels(new Int32Rect(0, 0, 256, 256), bytes, 128, 0);
                    wb.Freeze();

                    bmp = wb;
                }

                else if (SelectableTileTypes[i] == TileType.Room) // Csak ideiglenes kinézet, majd ha sok időm lesz csinálok jobbat
                {
                    WriteableBitmap wb = new WriteableBitmap(16, 16, 96, 96, PixelFormats.Indexed2, new BitmapPalette([Colors.DimGray, Colors.Black, Colors.Gold]));

                    byte[] bytes = new byte[64];

                    for (int x = 0; x < 16; ++x)
                    {
                        for (int y = 0; y < 16; ++y)
                        {
                            double dx = Math.Abs(x - 7.5);
                            double dy = Math.Abs(y - 7.5);

                            int maxD = 6;
                            int minD = 2;

                            if (dx > maxD || dy > maxD) continue;

                            if (dx > minD || dy > minD)
                            {
                                bytes[y * 4 + x / 4] |= (byte)(1 << (3 - x % 4) * 2);
                                continue;
                            }

                            bytes[y * 4 + x / 4] |= (byte)(2 << (3 - x % 4) * 2);
                        }
                    }

                    wb.WritePixels(new Int32Rect(0, 0, 16, 16), bytes, 4, 0);
                    wb.Freeze();

                    bmp = wb;
                }

                else if (SelectableTileTypes[i] == TileType.Player)
                {
                    WriteableBitmap wb = new WriteableBitmap(16, 16, 96, 96, PixelFormats.Indexed2, new BitmapPalette([Colors.DimGray, Colors.Black, Colors.LimeGreen]));

                    byte[] bytes = new byte[64];

                    for (int x = 0; x < 16; ++x)
                    {
                        for (int y = 0; y < 16; ++y)
                        {
                            double dx = x - 7.5;
                            double dy = y - 7.5;
                            double dist = Math.Sqrt(dx * dx + dy * dy);

                            if (dist > 6) continue;

                            // Telt kör: zöld belső, fekete körvonal — vizuálisan elkülönül a terem (Room) ikontól.
                            int val = dist > 4.5 ? 1 : 2;

                            bytes[y * 4 + x / 4] |= (byte)(val << (3 - x % 4) * 2);
                        }
                    }

                    wb.WritePixels(new Int32Rect(0, 0, 16, 16), bytes, 4, 0);
                    wb.Freeze();

                    bmp = wb;
                }

                else // Unknown tile / remove
                {
                    WriteableBitmap wb = new WriteableBitmap(16, 16, 96, 96, PixelFormats.Indexed1, new BitmapPalette([Colors.Gray, Colors.Red]));

                    byte[] bytes = new byte[32];

                    for (int x=0; x<16; ++x)
                    {
                        for (int y=0; y<16; ++y)
                        {
                            if (x <= 1 || x >= 14 || y <= 1 || y >= 14 || ((x == 2 || x == 13) && (y == 2 || y == 13))) continue;

                            if (x == 2 || x == 13 || y == 2 || y == 13 || 15-y==x)
                            {
                                bytes[y*2+x/8] |= (byte)(1 << 7-x%8);
                            }
                        }
                    }

                    wb.WritePixels(new Int32Rect(0, 0, 16, 16), bytes, 2, 0);
                    wb.Freeze();

                    bmp = wb;
                }

                drawingContext.DrawImage(bmp, new Rect(i * ActualHeight, 0, ActualHeight, ActualHeight));

                double borderThickness = 4;
                if (SelectableTileTypes[i] == SelectedTileType)
                {
                    drawingContext.DrawRectangle(null, new Pen(Brushes.DarkRed, borderThickness), new Rect(i * ActualHeight + borderThickness/2, borderThickness/2, ActualHeight - borderThickness, ActualHeight - borderThickness));
                }
            }

            drawingContext.Pop();
        }

        protected virtual void OnSelectedTileTypeChanged(TileType? tileType)
        {
            SelectedTileTypeChanged?.Invoke(this, tileType);
            InvalidateVisual();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(this);

            uint clickedTileIndex = (uint)(mousePos.X / ActualHeight);

            if (clickedTileIndex < SelectableTileTypes.Length)
            {
                selectedTileIndex = clickedTileIndex;
                OnSelectedTileTypeChanged(SelectedTileType);
            }

            e.Handled = true;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            uint newTileIndex = (uint)(selectedTileIndex + (e.Delta > 0 ? 1 : -1));

            if (newTileIndex < SelectableTileTypes.Length)
            {
                selectedTileIndex = newTileIndex;
                OnSelectedTileTypeChanged(SelectedTileType);
            }

            e.Handled = true;
        }
    }
}
