namespace KampusRota.Api.Models;

public class Yolculuk : BaseEntity
{
    public int SurucuId { get; set; }
    public Kullanici? Surucu { get; set; }
    public string KalkisNoktasi { get; set; } = string.Empty;
    public string VarisNoktasi { get; set; } = string.Empty;
    public DateTime KalkisZamani { get; set; }
    public int BosKoltukSayisi { get; set; }
    public decimal KisiBasiUcret { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public bool SadeceKadinlarMi { get; set; }

    public string Rota => $"{KalkisNoktasi} -> {VarisNoktasi}";
    public string TarihFormatli => KalkisZamani.ToString("dd/MM/yyyy HH:mm");
    public string UcretMetni => KisiBasiUcret == 0 ? "Ucretsiz" : $"{KisiBasiUcret} TL";
    public string KadinaOzelMetni => SadeceKadinlarMi ? "Sadece Kadinlar" : "Karma";
}
