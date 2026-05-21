using KampusRota.Api.DTOs;
using KampusRota.Api.Models;

namespace KampusRota.Api.Interfaces;

public interface IUserService
{
    Task<ServiceResult<Kullanici>> RegisterAsync(RegisterRequest request);
    Task<ServiceResult<Kullanici>> LoginAsync(LoginRequest request);
    Task<ServiceResult<Kullanici>> GetByIdAsync(int id);
    Task<ServiceResult<bool>> ChangePasswordAsync(int kullaniciId, ChangePasswordRequest request);
}
