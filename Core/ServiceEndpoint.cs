using vkbot_vitalya.Config;
using vkbot_vitalya.Services;

namespace vkbot_vitalya;

public record ServiceEndpoint()
{
    public MemeGen MemeGen { get; set; } = new MemeGen();
    public WeatherService WeatherService { get; set; } = new WeatherService();
    public DanbooruApi DanbooruApi { get; set; } = new DanbooruApi();
    public Map Map { get; set; } = new Map();
    public Wakaba Wakaba { get; set; } = new Wakaba();
    public SafebooruApi SafebooruApi { get; set; } = new SafebooruApi();
}
