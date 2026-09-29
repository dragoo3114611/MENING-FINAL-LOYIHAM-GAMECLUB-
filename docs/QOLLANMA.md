# DUST2 GAMEZONE — foydalanuvchi qo'llanmasi

Bu qo'llanma ikki qismdan iborat:

- **1-qism — operator uchun**: kunlik ish (seans ochish, vaqt qo'shish, bar, smena).
- **2-qism — administrator uchun**: o'rnatish, sozlash, kompyuterlarni ulash,
  muammolarni hal qilish.

Dasturda pul har doim **so'm** va butun son bo'lib ko'rsatiladi, mingliklar
bo'shliq bilan ajratiladi: `10 000 so'm`. Hisob-kitobda summa har doim **100
so'mgacha yuqoriga** yaxlitlanadi.

---

# 1-qism. Operator uchun

## 1.1. Kun boshlanishi — smenani ochish

Dastur ochilganda **Smena ochish** oynasi chiqadi. Kassadagi naqd pulni sanab,
summani kiriting va **Smenani ochish** ni bosing. Shu paytdan boshlab barcha
pul harakatlari shu smenaga yoziladi.

> Smena ochilmasa seans ochib bo'lmaydi — dastur ogohlantiradi.

## 1.2. Zal ekrani

Har bir kompyuter karta ko'rinishida. Rangi holatni bildiradi:

| Rang | Ma'nosi |
|---|---|
| Yashil | Bo'sh — seans ochish mumkin |
| Ko'k | Ishlayapti (seans ochiq) |
| Qizil | Vaqti tugayapti (10 daqiqadan kam) yoki tugadi |
| Kulrang | O'chiq yoki nosoz |
| **Sariq** | **Klient dasturi ishlamayapti** — kompyuterga qarash kerak |

- **Bir marta bosish** — kompyuterni tanlash, pastda uning tarixi chiqadi.
- **Ikki marta bosish** — vaqt qo'shish oynasi.
- **Ctrl + bosish** yoki "Bir nechtasini tanlash" — bir necha kompyuterni birga
  boshqarish.

Zal ko'rinishini Sozlamalar → Ko'rinish bo'limidan o'zgartirish mumkin
(zonalar, ixcham plitkalar yoki jadval).

## 1.3. Seans ochish

Kompyuterni tanlab, yuqoridagi **Boshlash** tugmasini bosing. Uch xil usul bor:

1. **Oldindan to'langan** — mijoz pul beradi, dastur necha daqiqa berilishini
   o'zi hisoblaydi. Masalan 10 000 so'm/soat tarifda 15 000 so'm = 1 soat 30 daqiqa.
2. **Ochiq vaqt** — vaqt sanaladi, hisob oxirida to'lanadi.
3. **Paket** — oldindan tayyorlangan narx (masalan "3 soat — 25 000 so'm").

Mijoz akkaunti bo'lsa, u kompyuterning o'zidan login va parol bilan kiradi —
vaqt balansidan yechiladi.

## 1.4. Vaqt qo'shish

Kompyuterni ikki marta bosing yoki **Vaqt qo'shish** ni tanlang. Summani yoki
daqiqani kiriting — ikkinchisi o'zi hisoblanadi.

## 1.4b. Pauza, bonus va jarima

Pauza paytida klient ekranida qulf ochiladi va **"Vaqt to'xtatilgan — administrator
davom ettirguncha kuting"** deb yoziladi. Kiritishni bloklash yoqilgan bo'lsa,
klaviatura va sichqoncha ham to'siladi — mijoz pauza davomida o'ynay olmaydi.
Qolgan vaqt saqlanib turadi.

**Pauza** yuqoridagi asosiy panelda ham bor — "Boshlash" va "Tugatish"
o'rtasida. Bir nechta kompyuter tanlangan bo'lsa, hammasi birdan to'xtaydi
(qayta bosilganda davom etadi). Vaqt to'xtagan kompyuterda tugma
"Davom ettirish" ga aylanadi va yashil rangga o'tadi.

Kompyuter oynasining yuqorisida, taymer ostida uchta tugma turadi:

| Tugma | Nima qiladi |
|---|---|
| **⏸ Pauza** | Vaqt sanashni to'xtatadi (masalan, mijoz tashqariga chiqdi) **va kompyuterni qulflaydi**. Qayta bosilganda **▶ Davom ettirish** — qulf ochiladi, vaqt joyidan davom etadi |
| **+ Bonus** | Mijozga **bepul** vaqt qo'shadi. Kassaga pul yozilmaydi |
| **− Jarima** | Seans vaqtidan olib tashlaydi |

Bonus va jarima oynasida tayyor tugmachalar (5 / 10 / 15 / 30 / 60 daqiqa),
qo'lda daqiqa kiritish va **izoh** maydoni bor. Izoh jurnalga tushadi:
*"Bonus vaqt: +30 daqiqa — tanlov sovrini"*.

- Oldindan to'langan seansda bonus qolgan vaqt ustiga qo'shiladi.
- Ochiq vaqt seansida chiqishda to'lanadigan summa kamayadi (jarima — ko'payadi).
- Akkaunt seansida balansdan yechiladigan vaqt kamayadi.
- Vaqti tugab qulflangan kompyuterga bonus berilsa, u **yana ochiladi**.

Klient taymeri darhol yangi vaqtga o'tadi.

Bonus va jarimani **har qanday operator** bera oladi — alohida ruxsat talab
qilinmaydi. Lekin iz qoladi:

- **Jurnalda** — kim, qachon, qancha va qanday izoh bilan berdi;
- **Hisobotda** — "Bonus va jarima" bo'limi: har bir yozuv va davr bo'yicha
  jami ("+2 soat 30 daqiqa · −40 daqiqa"). Excel eksportida ham alohida varaq
  bor. Bu yerda summa emas, **vaqt** ko'rsatiladi — kassaga ta'sir qilmaydi.

## 1.5. Seansni yakunlash

**Tugatish** tugmasi. Oynada ko'rinadi:

- o'ynalgan vaqt va uning puli;
- bar xizmatlari;
- to'lanishi kerak bo'lgan yoki qaytariladigan summa.

Oldindan to'langan vaqt to'liq ishlatilmasa, qolgan pul **qaytariladi**
(qaytarish summasi ham 100 so'mgacha yaxlitlanadi).

To'lay olmasa — **Qarzga yozish** ni tanlang, mijoz ismini kiriting. Qarz
"Qarz daftari" bo'limida ko'rinadi.

## 1.6. Bar

**Bar** bo'limida mahsulotni tanlab savatga qo'shasiz va:

- **Kassaga** — darhol to'laydi;
- **Kompyuterga** — seans hisobiga qo'shiladi, yakunlashda to'laydi;
- **Qarzga** — qarz daftariga yoziladi.

Ombor qoldig'i o'zi kamayadi. Qoldiq chegaradan tushsa, "Bar" yonida sariq
belgi chiqadi.

## 1.7. Klient so'rovlari (🔔)

Mijoz o'z kompyuteridan **"Vaqt so'rash"** yoki **"Administratorni chaqirish"**
tugmasini bossa, yuqoridagi qo'ng'iroq belgisida raqam chiqadi. Bosib ko'ring,
bajarilganini belgilang.

## 1.8. Kompyuterlarni boshqarish

Yuqoridagi panel orqali tanlangan kompyuterga:

- **Yoqish** — Wake-on-LAN signali (kompyuter o'chiq bo'lsa);
- **O'chirish** / **Qayta yuklash** — darhol yoki seans tugagach;
- **Jarayonlar** — ochiq ilovalarni ko'rish va yopish;
- **Xabar** — mijoz ekranida xabar oynasi chiqadi.

## 1.9. Smenani yopish

**Smena** bo'limi → **Smenani yopish**. Kassadagi pulni sanab kiriting; dastur
kutilgan summa bilan solishtirib farqni ko'rsatadi. Smenani boshqa operatorga
topshirish ham shu yerda.

---

# 2-qism. Administrator uchun

## 2.1. Nimalar kerak

- **Admin kompyuter** (kassa): Windows 10/11, DUST2 Admin dasturi.
- **Klient kompyuterlar**: Windows 10/11, DUST2 Klient agenti.
- Hammasi **bitta lokal tarmoqda** bo'lishi shart (bir xil Wi-Fi/kommutator).

## 2.2. Admin dasturini o'rnatish

1. `DUST2-Admin-Setup-x.y.z.exe` ni ishga tushiring.
2. Windows SmartScreen ogohlantirsa: **More info → Run anyway**
   (fayl kod imzosi bilan imzolanmagan).
3. O'rnatuvchi Windows Firewall'da TCP **7777** uchun qoidani o'zi qo'shadi.

Birinchi ochilishda baza bo'sh bo'ladi: bitta **Standart** zona va bitta
parolsiz **Admin** operatori.

## 2.3. Boshlang'ich sozlash

1. **Tariflar** — zonalar va soatlik narx (masalan Standart 10 000, VIP 15 000).
   Paketlar ham shu yerda.
2. **Sozlamalar → Operatorlar** — operatorlarga parol qo'ying va ruxsatlarni
   belgilang (hisobot, kassa summasi, narxlar, chegirma…).
3. **Sozlamalar → Kompyuterlar** — har bir kompyuterni **qo'lda qo'shing**:
   nom (masalan `PC 01`), zona, IP va MAC manzil.

> MAC manzil Wake-on-LAN uchun kerak. Uni klient kompyuterda
> `cmd` → `getmac /v` bilan bilib olish mumkin.

## 2.4. Klient agentni o'rnatish

Har bir o'yin kompyuterida:

1. `DUST2-Klient-Setup-x.y.z.exe` ni ishga tushiring (administrator huquqi bilan).
2. O'rnatuvchi xizmatni ro'yxatdan o'tkazadi, ishga tushiradi va ulanish oynasini
   ochadi.
3. Oynada kiriting:
   - **Admin kompyuter IP manzili** va **port** — adminda Sozlamalar →
     Kompyuterlar bo'limida ko'rsatilgan;
   - **Ulanish kodi** — o'sha bo'limdagi 6 xonali kod;
   - **Kompyuter nomi** — adminda qo'shilgan nom bilan **aynan bir xil**
     (masalan `PC 01`).
4. **Ulanish** — adminda kompyuter "yondi" deb belgilanadi.

Kompyuter nomi klient ekranining yuqori chap burchagida doimiy ko'rinib turadi.

## 2.5. Wake-on-LAN (BIOS sozlamasi)

Kompyuterni masofadan yoqish uchun har bir klient kompyuterda:

1. **BIOS/UEFI** → `Power Management` bo'limi → **Wake on LAN** (yoki
   "Power On by PCI-E", "Resume by LAN") — **Enabled**.
2. Windows: `Device Manager` → tarmoq kartasi → `Properties` →
   - **Power Management** yorlig'ida: "Allow this device to wake the computer" ✔
   - **Advanced** yorlig'ida: "Wake on Magic Packet" — Enabled.
3. Windows'da "Fast startup" (tez ishga tushish) o'chirilgan bo'lsa ishonchliroq:
   `Control Panel → Power Options → Choose what the power buttons do →
   Turn on fast startup` — belgini olib tashlang.

Adminda **Yoqish** bosilganda kompyuter 90 soniya ichida ulanmasa, u o'zi
"o'chiq" deb belgilanadi va jurnalga yoziladi.

### Avtomatik yoqish

Sozlamalar → Kompyuter xatti-harakati → **"Kompyuter o'chiq bo'lsa, avtomatik
yoqilsin"** yoqilgan bo'lsa, paket o'zi yuboriladi:

- o'chiq kompyuterda **seans ochilganda**;
- seans ochiq kompyuterga **vaqt qo'shilganda**, agar agent o'sha payt oflayn
  bo'lsa (ya'ni kompyuter o'chib qolgan);
- **agent seans ochiq turib yo'qolganda** — masalan tok uzilganda.

Oxirgi ikki holatda bitta paket bilan cheklanmaydi: kompyuter **har 60
soniyada, 10 daqiqa davomida** yoqishga urinib ko'riladi. Sabab — tok qaytgach
tarmoq kartasi darhol emas, bir necha o'n soniyadan keyin sehrli paketni qabul
qila boshlaydi.

Urinish to'xtaydi: agent ulanganda, seans yakunlanganda, operator kompyuterni
o'zi o'chirganda yoki 10 daqiqa o'tganda. Jurnalda `Avto yoqish: …` va
`Avto yoqish (takror): …` qatorlari ko'rinadi.

> Operator kompyuterni admin orqali ataylab o'chirgan bo'lsa (seans ochiq
> qoldirib), avto yoqish unga qarshi ishlamaydi.

### Tok uzilib qaytganda

Tok uzilganda kompyuterda hech narsa ishlamaydi va tarmoq kartasi ham o'ladi —
o'sha paytda hech qanday paket yordam bermaydi. Tok qaytganda kompyuter o'zi
yonishi uchun **BIOS sozlamasi** kerak:

`Restore AC Power Loss` (ASUS) / `AC BACK` (Gigabyte) / `After Power Loss`
(Dell, HP) → **`Last State`**

`Last State` o'yin klubi uchun to'g'ri tanlov: o'ynalayotgan kompyuterlar
qaytadi, bo'sh turganlari o'chiq qoladi. `Power On` qo'ysangiz, kechasi tok
uzilib-ulansa **hamma** kompyuter yonib ketadi.

Shu bilan birga `ErP Ready` / `EuP` / `Deep Sleep Control` → **Disabled**
bo'lishi shart: u yoqilgan bo'lsa o'chiq kompyuterga standby toki bormaydi va
Wake-on-LAN umuman ishlamaydi.

### MAC manzil to'g'rimi?

Wake-on-LAN aynan MAC manzil bo'yicha ishlaydi. Klient ulanganda agent MAC'ni
o'zi yuboradi va u **adminga boradigan yo'ldagi haqiqiy karta**dan olinadi —
virtual kartalar (VMware, VirtualBox, Hyper-V, WSL, VPN) chetlab o'tiladi.
Tekshirish: klient jurnalida `Tarmoq kartasi: Ethernet — 192.168.1.105 / 1C:…`
qatori bo'ladi; shu MAC adminda Sozlamalar → Kompyuterlar bo'limida turishi kerak.

Qo'lda kiritsangiz: klient kompyuterda `ipconfig /all` → **Ethernet adapter**
bo'limidagi "Physical Address".

### Paket yuborildimi?

**Yoqish** bosilgandan keyin admin jurnalida (`admin-*.log`) shunday qator
paydo bo'ladi:

```
PC 01: Wake-on-LAN — MAC 1C:1B:0D:xx:xx:xx, IP 192.168.1.105
Wake-on-LAN 1C:1B:0D:xx:xx:xx: 6 ta paket yuborildi (1 ta tarmoq, portlar 9/7)
```

Paket admin kompyuterning **har bir tarmoq kartasidan**, ham umumiy
(255.255.255.255), ham o'sha tarmoqning broadcast manziliga, 9 va 7-portlarga
yuboriladi. "0 ta paket" bo'lsa — Windows Firewall yoki antivirus to'sayapti.

Paket ketgan bo'lsa-yu kompyuter yonmasa, muammo klient tomonda: BIOS, tarmoq
kartasi sozlamasi yoki fast startup (yuqoridagi 1–3 qadamlar). Kompyuterlar
turli kommutatorlarda bo'lsa, ular **bitta tarmoqda (subnet)** ekaniga ishonch
hosil qiling — WoL routerdan o'tmaydi.

> Tez ishga tushirish yoqilgan klient ulanganda, admin jurnaliga ogohlantirish
> yoziladi: *"Tez ishga tushirish yoqilgan — o'chirilgandan keyin masofadan
> yoqish ishlamasligi mumkin"*.

## 2.6. Runpad Pro bilan birga ishlash

Klub kompyuterlarida qobiq sifatida Runpad Pro o'rnatilgan bo'lsa, DUST2 klienti
unga xalaqit bermaydi:

- DUST2 qobig'i Runpad ustida, alohida oyna sifatida ishlaydi;
- **Runpad Pro jarayoni himoyalangan** — "Jarayonlar" oynasida uni yopib
  bo'lmaydi (DUST2 qobig'i ham shunday);
- qulf ekrani ochilganda Runpad orqasida qoladi, seans ochilganda esa qulf
  yopiladi va Runpad odatdagidek ishlaydi.

## 2.7. Qulf ekrani va kiritishni bloklash

**Sozlamalar → Qulf ekrani**: mavzu, fon rasmi, sarlavha, pastki yozuv, matnlar
joylashuvi va o'lchami. O'zgartirgandan keyin **"Kompyuterlarga yuborish"** ni
bosing.

**Sozlamalar → Kompyuter xatti-harakati**:

- vaqt tugaganda nima bo'lishi (qulflash / o'chirish / kechikish bilan o'chirish);
- ogohlantirish vaqti;
- **Sichqoncha va klaviatura bloklansin** — qulf ekranida Win, Alt+Tab, Alt+F4,
  Ctrl+Esc kabi kombinatsiyalar to'siladi va Task Manager o'chiriladi;
- **Klient ekranida taymer oynachasi ko'rinsin** — qolgan vaqt, tarif va
  "Vaqt so'rash" tugmasi. O'chirilsa, seans davomida ekranda hech narsa
  turmaydi — o'yinga xalaqit qilmaydi.

**Kompyuter nomi** (masalan `PC 01`) qulf ekranida soat bilan bitta ustunda,
katta harflar bilan ko'rsatiladi. Joylashuv yoki soat o'lchami o'zgarsa, nom
ham u bilan birga ko'chadi va kattalashadi. Seans ochiq paytda nom ko'rsatilmaydi.

Kerak bo'lmasa: **Sozlamalar → Qulf ekrani → "Kompyuter nomini ko'rsatish"**
belgisini oling va "Kompyuterlarga yuborish" bosing.

> Bloklash faqat **qulf ekrani ochiq** bo'lganda ishlaydi. Seans davomida
> kompyuter odatdagidek ishlaydi.

## 2.8. Xizmat paroli va favqulodda kirish

**Sozlamalar → Kompyuterlar → Klient xizmat paroli** — bu parol klient
kompyuterda ulanish sozlamalarini ochish va qulfni qo'lda ochish uchun kerak.

Klient kompyuterda **Ctrl+Alt+P** (kombinatsiyani o'zgartirish mumkin) bosilsa,
boshqaruv oynasi ochiladi:

- **Ulanish sozlamalari** — IP, port, kompyuter nomi;
- **Klient dasturini to'xtatish** — qulf ekrani yopiladi, kompyuter odatdagidek
  ishlaydi. Qayta yoqish uchun ish stolidagi **"DUST2 klient"** yorlig'ini oching.

Parol o'rnatilgan bo'lsa, avval u so'raladi. Parol admin o'chiq bo'lsa ham
ishlaydi (klientda hashlangan holda saqlanadi).

## 2.9. Backup

**Sozlamalar → Backup** — papkani tanlang (masalan flesh yoki tarmoq papkasi).
Har bir o'zgarish shu papkadagi `DUST2-backup.json` fayliga yoziladi.

Tiklash: **Backupdan tiklash** → faylni tanlang. **Diqqat:** hozirgi
ma'lumotlar butunlay almashtiriladi.

## 2.10. Muammolarni hal qilish

| Belgi | Nima qilish kerak |
|---|---|
| Zalda kompyuter **sariq** | Klient dasturi javob bermayapti. Kompyuterga qarang: ishlab turganmi, tarmoq bormi. Kerak bo'lsa "DUST2 klient" yorlig'ini oching |
| Klientda "Xizmat bilan aloqa yo'q" | `services.msc` → **DUST2 klient agent** xizmati ishlayaptimi, ishga tushiring |
| Juftlash "Bu nom bilan kompyuter yo'q" deydi | Adminda Sozlamalar → Kompyuterlar bo'limida **aynan shu nom** bilan kompyuter qo'shilganini tekshiring |
| Klient ulanmayapti | Admin IP to'g'rimi, ikkalasi bir tarmoqdami, TCP 7777 ochiqmi (Sozlamalar → Kompyuterlar bo'limida Firewall holati ko'rinadi) |
| Wake-on-LAN ishlamayapti | 2.5-bo'limga qarang: admin jurnalida "paket yuborildi" qatori bormi, MAC to'g'rimi, BIOS va fast startup sozlanganmi |
| Kompyuter soati noto'g'ri | Agent uni admin vaqtiga o'zi moslaydi. Seans taymeri Windows soatiga bog'liq emas — soat xato bo'lsa ham vaqt to'g'ri sanaladi |

### Jurnallar

| Nima | Joyi |
|---|---|
| Admin | `%ProgramData%\DUST2\logs\admin-*.log` |
| Klient xizmati | `%ProgramData%\DUST2\logs\service-*.log` |
| Klient qobig'i | `%LOCALAPPDATA%\DUST2\logs\shell-*.log` |

Jurnallar 14 kundan keyin o'zi o'chadi.
