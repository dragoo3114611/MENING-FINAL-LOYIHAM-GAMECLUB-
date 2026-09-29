# DUST2 Boshqaruv — haqiqiy dasturni yaratish (Claude Code uchun topshiriq)

Sen tajribali dasturchisan. Vazifa: o'yin klubi (kompyuter klubi) uchun **Astrum iCafe'ga o'xshash boshqaruv tizimini** yaratish. Tizim ikki qismdan iborat:

1. **Admin dasturi** — kassadagi Windows kompyuterda ishlaydi (Electron).
2. **Klient agent** — har bir o'yin kompyuterida ishlaydi (C# / .NET 8).

Natija GitHub Actions orqali **Windows uchun EXE / o'rnatish fayllari** bo'lib chiqishi kerak.

---

## 0. Eng muhim: namuna prototiplar

Repozitoriyada `docs/prototype/` papkasida ikkita tayyor HTML prototip bor. Ular **dizayn va biznes-mantiqning yagona manbai**:

- `docs/prototype/dust2-boshqaruv-prototip.html` — admin panel (hamma bo'limlar, hisob-kitob formulalari, dialoglar, ruxsatlar).
- `docs/prototype/dust2-klient-prototip.html` — klient ekrani (ulanish oynasi, qulf ekrani, taymer, mavzular, protokol xabarlari jurnali).

Qoidalar:

- Ishni boshlashdan oldin **ikkala faylni to'liq o'qib chiq**. Barcha funksiyalar, matnlar, hisob-kitob va chekka holatlar shu yerda.
- UI ko'rinishi, matnlari (o'zbek lotin), ranglar, mavzular, joylashuv va tugmalar tartibi **prototip bilan bir xil** bo'lsin.
- Prototipda "simulyatsiya" deb yozilgan joylar (Wake-on-LAN, o'chirish, jarayonlar, soat sinxronizatsiyasi, klient so'rovlari) haqiqiy ishlaydigan qilib yoziladi.
- Prototipda ma'lumotlar `localStorage`da. Haqiqiy dasturda **SQLite bazada** saqlanadi.
- Prototipdagi demo (seed) ma'lumotlar haqiqiy dasturga **o'tkazilmaydi**. Dastur bo'sh baza bilan ochiladi, faqat bitta standart zona va bitta Admin operator yaratiladi.
- Biror narsa prototipda noaniq bo'lsa yoki texnik jihatdan imkonsiz bo'lsa — to'xta va menga savol ber, o'zingcha taxmin qilma.

---

## 1. Klub haqida kontekst

