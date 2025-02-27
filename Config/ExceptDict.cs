namespace vkbot_vitalya.Config;

public class ExceptDict {
    public List<string> GetExceptions() {
        var result = new List<string>();

        result.AddRange(Conf.Instance.BotNames);
        result.Add("http://");
        result.Add("https://");

        return result;
    }
}