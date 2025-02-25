using VkNet.Model;

namespace vkbot_vitalya.Core;

public static class Vk {
    public static string PingUser(User user, string? name = null) {
        return $"[id{user.Id}|{name ?? user.FirstName + " " + user.LastName}]";
    }
}