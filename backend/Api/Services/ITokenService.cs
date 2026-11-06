using Api.Entities;
using Api.Services.Models;

namespace Api.Services;

public interface ITokenService
{
    AccessToken CreateToken(AdminUser user);
}
