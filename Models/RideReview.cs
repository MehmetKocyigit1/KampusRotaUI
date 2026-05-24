namespace KampusRotaUI.Models;

public class YolculukYorumu
{
    public int Id { get; set; }
    public DateTime OlusturulmaTarihi { get; set; }
    public bool AktifMi { get; set; }
    public bool SilindiMi { get; set; }
    public int YolculukId { get; set; }
    public int YorumYapanKullaniciId { get; set; }
    public int PuanlananKullaniciId { get; set; }
    public int Puan { get; set; }
    public string Yorum { get; set; } = string.Empty;
    public DateTime YorumTarihi { get; set; }
    public Yolculuk? Yolculuk { get; set; }
    public Kullanici? YorumYapanKullanici { get; set; }
    public Kullanici? PuanlananKullanici { get; set; }
}
