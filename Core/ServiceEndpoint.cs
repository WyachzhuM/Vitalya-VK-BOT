using vkbot_vitalya.Config;
using vkbot_vitalya.Services;

namespace vkbot_vitalya;

public record ServiceEndpoint(Authentication Auth)
{
    public MemeGen MemeGen { get; set; } = new MemeGen(Auth);
    public WeatherService WeatherService { get; set; } = new WeatherService(Auth);
    public DanbooruApi DanbooruApi { get; set; } = new DanbooruApi(Auth);
    public Map Map { get; set; } = new Map(Auth);
    public Wakaba Wakaba { get; set; } = new Wakaba();
    public SafebooruApi SafebooruApi { get; set; } = new SafebooruApi(Auth);
}
