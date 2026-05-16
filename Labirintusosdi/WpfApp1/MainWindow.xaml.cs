using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp1.Models;
using WpfApp1.Utils;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        LANG lang = LANG.HUN;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var res = read_map();
            if (res != null)
            {
                Tuple<char[,], int, int> t = res;
                Labyrinth labyrinth = new Labyrinth(t.Item2, t.Item3, t.Item1);
            }
        }
        Tuple<char[,], int, int>? read_map()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.RestoreDirectory = true;
            ofd.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";

            if (ofd.ShowDialog() != true)
            {
                MessageBox.Show((lang == LANG.HUN) ? "Nem lehetett beolvasni a filet!" : "Couldn't open the file!");
                return null;
            }

            string mapPath = ofd.FileName;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(mapPath, Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show((lang == LANG.HUN) ? $"Hiba a fájl beolvasása közben: {ex.Message}" : $"Error reading file: {ex.Message}");
                return null;
            }

            if (lines.Length == 0)
            {
                MessageBox.Show((lang == LANG.HUN) ? "A fájl üres." : "The file is empty.");
                return null;
            }

            int height = lines.Length;
            int width = 0;
            foreach (var l in lines)
                if (l.Length > width) width = l.Length;

            var map = new char[height, width];

            for (int y = 0; y < height; y++)
            {
                var line = lines[y];
                for (int x = 0; x < width; x++)
                {
                    map[y, x] = (x < line.Length) ? line[x] : ' ';
                }
            }

            return Tuple.Create(map, width, height);
        }
    }
}