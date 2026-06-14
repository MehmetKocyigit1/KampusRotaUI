# Katki Rehberi

Bu repo portfolyo ve egitim amacli gelistirildigi icin UI degisikliklerinde okunabilirlik, tutarlilik ve basit test edilebilirlik onceliklidir.

## Gelistirme Akisi

1. Yeni bir branch olusturun.
2. Ekran, servis veya model degisikligini kucuk kapsamda tutun.
3. `dotnet build` komutunu calistirin.
4. Degisiklik UI davranisini etkiliyorsa kisa manuel test notu ekleyin.
5. Pull request acarken ekran/akis etkisini aciklayin.

## Kod Standartlari

- XAML stillerini mumkun oldugunca mevcut kaynaklarla uyumlu tutun.
- API iletisimlerini `Services` katmaninda toplayin.
- Yerel gelistirme adresleri ve hassas bilgiler commitlenmemelidir.
- Platforma ozel davranislari ilgili `Platforms` klasoru veya servis yardimcilariyla sinirlandirin.
