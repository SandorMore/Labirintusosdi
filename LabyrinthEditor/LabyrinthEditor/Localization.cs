using System.IO;
using System.Text.Json;

namespace LabyrinthEditor
{
    /// <summary>
    /// Egyszerű, fájl alapú lokalizáció. A nyelveket és a szövegeket egy külső
    /// JSON fájl tartalmazza (languages.json), így új nyelv felvételéhez nem kell
    /// a kódot módosítani. A JSON szerkezete: nyelvkód -> (kulcs -> szöveg).
    /// </summary>
    public static class Localization
    {
        private static Dictionary<string, Dictionary<string, string>> languages = new();
        private static string currentLanguage = "hu";

        // Akkor sül el, amikor a felhasználó nyelvet vált; a UI ekkor frissíti a szövegeit.
        public static event Action? LanguageChanged;

        // Az elérhető nyelvek kódjai (pl. "en", "hu") a JSON-ban szereplő sorrendben.
        public static IReadOnlyList<string> AvailableLanguages => languages.Keys.ToList();

        public static string CurrentLanguage
        {
            get => currentLanguage;
            set
            {
                if (value == currentLanguage || !languages.ContainsKey(value)) return;
                currentLanguage = value;
                LanguageChanged?.Invoke();
            }
        }

        /// <summary>
        /// Betölti a nyelvi fájlt. Ha a betöltés nem sikerül, a Get() a kulcsot adja vissza,
        /// így az alkalmazás nyelvi fájl nélkül is elindul (csak a kulcsok látszanak).
        /// </summary>
        public static void Load(string path)
        {
            string json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

            if (data != null && data.Count > 0)
            {
                languages = data;

                if (!languages.ContainsKey(currentLanguage))
                {
                    currentLanguage = languages.Keys.First();
                }
            }
        }

        // A kulcshoz tartozó szöveg az aktuális nyelven. Ha nincs meg, maga a kulcs.
        public static string Get(string key)
        {
            return languages.TryGetValue(currentLanguage, out var dict) && dict.TryGetValue(key, out var value)
                ? value
                : key;
        }

        // Formázott változat: a szöveg {0}, {1}, ... helyőrzőit tölti ki.
        public static string Get(string key, params object?[] args)
        {
            return string.Format(Get(key), args);
        }

        // Egy nyelvkód emberi olvasásra szánt neve (languageName), pl. "en" -> "English".
        public static string DisplayName(string code)
        {
            return languages.TryGetValue(code, out var dict) && dict.TryGetValue("languageName", out var name)
                ? name
                : code;
        }
    }
}
