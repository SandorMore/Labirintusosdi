using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace WpfApp1.Models
{
    internal class Labyrinth
    {
        int width;
        int height;
        int rooms;
        char[,] map;

        public Labyrinth(int _width, int _height, char[,] _map)
        {
            this.width = _width;
            this.height = _height;
            this.rooms = 0;
            this.map = _map;
        }

        public void print_labyrinth()
        {
            for (int row = 0; row < height; ++row)
            {
                for (int col = 0; col < width; ++col)
                {
                    Trace.Write(map[row, col]);
                }
                Trace.Write('\n');
            }
        }
        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }
        public int Rooms { get => rooms; set => rooms = value; }
        public char[,] Map { get => map; set => map = value; }
    }
}
