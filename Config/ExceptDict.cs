namespace vkbot_vitalya.Config;

public class ExceptDict
{
    private Conf? _config;

    public ExceptDict(Conf? config)
    {
        _config = config;
    }

    public List<string> GetExceptions()
    {
        var result = new List<string>();

        result.AddRange(_config.BotNames);
        result.Add("http://");
        result.Add("https://");

        return result;
    }
}
