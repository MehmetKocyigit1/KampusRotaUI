# Kampus Rota UI

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-Mobile%20App-512BD4)](https://learn.microsoft.com/dotnet/maui/)
[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS%20%7C%20Windows-0078D4)](https://learn.microsoft.com/dotnet/maui/supported-platforms)

Kampus Rota UI, kampus ici yolculuk paylasimi icin gelistirilmis .NET MAUI tabanli mobil/masaustu istemci uygulamasidir. Kullanici girisi, ilan listeleme, yeni yolculuk ilani olusturma, harita uzerinden konum secimi, katilim talepleri ve profil islemleri gibi akislari destekler.

## Ozellikler

- Kullanici kayit ve giris ekrani
- Yolculuk ilanlarini listeleme ve detaylarini goruntuleme
- Yeni yolculuk ilani olusturma
- Harita uzerinden kalkis ve varis noktasi secme
- Surucu icin gelen katilim taleplerini onaylama veya reddetme
- Yolcu icin gonderilen talepleri takip etme
- Yolculuk sonrasi puanlama ve yorum akisi
- Profil goruntuleme, profil guncelleme ve sifre degistirme
- Android emulator icin yerel API adresi destegi

## Teknolojiler

- .NET 8
- .NET MAUI
- CommunityToolkit.Maui
- XAML
- HttpClient tabanli API iletisim katmani

## Proje Yapisi

```text
KampusRotaUI/
├── Models/            # UI tarafinda kullanilan veri modelleri
├── Services/          # API ve harita yardimci servisleri
├── Views/             # Uygulama ekranlari
├── Resources/         # Stil, gorsel, font ve raw kaynaklar
├── Platforms/         # Platforma ozel baslangic dosyalari
├── AppShell.xaml      # Sekmeli navigasyon yapisi
└── MauiProgram.cs     # MAUI uygulama kurulumu
```

## Kurulum

Gereksinimler:

- .NET 8 SDK
- .NET MAUI workload
- Visual Studio 2022 veya MAUI destekli bir IDE
- Calisan Kampus Rota API projesi

Projeyi klonlayin:

```bash
git clone https://github.com/MehmetKocyigit1/KampusRotaUI.git
cd KampusRotaUI
```

MAUI workload kurulu degilse yukleyin:

```bash
dotnet workload install maui
```

Bagimliliklari yukleyin:

```bash
dotnet restore
```

Uygulamayi build edin:

```bash
dotnet build
```

Windows hedefi icin:

```bash
dotnet build -f net8.0-windows10.0.19041.0
```

Android hedefi icin:

```bash
dotnet build -f net8.0-android
```

## API Baglantisi

Uygulama API adresini `Services/ApiServices.cs` icindeki `GetBaseUrl()` metodundan alir.

Varsayilan adresler:

- Android emulator: `https://10.0.2.2:7107/`
- Windows ve diger platformlar: `https://localhost:7107/`

API projesi gelistirme ortaminda `https://localhost:7107` adresinde calismalidir.

## Kalite Kontrol

Build kontrolu:

```bash
dotnet build
```

Format kontrolu:

```bash
dotnet format --verify-no-changes
```

## Ilgili Repo

Backend API: [KampusRotaAPI](https://github.com/MehmetKocyigit1/KampusRotaAPI)

## Notlar

- Bu proje egitim ve portfolyo amacli gelistirilmistir.
- Android emulator HTTPS gelistirme sertifikasi icin `ApiServices` gelistirme ortaminda sertifika dogrulamasini esnek tutar.
- Uretim ortamina cikmadan once API adresi, sertifika dogrulama ve kullanici oturum yonetimi ortam bazli hale getirilmelidir.