- Klub: **DUST2 GAMEZONE**. Hozir 12 ta o'yin kompyuteri (keyin ko'payishi mumkin), zonalar: Standart, VIP, PlayStation.
- Hamma kompyuterlar bitta lokal tarmoqda (LAN), Windows 10/11.
- Klient kompyuterlarda **Runpad Pro qobig'i qoladi**. Agent Runpad'ni almashtirmaydi, faqat uning ustida qulf ekrani va taymerni boshqaradi.
- Deep Freeze yo'q.
- Hozir GameClass ishlatiladi. Yangi tizim to'liq ishlagach, GameClass olib tashlanadi.
- Interfeys tili: **o'zbek (lotin)**. Pul birligi: so'm, mingliklar bo'shliq bilan (`10 000 so'm`).

---

## 2. Arxitektura

### Repozitoriya tuzilishi (monorepo)

```
/admin          Electron admin dasturi
/agent          C# .NET 8 klient agent (service + shell)
/shared         Protokol hujjati va xabar sxemalari (JSON Schema)
/docs           Hujjatlar, prototiplar
/.github/workflows  CI/CD
```

### 2.1 Admin dasturi (`/admin`)

- **Electron** (so'nggi barqaror versiya), **TypeScript**.
- UI: prototipdagi HTML/CSS/JS ni bo'laklarga ajratib ko'chir. Framework majburiy emas — agar React/Vue ishlatsang, ko'rinish prototip bilan piksel darajasida mos bo'lsin. Oddiyroq yo'l: prototip kodini modullarga bo'lib, vanilla JS/TS da qoldirish.
- **Main process**:
  - SQLite baza (`better-sqlite3`, `electron-rebuild` bilan).
  - WebSocket server (`ws`), port standart **7777** (Sozlamalarda o'zgartiriladi).
  - Wake-on-LAN magic packet yuborish (UDP 9, broadcast).
  - Avto backup fayl yozish, Excel eksport (`exceljs`).
- **Renderer ↔ main**: `contextBridge` + `ipcRenderer.invoke`. Renderer to'g'ridan-to'g'ri Node API ga kira olmaydi (`contextIsolation: true`, `nodeIntegration: false`).
- Admin dasturi yopilsa ham klientlar ishlashda davom etadi (quyida 2.2).
- Birinchi ishga tushishda Windows Firewall'da 7777-portga kirish qoidasini qo'shishni taklif qilsin (`netsh advfirewall`), administrator huquqi so'rab.

### 2.2 Klient agent (`/agent`)

Windows'da xizmat (service) foydalanuvchi ekraniga oyna chiqara olmaydi (Session 0 izolyatsiyasi), shuning uchun agent **ikki jarayondan** iborat:

1. **`Dust2Agent.Service`** — Windows Service, `LocalSystem` huquqi bilan, kompyuter yonishi bilan ishga tushadi.
   - Admin bilan WebSocket aloqasi, qayta ulanish (exponential backoff), heartbeat.
   - Seans holati va taymer (**monotonik soat**: `Stopwatch`/`Environment.TickCount64`, Windows soatiga bog'liq emas).
   - Seans holatini diskka saqlash (admin o'chsa yoki kompyuter qayta yuklansa ham davom etadi).
   - Windows soatini o'rnatish (`SetSystemTime`).
   - O'chirish / qayta yuklash (`shutdown` yoki `ExitWindowsEx`).
   - Jarayonlar ro'yxati va yopish.
   - Shell jarayonini foydalanuvchi sessiyasida ishga tushirish va nazorat qilish (`WTSQueryUserToken` + `CreateProcessAsUser`), yopilsa qayta ishga tushirish.
2. **`Dust2Agent.Shell`** — foydalanuvchi sessiyasidagi WPF ilova.
   - **Qulf ekrani**: to'liq ekran, eng ustida (topmost), barcha monitorlarda, mavzu va joylashuv sozlamalari bilan (prototipdagidek).
   - **Taymer oynachasi** (widget): Runpad ustida, surib bo'ladigan, kichraytiriladigan.
   - Ulanish sozlamalari oynasi (admin IP, port, ulanish kodi, kompyuter nomi), ⚙ tugmasi administrator paroli bilan himoyalangan.
   - Akkaunt bilan kirish formasi, ogohlantirishlar, admin xabarlari, "Vaqt so'rash" tugmasi.
   - Service bilan aloqa: **named pipe** (`\\.\pipe\dust2agent`), faqat lokal.
- Klaviatura va sichqonchani bloklash: qulf ekranida low-level hook (`WH_KEYBOARD_LL`) bilan Win, Alt+Tab, Alt+F4, Ctrl+Esc ni to'sish. **Diqqat:** Ctrl+Alt+Del ni hook bilan to'sib bo'lmaydi — buning o'rniga qulf paytida Task Manager'ni registry siyosati orqali o'chirish va qulf ochilganda qaytarish. Nimani to'sib bo'lmasligini hujjatda ochiq yoz.
- .NET 8, `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` (klient kompyuterlarda .NET o'rnatish shart bo'lmasin).
- O'rnatish: **Inno Setup** installer — service'ni ro'yxatdan o'tkazadi, Firewall qoidasi, avtomatik ishga tushish, o'chirish (uninstall) toza bo'lsin.
- Favqulodda chiqish: prototipdagi tizimda bo'lgani kabi maxfiy tugmalar kombinatsiyasi + administrator paroli bilan qulfni ochish (sozlanadigan).

### 2.3 Tarmoq protokoli (`/shared/protocol.md`)

- WebSocket, JSON xabarlar: `{ "type": "session.start", "id": "<uuid>", "ts": <server_ms>, "payload": {...} }`.
- Har bir buyruqqa javob (`ack` yoki `error`) bo'lsin, yetib bormagan buyruqlar navbatda qolsin.
- Birinchi ulanish: klientda admin IP + port + **6 xonali ulanish kodi** qo'lda kiritiladi → `pair.request` → admin tekshiradi → `pair.ok {token}`. Token klientda **DPAPI** bilan shifrlab saqlanadi, keyingi ulanishlarda kod so'ralmaydi.
- Prototipdagi xabarlar (klient prototipining "Admin bilan aloqa" jurnaliga qara) — kamida shular:
  - `hello`, `hello.ok`, `pair.request`, `pair.ok`, `pair.denied`, `heartbeat`, `sync {state}`
  - `lock.config {theme, layout, valign, clockSize, titleSize, textSize, title, text, clock, login, wallpaper}`
  - `theme.set`
  - `session.start`, `session.add_time`, `session.pause`, `session.resume`, `session.end`, `session.lock`
  - `session.started`, `session.updated`, `session.ended`
  - `auth.login`, `auth.ok`, `auth.denied`
  - `message`, `message.shown`
  - `client.request_time`, `client.call_admin`, `client.warning_shown`
  - `process.list`, `process.kill`, `power.off`, `power.reboot`
  - `time.request`, `time.sync`, `time.synced {drift_ms}`
- Seans uchun admin **mutlaq tugash vaqtini** (server soati bo'yicha) va qolgan millisekundlarni yuboradi. Agent qolgan vaqtni monotonik soat bilan sanaydi.

---

## 3. Biznes-qoidalar (prototipdagidek, aniq bajarilsin)

Barcha formulalarni prototip kodidan tekshir. Asosiylari:

**Seanslar**
- Rejimlar: **oldindan to'langan** (paket yoki pul bo'yicha — pul soatlik tarifga bo'linib vaqtga aylanadi), **ochiq vaqt** (chiqishda to'lanadi), **akkaunt** (balansdan daqiqama-daqiqa yechiladi).
- Soatlik tarif har safar tanlanadi (zona narxi yoki qo'lda narx).
- Summalar **100 so'mgacha yuqoriga yuvarlanadi**.
- Ochiq vaqtga vaqt/pul qo'shilsa, taymer paydo bo'lmaydi — summa **"Ustiga qo'shilgan"** sifatida hisobga qo'shiladi va chiqishda olinadi.
- Oldindan to'langan seans vaqtidan oldin yakunlansa: ishlatilgan = to'langan × (o'ynalgan / sotib olingan vaqt), ishlatilmagan qism **mijozga qaytariladi** (minus summa, belgilash bilan).
- Kartada va jadvalda jonli: ochildi, qo'shildi, tugaydi, hisob, **o'ynagan vaqt** va **o'ynagan pul**.
- **Vaqt kompyuter o'chiq bo'lsa ham sanaladi.** Kompyuter yonganda admin bilan sinxronlanadi.
- O'chirishda band kompyuter uchun 3 variant: hozir o'chirish (seans ochiq qoladi), yakunlab o'chirish, vaqt tugagach o'chirish.
- Vaqt tugaganda: qulflash / o'chirish / qulflab N daqiqadan keyin o'chirish (sozlama). Tugashidan oldin ogohlantirish (sozlama).
- To'lanmagan summa (bar yoki ochiq vaqt qarzi) bo'lsa, vaqt tugaganda kompyuter qulflanadi, seans yopilmaydi.
- Bar xizmati **faqat vaqt ochilgan kompyuterga**, kompyuter hisobiga yoziladi va qaytarilishi mumkin (ombor qoldig'i qaytadi).

**Boshqaruv tugmalari** (Boshlash, Tugatish, Yoqish ▾, O'chirish ▾, Qayta yuklash ▾, Jarayonlar, Bar xizmati) — faqat Zal bo'limida, har doim tasdiqlash bilan (Bar xizmati bundan mustasno). Strelka menyusi: tanlangan / hammasi / bo'sh turganlari.

**Kassa, smena, operatorlar**
- Smena ochishda kassadagi naqd kiritiladi; topshirish (qabul qiluvchi paroli, agar bor bo'lsa), yopish (sanab chiqilgan pul, farq).
- Operatorlar va rollar (admin/operator), 10 ta ruxsat (prototipdagi `PERMS`). Ruxsat yo'q amalda administrator paroli so'raladi.
- **Parollar dastlab yo'q** — dastur parolsiz ochiladi. Parol Sozlamalarda o'rnatilgach kirish ekrani chiqadi. Parollar **argon2 yoki bcrypt hash** bilan saqlansin (prototipda ochiq matn edi — buni takrorlama).
- Har bir jurnal yozuvida operator nomi.

**Bar va ombor**: tan narxi (kirimda o'rtacha og'irlikli), "tugayapti" chegarasi, kirim / sanash tarixi, sotuvda tan narxi saqlanadi (bar foydasi uchun), mahsulotni o'chirish.

**Qarz daftari**: qarz yozish, to'lash (oddiy va chegirma bilan — chegirma berilgan summaga foiz sifatida qo'shiladi), tahrirlash/o'chirish (yozuv va qarzdor), Excel eksport.

**Hisobot**: sana + soat oralig'i filtri, tez tanlash (bugun/kecha/shu hafta/shu oy), kompyuterlar, bar (tan narxi va foyda), kassa harakati, qarzlar, chiqimlar, soatlar bo'yicha tushum grafigi, **9 varaqli Excel eksport**.

**Foyda hisob-kitobi**: mustaqil kirim/chiqim hisob-kitobi, hisobotga ulanmaydi, lekin backupga kiradi.

**Sozlamalar bo'limlari**: Ko'rinish (10 mavzu, shrift, summa shrifti, zal ko'rinishi: kartalar / ixcham plitkalar + hover oynacha / jadval), Kompyuterlar (server manzili, ulanish kodi, vaqt sinxronizatsiyasi, kompyuterlar ro'yxati: nom, zona, IP, MAC), Kompyuter xatti-harakati, Qulf ekrani (fon rasmi, 5 klient mavzusi, joylashuv va o'lchamlar, kompyuterlarga yuborish), Operatorlar, Backup, Tozalash (bo'limlab, "TOZALASH" so'zi bilan tasdiqlash, oldin backup).

**Klient so'rovlari**: klientdan "vaqt so'rash / administratorni chaqirish" kelganda adminda pastki o'ng burchakda karta va 🔔 ro'yxat.

**Vaqt sinxronizatsiyasi**: klient ulanganda, aloqa tiklanganda va har 10 daqiqada admin vaqtini so'raydi, farq ≥ 1 soniya bo'lsa Windows soatini o'rnatadi va `time.synced {drift_ms}` yuboradi. Admin jadvalida "Soat" ustuni.

**Backup**: foydalanuvchi bir marta papka tanlaydi, o'sha papkada **bitta** backup fayl har o'zgarishda va dastur ochilganda yangilanadi (SQLite `backup` API yoki JSON eksport). Backupdan tiklash.

---

## 4. Baza (SQLite) — taxminiy jadvallar

`settings, zones, packages, pcs, clients, sessions, ledger (pul harakatlari: time/bar/top/settle/debt/debtpay/exp/refund), items, stock_log, debtors, debt_entries, shifts, operators, requests, journal, expense_categories, profit_rows, app_templates`

- Pul butun son (so'm), vaqtlar UTC millisekund.
- Har bir o'zgartiruvchi amal tranzaksiyada.
- Migratsiyalar versiyalangan (`schema_version`).

---

## 5. GitHub Actions — EXE yig'ish

`.github/workflows/build.yml`:

- Trigger: `push` (main), `pull_request`, va `v*` teglar.
- Runner: `windows-latest`.
- **Admin job**: Node LTS → `npm ci` → testlar → `electron-builder --win nsis portable` → artefakt: `DUST2-Admin-Setup-x.y.z.exe` va portable exe.
- **Agent job**: `actions/setup-dotnet` (8.x) → `dotnet test` → `dotnet publish` (win-x64, self-contained, single-file) → Inno Setup (`choco install innosetup` yoki `Minionguyjpro/Inno-Setup-Action`) → artefakt: `DUST2-Klient-Setup-x.y.z.exe`.
- `v*` teg push qilinganda ikkala installer **GitHub Release**ga avtomatik yuklansin (`softprops/action-gh-release`).
- Versiya raqami tegdan olinadi va ikkala dasturda ko'rinadi.
- Kod imzolash (code signing) hozircha yo'q — README'da Windows SmartScreen ogohlantirishi haqida yoz.

---

## 6. Bosqichlar

Har bir bosqich oxirida: ishlaydigan holat, commit, qisqa hisobot (nima qilindi, nima qoldi, qanday sinash), keyin menga savol bilan to'xta.

1. **Asos**: monorepo, Electron + TS skeleti, SQLite va migratsiyalar, CI workflow (bo'sh dastur ham EXE bo'lib chiqsin).
2. **Admin UI ko'chirish**: prototipni ko'chirish, localStorage o'rniga IPC + SQLite. Hamma bo'limlar prototipdagidek ishlasin (hali klientsiz).
3. **Protokol va server**: WebSocket server, juftlash (pairing), heartbeat, protokol hujjati va JSON sxemalar, test klient (Node skript) bilan sinov.
4. **Klient agent MVP**: Service + Shell, ulanish oynasi, qulf ekrani, taymer, seans start/add/end, offline davom etish, soat sinxronizatsiyasi.
5. **Kompyuter boshqaruvi**: Wake-on-LAN, o'chirish/qayta yuklash, jarayonlar va shablonlar, xabarlar, klient so'rovlari, akkaunt bilan kirish, qulf ekrani mavzulari.
6. **Pishiqlik**: kiritishni bloklash, Shell yiqilsa qayta ishga tushishi, installerlar, uninstall, loglar (`%ProgramData%\DUST2\logs`), backup/tiklash.
7. **Sinov va hujjat**: 1–2 ta haqiqiy kompyuterda sinov ro'yxati (checklist), README (o'rnatish, Firewall, BIOS'da Wake-on-LAN yoqish, Runpad bilan birga ishlash), foydalanuvchi qo'llanmasi (o'zbekcha).

---

## 7. Sifat talablari

- Testlar: hisob-kitob formulalari (billing, refund, ustiga qo'shilgan, qarz chegirmasi, smena kassasi, ombor o'rtacha narxi) uchun unit testlar — prototipdagi natijalar bilan bir xil chiqsin.
- Pul hisobida floating-point xatosi bo'lmasin (butun sonlar).
- Admin va agent loglari; xatolar foydalanuvchiga o'zbekcha tushunarli xabar bilan.
- Xavfsizlik: parollar hash, token DPAPI, faqat LAN, WebSocket xabarlari token bilan, noma'lum klient buyruqlari rad etiladi.
- Klient agent resurs sarfi past bo'lsin (o'yin kompyuterlari).

Birinchi javobingda: prototiplarni o'qib chiqqaningni tasdiqla, arxitekturaga e'tirozlaring yoki savollaringni yoz, keyin 1-bosqichni boshla.
