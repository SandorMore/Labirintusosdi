using System.IO;
using System.Windows;

namespace LabyrinthEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // A nyelvi fájl a kimeneti mappában, az exe mellett található.
            string langPath = Path.Combine(AppContext.BaseDirectory, "languages.json");

            try
            {
                Localization.Load(langPath);
            }
            catch (Exception ex)
            {
                // Nyelvi fájl nélkül is induljon el az app (a kulcsok jelennek meg szövegként).
                MessageBox.Show($"Could not load language file ({langPath}): {ex.Message}",
                    "Localization", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
