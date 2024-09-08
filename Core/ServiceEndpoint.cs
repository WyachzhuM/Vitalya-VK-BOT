using vkbot_vitalya.Config;
using vkbot_vitalya.Services;

namespace vkbot_vitalya.Core;

public class ServiceEndpoint
{
    public ServiceEndpoint(AuthBotFile auth)
    {
        MemeGen = new MemeGen(auth);
        WeatherService = new WeatherService(auth);
        DanbooruApi = new DanbooruApi(auth);
        Map = new Map(auth);
        Wakaba = new Wakaba();
    }

    public MemeGen MemeGen { get; set; }
    public WeatherService WeatherService { get; set; }
    public DanbooruApi DanbooruApi { get; set; }
    public Map Map { get; set; }
    public Wakaba Wakaba { get; set; }
}
