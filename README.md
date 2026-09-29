# DUST2 Klub — Klub Pult + DUST2 Klient

**DUST2 GAMEZONE** o'yin klubi uchun boshqaruv tizimining yakuniy versiyasi. Ikki dastur, bitta repo:

| Papka | Dastur | Qayerda ishlaydi |
|-------|--------|------------------|
| [`klub-pult/`](klub-pult/) | **Klub Pult Server** — Electron admin paneli + LAN server (SQLite) | Kassadagi kompyuter |
| [`dust2-klient/`](dust2-klient/) | **DUST2 Klient** — .NET 8 agent (Windows xizmati + WPF qobiq) | Har bir o'yin kompyuteri |
| [`shared/`](shared/) | Ikkalasi gaplashadigan protokol (`protocol.md`, JSON sxemalar) | — |
| [`docs/`](docs/) | Qo'llanma, sinov ro'yxati, qarorlar, HTML prototiplar | — |

Ikkala dastur `ws://<admin-ip>:7777/agent` orqali `shared/protocol.md` bo'yicha ulanadi.

## Qayerdan olindi

Bu repo eski ikki repodan tarixsiz, toza holda yig'ilgan:

- `klub-pult/` ← `pro-artifact`, commit `c2c8b79` (Actions build #36464600725, «KlubPult-Server-windows»)
- `dust2-klient/`, `shared/`, `docs/` ← `O-ZIMNI-PROTOTIPIM-GAME-CLUB`, reliz **v0.1.8**

## Yig'ish

GitHub Actions har push'da ikkala EXE'ni yig'adi (Actions → oxirgi run → Artifacts):

- `KlubPult-Server-windows` — `KlubPult-Server-*-setup.exe` va `*-portable.exe`
- `DUST2-Klient-windows` — `DUST2-Klient-Setup-*.exe`

Reliz chiqarish: `v1.0.0` kabi teg push qiling — ikkala dastur bitta GitHub Release'ga yuklanadi, versiya tegdan olinadi.

Qo'lda yig'ish:

```bash
# Klub Pult Server
cd klub-pult/admin && npm ci && npm run dist          # → klub-pult/admin/dist/

# DUST2 Klient (Windows, .NET 8 SDK + Inno Setup)
cd dust2-klient
dotnet test Dust2Agent.sln
dotnet publish src/Dust2Agent.Service/Dust2Agent.Service.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/service
dotnet publish src/Dust2Agent.Shell/Dust2Agent.Shell.csproj   -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/shell
iscc /DMyAppVersion=0.1.8 installer/dust2-klient.iss
```

## Birinchi ulanish

1. Kassada Klub Pult Serverni o'rnating va oching (7777-port; Sozlamalar › Kompyuterlar › Server bo'limida IP va 6 xonali kod).
2. Sozlamalar › Kompyuterlar'da kompyuter qo'shing — nomi `PC <raqam>` bo'lsin (masalan «PC 5»).
3. O'yin kompyuteriga DUST2 Klient'ni o'rnating, admin IP, port va kodni kiriting.

Batafsil: [`klub-pult/README.md`](klub-pult/README.md), [`docs/QOLLANMA.md`](docs/QOLLANMA.md), [`shared/protocol.md`](shared/protocol.md).

Fayllar kod imzosi bilan imzolanmagan — SmartScreen chiqsa: **More info / Дополнительно → Run anyway**.
