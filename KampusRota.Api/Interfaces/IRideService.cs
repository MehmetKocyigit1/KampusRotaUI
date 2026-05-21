using KampusRota.Api.DTOs;
using KampusRota.Api.Models;

namespace KampusRota.Api.Interfaces;

public interface IRideService
{
    Task<IReadOnlyList<Yolculuk>> GetAllAsync(string? kalkis, string? varis, DateTime? tarih, bool? sadeceKadinlar);
    Task<IReadOnlyList<Yolculuk>> GetByUserAsync(int kullaniciId);
    Task<ServiceResult<Yolculuk>> GetByIdAsync(int id);
    Task<ServiceResult<Yolculuk>> CreateAsync(int kullaniciId, RideCreateRequest request);
    Task<ServiceResult<Yolculuk>> UpdateAsync(int id, int guncelleyenKullaniciId, RideUpdateRequest request);
    Task<ServiceResult<bool>> DeleteAsync(int id, int silenKullaniciId);
}
