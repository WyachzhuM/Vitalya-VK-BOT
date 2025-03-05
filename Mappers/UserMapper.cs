using AutoMapper;
using VkNet.Model;

namespace vkbot_vitalya.Mappers;

public class UserMapper : Profile {
    private static readonly MapperConfiguration Config = new MapperConfiguration(c => c.AddProfile<UserMapper>());
    private static readonly IMapper Mapper = Config.CreateMapper();

    public UserMapper() {
        CreateMap<User, Config.User>();
    }

    public static T Map<T>(object source) {
        return Mapper.Map<T>(source);
    }
}