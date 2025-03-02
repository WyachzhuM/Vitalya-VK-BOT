using AutoMapper;
using VkNet.Model;

namespace vkbot_vitalya.Mappers;

public class UserMapper : Profile {
    private static readonly MapperConfiguration MapperConfig = new MapperConfiguration(config => config.AddProfile<UserMapper>());
    private static readonly IMapper Mapper = MapperConfig.CreateMapper();

    public UserMapper() {
        CreateMap<User, Config.User>();
    }

    public static T Map<T>(object source) {
        return Mapper.Map<T>(source);
    }
}