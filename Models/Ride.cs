namespace KampusRotaUI.Models;

public class Yolculuk
{
     public int Id { get; set; }
    public DateTime OlusturulmaTarihi { get; set; }
    public bool AktifMi { get; set; }
    public bool SilindiMi { get; set; }

     public int SurucuId { get; set; }
    public string KalkisNoktasi { get; set; } = string.Empty;
    public string VarisNoktasi { get; set; } = string.Empty;
    public DateTime KalkisZamani { get; set; }
    public int BosKoltukSayisi { get; set; }

     public decimal KisiBasiUcret { get; set; } = 0;
    public string Aciklama { get; set; } = string.Empty;
    public string IletisimTelefonu { get; set; } = string.Empty;
    public bool SadeceKadinlarMi { get; set; } = false;

     public Kullanici? Surucu { get; set; }
     public int? UniversityId { get; set; }
     public University? University { get; set; }
     public double? KalkisLatitude { get; set; }
     public double? KalkisLongitude { get; set; }
     public double? VarisLatitude { get; set; }
     public double? VarisLongitude { get; set; }

     public string Rota => $"{KalkisNoktasi} ➔ {VarisNoktasi}";
    public string TarihFormatli => KalkisZamani.ToString("dd/MM/yyyy HH:mm");
    public string UcretMetni => KisiBasiUcret == 0 ? "Ücretsiz" : $"{KisiBasiUcret} ₺";
    public string KadinaOzelMetni => SadeceKadinlarMi ? "👩 Sadece Kadınlar" : "Karma";
}
