# Klub Pult — game club boshqaruv paneli (prototip)

Kompyuter klubi (game club) uchun admin/operator paneli prototipi. Bitta `index.html` fayl, brauzerda ochiladi, maʼlumotlar brauzerning `localStorage`ida saqlanadi.

## Boʻlimlar
- **Zal** — 50 ta kompyuter, GameClass uslubidagi jadval va kartalar koʻrinishi. Holatlar: Boʻsh, Band, Pauza, Oʻchiq.
  Amallar: Start, Qoʻshimcha vaqt, Stop, Koʻchirish, Pauza, Qayta yuklash, Oʻchirish, Yoqish — har birining yonida oʻz ▾ menyusi bor: *Qayta yuklash ▾* (tanlanganlar / boʻsh PClar / hammasi), *Oʻchirish ▾* (tanlanganlar / boʻsh PClar / hammasi; seansi ochiq PC oʻchirilsa seans pauzaga olinib vaqti saqlanadi), *Yoqish ▾* (tanlanganlar / hammasi), Xabar yuborish, Bar xizmati, Jarayonlar (bir yoki bir nechta kompyuterda qaysi ilova ochiq, tugatish, tozalash; «Saqlansin» belgilangan ilova tozalashda yopilmaydi), Mijoz ekrani. Klaviatura: strelkalar bilan yurish, Shift+strelka yoki Shift+bosish bilan oraliqni belgilash (cheklovsiz), Ctrl+A — hammasi; bitta yoki bir nechta PC belgilab *Enter* — seansni boshlash oynasi, *Delete* — seansni yakunlash oynasi.
  Toʻlov turlari: oldindan toʻlov, keyin toʻlov, mijoz balansidan. Pastda xabarlar jurnali jadval koʻrinishida (sana, vaqt, turi, PC, xabar): yuqoriga sursangiz eski yozuvlar yuklanadi, balandligini ham oʻzgartirish mumkin.
- **Mijozlar** — akkaunt (login/parol), balans, bonus darajalari, tashriflar tarixi.
- **Bar** — ikki yorliq. *Sotuv*: mahsulot kartalari, savat, «Kimga yoziladi» (kassa yoki PC seansi), Chiqim kiritish, Mahsulot sotish. *Ombor*: tan narxi, sotuv narxidagi qiymat, foyda/dona, qoldiq va chegara, Tovar kirimi (yetkazuvchi, kassadan toʻlash), Sanash, kirim va sanash tarixi.
- **Qarz daftari** — har bir qarzdor bitta hisobda (qarzlar va toʻlovlar), qidiruv (ism yoki telefon), toʻlanganlarni koʻrsatish, Excelga eksport, qarzdor tarixi (har bir yozuvni tahrirlash va oʻchirish), toʻlovda chegirma.
- **Smena** — ochish va yopish, kassa sanash, toʻlov usullari (naqd, karta, Click, Payme). Smena yopilganda yangisi avtomatik ochiladi (xodim va qoldiriladigan naqd tanlanadi, qolgani «topshirildi»).
- **Hisobot** — davr (Bugun, Kecha, Shu hafta, Shu oy yoki Dan–Gacha sana va vaqt); kartalar: Jami ishlandi, Kompyuterlar, Bar, Qarz boʻldi, Qarz toʻlandi, Kassadan chiqim, Kassada naqd; «Kassa harakati (naqd)» jadvali; grafiklar va toplar.
- **Foyda hisob-kitobi** (faqat admin) — xarajatlar va sof foyda.
- **Tariflar** — soatbay va paket tariflari, kun va vaqt boʻyicha. CYBER tarifi: 10 000 soʻm/soat.
- **Sozlamalar** — chapda boʻlimlar, oʻngda tanlangan boʻlim:
  - *Kompyuterlar*: jadval (qoʻshish/oʻchirish, guruh, IP qoʻlda, MAC avto yoki qoʻlda, Wake-on-LAN), *Guruhlar*, *Server ulanishi* (admin IP, port, ulanish kodi, klient xizmat paroli, vaqt sinxronizatsiyasi), *Xatti-harakat* (vaqt ochilganda avto yoqish, taymer oynachasi, vaqt tugaganda qulflash/oʻchirish, ogohlantirish, sichqoncha va klaviaturani bloklash).
  - *Qulf ekrani*: fon rasmi, 5 ta mavzu, joylashuv, qorongʻilashtirish, yozuvlar va ularning oʻlchami, oldindan koʻrish, kompyuterlarga (hammasi yoki guruh) yuborish.
  - *Foydalanuvchilar va huquqlar* (har bir operatorning oʻz ruxsatlari, parol, bloklash; ruxsat yoʻq amalda administrator paroli soʻraladi), *Server interfeysi* (shrift turi va oʻlchami, tema, jadval ustunlari va karta maʼlumotlarini tanlash va tartibini oʻzgartirish), *Backup* (papkaga avto backup, qoʻlda yuklab olish va tiklash, arxiv nusxalar — tozalash va tiklashdan oldin avtomatik olinadi, ulardan orqaga qaytish mumkin; holati Zalda soat yonida koʻrinadi), *Maʼlumotlarni tozalash* (kassa operatsiyalari, jurnal, qarz daftari, smenalar, ombor tarixi, klient soʻrovlari, foyda yozuvlari, ochiq seanslar, mijoz akkauntlari — boʻlimlab).
