namespace vkbot_vitalya.Config;

public static class ExceptDict {
    public static List<string> Get() {
        List<string> result = ["http://", "https://"];
        result.AddRange(Conf.Instance.BotNames);
        return result;
    }
}