using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace LabyrinthEditor
{
    public class TileSelector : FrameworkElement
    {
        public TileSelector()
        {
            MouseDown += OnMouseDown;
            //MouseMove += OnMouseMove;
            //MouseUp += OnMouseUp;
            //MouseWheel += OnMouseWheel;

            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight)); // Background

            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualWidth)));

            //drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 10, 10));

            TileType[] tileTypes = Enum.GetValues<TileType>();
            for (int i=0; i<tileTypes.Length; ++i)
            {
                BitmapSource bmp;

                if (tileTypes[i] == TileType.Wall)
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

                else if (tileTypes[i] == TileType.Path)
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


                            bytes[row * 128 + col / 2] |= (byte)(byteToWrite << 4 - col % 2 * 4);
                        }
                    }

                    wb.WritePixels(new Int32Rect(0, 0, 256, 256), bytes, 128, 0);
                    wb.Freeze();

                    bmp = wb;
                }

                //else if (tileTypes[i] == TileType.Room)
                //{
                //    WriteableBitmap wb = new WriteableBitmap(16, 16, 96, 96, PixelFormats.Indexed1, new BitmapPalette([Colors.White, Colors.Black]));

                //    byte[] bytes = new byte[32];

                //    wb.WritePixels(new Int32Rect(0, 0, 16, 16), bytes, 2, 0);
                //    wb.Freeze();

                //    bmp = wb;
                //}

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
            }

            drawingContext.Pop();
        }


        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
