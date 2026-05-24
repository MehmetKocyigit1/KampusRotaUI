namespace KampusRotaUI.Models;

public class YolculukTalebi
{
    public int Id { get; set; }
    public DateTime OlusturulmaTarihi { get; set; }
    public bool AktifMi { get; set; }
    public bool SilindiMi { get; set; }
    public int YolculukId { get; set; }
    public int YolcuId { get; set; }
    public string Durum { get; set; } = "Bekliyor";
    public string TalepMesaji { get; set; } = string.Empty;
    public string SurucuNotu { get; set; } = string.Empty;
    public DateTime TalepTarihi { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public Yolculuk? Yolculuk { get; set; }
    public Kullanici? Yolcu { get; set; }

    public string DurumRengi => Durum switch
    {
        "Onaylandı" => "#0F9F6E",
        "Reddedildi" => "#EF4444",
        _ => "#F59E0B"
    };

    public bool BekliyorMu => string.Equals(Durum, "Bekliyor", StringComparison.OrdinalIgnoreCase);
    public bool OnaylandiMi => string.Equals(Durum, "Onaylandı", StringComparison.OrdinalIgnoreCase);
    public bool SonuclandiMi => !BekliyorMu;

    public string KartVurguRengi => BekliyorMu ? "#1D9DE5" : "#BDDDED";
}
