namespace KampusRotaUI.Models;

public class Kullanici
{
     public int Id { get; set; }
    public DateTime OlusturulmaTarihi { get; set; }
    public bool AktifMi { get; set; }
    public bool SilindiMi { get; set; }

     public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SifreHash { get; set; } = string.Empty;
    public string TelefonNumarasi { get; set; } = string.Empty;

     public string OgrenciNumarasi { get; set; } = string.Empty;
    public string ProfilFotografiUrl { get; set; } = string.Empty;
    public string Cinsiyet { get; set; } = string.Empty;
    public double OrtalamaPuan { get; set; } = 5.0;
    public string Biyografi { get; set; } = string.Empty;

    // UI (Arayüz) İçin Hesaplanmış Özellik - Ekranda Göstermek İçin
    public string TamAd => $"{Ad} {Soyad}";
}