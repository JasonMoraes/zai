using Api.Entities;

namespace Api.Services;

public interface ITokenService
{
    AccessToken CreateToken(AdminUser user);
}