- Qarz toʻlovida chegirma toʻlov oynasining oʻzida qoʻllanadi: berilgan summaga foiz qoʻshib qarzdan ayriladi.
- **Mijoz kompyuteri** (Zal › «Mijoz ekrani») — Klub Pult ichidagi klient PC prototipi, Klub Pult maʼlumotlari bilan jonli ishlaydi: oʻchiq va «Qayta yuklanmoqda» ekranlari, qulf ekrani (Sozlamalar › Qulf ekrani: mavzu, fon, yozuvlar), pauza, «Vaqtingiz tugadi» (toʻlanmagan summa bilan), akkaunt bilan kirish (seans balansdan boshlanadi), ish stoli (Oʻyinlar / Dasturlar / Internet, vazifalar paneli — «Jarayonlar» dagi ilovalar, qolgan vaqt va soat), suriladigan va kichraytiriladigan taymer oynachasi («Vaqt soʻrash», akkauntdan «Chiqish»), administrator xabarlari va avtomatik ogohlantirishlar.
- **`klient.html`** — alohida DUST2 Klient prototipi (oʻz simulyatsiya paneli bilan).

Namuna maʼlumotlarda parol yoʻq — dastur parolsiz ochiladi. Parol oʻrnatilsa, ochilganda kirish oynasi chiqadi.

Ishga tushirish: `index.html` faylini brauzerda oching.

## Haqiqiy SQLite baza
Klub Pult Server (Windows ilovasi) maʼlumotni admin kompyuterda haqiqiy **SQLite** faylida saqlaydi
(`%AppData%/Klub Pult/klubpult.db`, WAL rejimi) — brauzer `localStorage`iga bogʻliq emas, tozalansa
yoʻqolmaydi. Har oʻzgarish darhol (debounce bilan) yoziladi; dastur qayta ochilganda shu fayldan oʻqiydi.
Native modul (`better-sqlite3`) yuklanmasa, dastur `localStorage`ga xavfsiz qaytadi va ishlashda davom etadi.
Sozlamalar › Backup boʻlimida baza holati (fayl yoʻli) koʻrsatiladi.

## DUST2 klient agentiga moslik (../shared/protocol.md)
Klub Pult Server endi haqiqiy **DUST2 Klient** agenti (`../dust2-klient/`) bilan
`shared/protocol.md` protokoli boʻyicha gaplashadi. Server `ws://<ip>:<port>/agent` manzilida tinglaydi,
xabarlar konverti `{v,type,id,ts,payload}`.
- **Juftlash:** agent `pair.request {name, code, ip, mac, version, fastStartup}` yuboradi. Nom `PC <raqam>`
  ga (masalan «PC 5») mos boʻlishi va kod Sozlamalar › Kompyuterlar › Server boʻlimidagi kodga teng boʻlishi
  kerak. Toʻgʻri boʻlsa `pair.ok {token}` va toʻliq `sync`, aks holda `pair.denied {reason}` (bad_code / unknown_pc).
- **Qayta ulanish:** `hello {token}` → `hello.ok` (token SHA-256 hash boʻlib saqlanadi).
- **Server → agent:** `sync`, `session.start/pause/resume/lock`, `lock.config`, `behaviour`, `message`,
  `process.list`, `process.kill`, `power.off`, `power.reboot`, `time.sync`, `auth.ok/denied`, `lock.wallpaper`.
- **Agent → server:** `heartbeat`, `auth.login`, `session.started/updated/ended`, `client.request_time`,
  `client.call_admin`, `client.warning_shown`, `process.list.result`, `time.request/synced`, `message.shown`,
  `lock.wallpaper.request`, `client.unpair`. Har biriga `ack`/`error` javob.
- **Xizmat paroli** agentga `pbkdf2$<takror>$<salt b64>$<hash b64>` (SHA-256) formatida yuboriladi.
- **Wake-on-LAN** haqiqiy UDP paket (255.255.255.255 va subnet broadcast, 9 va 7-port).

### DUST2 Klientni Klub Pult Serverga ulash
1. Admin kompyuterda Klub Pult Serverni oching (server 7777-portda ishga tushadi; Sozlamalar › Kompyuterlar ›
   Server boʻlimida IP, port va 6 xonali ulanish kodi koʻrinadi).
2. Oʻsha boʻlimda oʻyin kompyuterini **aynan «PC 5» kabi nom bilan** qoʻshing (agent yuboradigan nomga mos).
3. Oʻyin kompyuterida DUST2 Klientni oʻrnating va ochib, admin IP, port va ulanish kodini kiriting.
4. Juftlangach kompyuter Zal jadvalida «Boʻsh» boʻlib chiqadi; keyingi ulanishlarda kod soʻralmaydi.

## Klub Pult Server — admin kompyuter uchun Windows ilovasi
`admin/` papkasida Electron loyihasi: `index.html` oddiy Windows dasturi sifatida ochiladi va ichida **server** ishlaydi — klient kompyuterlar LAN orqali WebSocket bilan ulanadi (standart port 7777, Sozlamalar › Kompyuterlar › Server).
- Juftlash: klient PC raqami va ulanish kodini yuboradi → server kalit (token) beradi, keyingi safar kodsiz ulanadi. IP va MAC avtomatik yoziladi.
- Server har bir ulangan PC ga holatini yuboradi: qulf ekrani sozlamalari, seans (qolgan vaqt, tarif, balans), pauza, xabarlar. Klientdan akkaunt bilan kirish, «Vaqt soʻrash», akkauntdan chiqish, xabar oʻqilgani keladi.
- Wake-on-LAN haqiqiy UDP paket bilan yuboriladi. Birinchi ishga tushishda namuna maʼlumotlarsiz, toza baza bilan ochiladi.
- Yigʻish: `cd admin && npm install && npm run dist` → `admin/dist/` (oʻrnatuvchi `setup.exe` — Windows Firewall qoidasini ham qoʻshadi, va `portable.exe`).
- GitHub Actions: «Windows exe» workflow exe fayllarni yigʻib, *Artifacts* ga qoʻyadi.
