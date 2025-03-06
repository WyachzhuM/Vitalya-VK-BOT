using Newtonsoft.Json;

namespace vkbot_vitalya.Services.Generators.TextGeneration;

public class WordAssociations {
    private static Dictionary<string, Dictionary<string, int>> _associations;

    public const string AssocFilePath = "associations.json";

    public WordAssociations() {
        _associations = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        Load(AssocFilePath);
    }

    // Метод для добавления связи между двумя словами
    public static void AddAssociation(string word1, string word2) {
        if (!_associations.ContainsKey(word1)) {
            _associations[word1] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        if (_associations[word1].ContainsKey(word2)) {
            _associations[word1][word2]++;
        } else {
            _associations[word1][word2] = 1;
        }
    }

    // Получение следующего слова на основе текущего
    public static string GetNextWord(string currentWord) {
        if (_associations.TryGetValue(currentWord, out var possibleWords))
            return possibleWords.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? string.Empty;
        return string.Empty;
    }


    public static void Save(string path) {
        var json = JsonConvert.SerializeObject(_associations);
        File.WriteAllText(path, json);
    }

    private static void Load(string path) {
        if (File.Exists(path)) {
            var json = File.ReadAllText(path);
            _associations =
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json) ??
                new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        } else {
            Save(path);
        }
    }
}