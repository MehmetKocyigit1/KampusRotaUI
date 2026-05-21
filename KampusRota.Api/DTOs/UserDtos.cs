namespace KampusRota.Api.DTOs;

public record LoginRequest(string Email, string Sifre);

public record RegisterRequest(
    string Ad,
    string Soyad,
    string Email,
    string Sifre,
    string? TelefonNumarasi,
    string? OgrenciNumarasi,
    string? Cinsiyet);

public record ChangePasswordRequest(
    string EskiSifre,
    string YeniSifre,
    string YeniSifreTekrar);
