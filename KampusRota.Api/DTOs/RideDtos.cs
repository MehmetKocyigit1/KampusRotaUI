namespace KampusRota.Api.DTOs;

public record RideCreateRequest(
    string KalkisNoktasi,
    string VarisNoktasi,
    DateTime KalkisZamani,
    int BosKoltukSayisi,
    decimal KisiBasiUcret,
    string? Aciklama,
    bool SadeceKadinlarMi);

public record RideUpdateRequest(
    string KalkisNoktasi,
    string VarisNoktasi,
    DateTime KalkisZamani,
    int BosKoltukSayisi,
    decimal KisiBasiUcret,
    string? Aciklama,
    bool SadeceKadinlarMi,
    bool AktifMi);
