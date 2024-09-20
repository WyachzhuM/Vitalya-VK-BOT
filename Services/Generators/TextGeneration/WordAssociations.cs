using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vkbot_vitalya.Services.Generators.TextGeneration;

public class WordAssociations
{
    private static Dictionary<string, Dictionary<string, int>> _associations;

    public static string assocFilePath = "./associations.json";

    public WordAssociations()
    {
        _associations = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        LoadFromFile(assocFilePath);
    }

    // Метод для добавления связи между двумя словами
    public static void AddAssociation(string word1, string word2)
    {
        if (!_associations.ContainsKey(word1))
        {
            _associations[word1] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        if (_associations[word1].ContainsKey(word2))
        {
            _associations[word1][word2]++;
        }
        else
        {
            _associations[word1][word2] = 1;
        }
    }

    // Получение следующего слова на основе текущего
    public static string GetNextWord(string currentWord)
    {
        if (currentWord == null || !_associations.ContainsKey(currentWord))
        {
            return string.Empty;
        }

        var possibleWords = _associations[currentWord];
        return possibleWords.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? string.Empty;
    }


    public static void SaveToFile(string path)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(_associations);
        File.WriteAllText(path, json);
    }

    public void LoadFromFile(string path)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            _associations = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json) ?? new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            SaveToFile(path);
        }
    }
}
