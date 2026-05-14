## Programlama 2 Ödev Uyum Kuralları

Bu proje yalnızca UI uygulaması olarak düşünülmemelidir. Programlama 2 proje isterleri gereği çözüm şu parçaları kapsamalıdır:

- .NET MAUI istemci uygulaması
- Minimal API backend projesi
- Entity Framework Core Code First yaklaşımı
- Migration dosyaları
- Veritabanı bağlantısı
- Listeleme, ekleme, güncelleme ve silme işlemleri
- Kullanıcı girişi, çıkışı ve şifre değiştirme
- Service interface ve service sınıfları
- LINQ kullanımı
- Form doğrulamaları
- C# isimlendirme kurallarına uyum

Ajanlar geliştirme yaparken PDF isterlerindeki 26 maddeyi tamamlanması gereken kabul kriterleri olarak değerlendirmelidir.

Öncelik sırası:

1. Mevcut MAUI UI yapısını bozmadan koru.
2. Ayrı bir Minimal API projesi ekle.
3. EF Core modellerini domain ile uyumlu oluştur.
4. Migration ekle ve veritabanını oluştur.
5. CRUD endpoint’lerini yaz.
6. UI `ApiServices` sınıfını bu endpoint’lere bağla.
7. Login/logout/şifre değiştirme akışını uçtan uca doğrula.
8. Proje sonunda tüm isterleri checklist olarak kontrol et.

Backend tarafında şu entity’ler önceliklidir:

- `Kullanici`
- `Yolculuk`

Her veride mümkünse audit alanları tutulmalıdır:

- `OlusturulmaTarihi`
- `OlusturanKullaniciId`
- `GuncellenmeTarihi`
- `GuncelleyenKullaniciId`
- `AktifMi`
- `SilindiMi`

Silme işlemlerinde mümkünse fiziksel silme yerine soft delete tercih edilmelidir.