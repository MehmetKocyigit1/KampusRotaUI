namespace KampusRota.Api.Models;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime OlusturulmaTarihi { get; set; } = DateTime.UtcNow;
    public int? OlusturanKullaniciId { get; set; }
    public DateTime? GuncellenmeTarihi { get; set; }
    public int? GuncelleyenKullaniciId { get; set; }
    public bool AktifMi { get; set; } = true;
    public bool SilindiMi { get; set; }
}
