# Sinov ro'yxati (checklist)

Haqiqiy kompyuterlarda o'tkaziladi: bitta **admin** (kassa) va kamida bitta
**klient** kompyuter. Har bir bandni belgilab boring; xato chiqsa jurnal faylini
saqlab qo'ying (yo'llar oxirida).

Versiya: ____________  Sana: ____________  Tekshirgan: ____________

---

## A. O'rnatish

- [ ] A1. Admin o'rnatuvchisi xatosiz tugadi, dastur ochildi
- [ ] A2. Birinchi ochilishda baza bo'sh: bitta "Standart" zona, bitta "Admin"
      operatori, demo ma'lumot yo'q
- [ ] A3. Windows Firewall'da "DUST2 Admin" qoidasi bor (TCP 7777)
- [ ] A4. Klient o'rnatuvchisi tugagach ulanish oynasi **o'zi ochildi**
- [ ] A5. Xizmat ishlayapti (`services.msc` → "DUST2 klient agent" — Running)
- [ ] A6. Ish stolida va Boshlash menyusida "DUST2 klient" yorlig'i bor

## B. Ulanish (juftlash)

- [ ] B1. Adminda kompyuter qo'shildi (nom, zona, IP, MAC)
- [ ] B2. Klientda IP, port, kod va **aynan o'sha nom** kiritildi → "Ulanish"
- [ ] B3. Adminda kompyuter "Bo'sh" (yashil) bo'ldi, agent versiyasi ko'rinadi
- [ ] B4. Klient ekranining yuqori chap burchagida kompyuter nomi turibdi
- [ ] B5. Noto'g'ri kod bilan ulanish rad etildi va sabab ko'rindi
- [ ] B6. Adminda ro'yxatda yo'q nom bilan ulanish rad etildi

## C. Seans

- [ ] C1. Oldindan to'langan seans ochildi, daqiqa to'g'ri hisoblandi
      (10 000 so'm/soat → 15 000 so'm = 1 soat 30 daqiqa)
- [ ] C2. Klientda qulf ekrani yopildi, taymer ko'rindi
- [ ] C3. Vaqt qo'shildi — klientdagi taymer darhol yangilandi
- [ ] C4. Pauza va davom ettirish ishladi (yuqoridagi panelda ham, kompyuter
      oynasida ham; kartada "pauza" belgisi chiqdi)
- [ ] C4a. Pauza bosilganda klient **qulflandi** — ekranda "Vaqt to'xtatilgan",
      kiritish bloklash yoqilgan bo'lsa klaviatura/sichqoncha ishlamadi;
      davom ettirilganda qulf ochildi va vaqt joyidan davom etdi
- [ ] C4b. Taymer oynachasi o'chirilganda seans davomida ekranda hech narsa
      turmaydi (kompyuter nomi ham chiqmaydi)
- [ ] C4c. **+ Bonus** 30 daqiqa berildi — qolgan vaqt 30 daqiqaga oshdi,
      kassaga pul yozilmadi, jurnalda izoh bilan ko'rindi
- [ ] C4d. **− Jarima** ishladi — qolgan vaqt kamaydi
- [ ] C4e. Vaqti tugab qulflangan kompyuterga bonus berilganda qulf ochildi
- [ ] C4f. Bonus/jarimadan keyin klientdagi taymer darhol yangilandi
- [ ] C4g. Hisobotda "Bonus va jarima" bo'limida yozuvlar va jami ko'rindi;
      Excel eksportida ham alohida varaq bor
- [ ] C5. Ochiq vaqt seansi: yakunlashda hisob to'g'ri chiqdi
- [ ] C6. Yakunlashda qaytariladigan pul to'g'ri hisoblandi (100 so'mgacha
      yuqoriga yaxlitlangan)
- [ ] C7. Qarzga yozish ishladi, "Qarz daftari" da ko'rindi
- [ ] C8. Akkaunt bilan kirish: klientdan login/parol bilan kirildi, balansdan
      yechildi

## D. Vaqt tugashi

- [ ] D1. Ogohlantirish belgilangan daqiqada chiqdi
- [ ] D2. Vaqt tugadi → sozlamaga ko'ra qulflandi / o'chdi / kechikib o'chdi
- [ ] D3. To'lanmagan summa bo'lsa, qulf ekranida ko'rindi

## E. Aloqa uzilganda (eng muhim qism)

- [ ] E1. Seans ochiq holda tarmoq kabeli uzildi → klientda taymer sanashda
      davom etdi
- [ ] E2. Adminda kompyuter **sariq** bo'ldi ("Klient dasturi ishlamayapti")
- [ ] E3. Kabel ulanganda klient o'zi qayta ulandi, holat moslashdi
- [ ] E4. Seans ochiq holda **klient kompyuter o'chirildi va yoqildi** → seans
      tiklandi, qolgan vaqt to'g'ri
- [ ] E5. Admin dasturi yopilib qayta ochildi → klientlar qayta ulandi
- [ ] E6. Klient "Uzish" qilindi va qaytadan juftlandi (bir necha marta)

## F. Kompyuter boshqaruvi

- [ ] F1. Wake-on-LAN: o'chiq kompyuter yondi (BIOS sozlangan bo'lishi kerak)
- [ ] F2. Yonmasa, 90 soniyadan keyin adminda "o'chiq" bo'ldi
- [ ] F3. O'chirish va qayta yuklash ishladi
- [ ] F4. "Seans tugagach o'chirish" ishladi
- [ ] F5. Xabar yuborildi — klient ekranida oyna chiqdi
- [ ] F6. Klientdan "Vaqt so'rash" va "Administratorni chaqirish" → adminda 🔔

## W. Wake-on-LAN (batafsil)

- [ ] W1. Klient jurnalida `Tarmoq kartasi: … / MAC` qatori bor va MAC
      haqiqiy Ethernet kartasiniki (virtual emas)
- [ ] W2. Adminda Sozlamalar → Kompyuterlar bo'limida aynan shu MAC turibdi
- [ ] W3. **Yoqish** bosilgach admin jurnalida "… ta paket yuborildi" qatori
      chiqdi (0 bo'lmasligi kerak)
- [ ] W4. Kompyuter to'liq o'chirilgan holatdan yondi
- [ ] W5. Fast startup yoqilgan klient ulanganda jurnalga ogohlantirish yozildi
- [ ] W6. O'chiq kompyuterda **seans ochilganda** paket o'zi ketdi: jurnalda
      `Avto yoqish: Wake-on-LAN yuborildi (MAC)` va kompyuter yondi
- [ ] W7. MAC kiritilmagan kompyuterda seans ochilganda jurnalda "yuborildi"
      emas, `Avto yoqish: MAC manzil kiritilmagan…` yozuvi chiqdi
- [ ] W8. Seans ochiq kompyuter o'chirilgandan keyin **vaqt qo'shilganda**
      paket ketdi va kompyuter yondi
- [ ] W9. Seans ochiq kompyuter tokdan uzildi → tok qaytarildi (BIOS'da
      `Last State` qo'yilmagan) → admin har daqiqada `Avto yoqish (takror)`
      yozyapti va kompyuter 10 daqiqa ichida o'zi yondi
- [ ] W10. Agent ulangan zahoti `Avto yoqish (takror)` yozuvlari to'xtadi
- [ ] W11. Sozlamada avto yoqish **o'chirilgan** bo'lsa hech qanday avtomatik
      paket yuborilmadi
- [ ] W12. Admindan "o'chirish, seans ochiq qolsin" qilingan kompyuter avto
      yoqilmadi (operator qaroriga qarshi ishlamaydi)
- [ ] W13. Admindan o'chirilgan (seansi ochiq) kompyuterda **Yoqish** bosilganda
      kompyuter haqiqatan yondi va seans davom etdi

### Vaqt tugaganda

- [ ] E1. "Qulflash, keyin o'chirish" + 1 daqiqa: vaqt **o'zi tugagach** ekran
      qulflandi, 1 daqiqadan keyin kompyuter o'chdi
- [ ] E2. Vaqt tugadi, lekin bar to'lanmagan: PC qulflandi, to'lov olinib seans
      yakunlangach 1 daqiqadan keyin baribir o'chdi
- [ ] E3. Vaqti tugamagan seansni operator qo'lda yakunladi — kompyuter
      o'chmadi (bu to'g'ri: sozlama faqat vaqt tugaganda ishlaydi)
- [ ] E4. "Kompyuterni o'chirish" tanlansa vaqt tugagach darhol o'chdi
- [ ] E5. "Ekranni qulflash" tanlansa kompyuter yoniq qoldi
- [ ] E6. **Admin dasturi yopiq** turib vaqt tugadi: klient o'zi qulflandi va
      belgilangan daqiqadan keyin o'zi o'chdi (klient jurnalida
      "Sozlamaga ko'ra N daqiqadan keyin o'chadi")
- [ ] E7. Kechiktirilgan o'chirish kutilayotganda yangi vaqt ochildi —
      kompyuter o'chmadi
- [ ] E8. "Tugashidan oldin ogohlantirish" tanlangan daqiqada klient ekranida
      xabar chiqdi; "O'chiq" tanlansa chiqmadi
- [ ] E9. "Klient ekranida taymer oynachasi" o'chirilganda oynacha yo'qoldi,
      yoqilganda qaytdi (sozlama darhol yetib boradi)
- [ ] E10. "Sichqoncha va klaviatura bloklansin" o'chirilganda qulf ekranida
      sichqoncha ishladi, yoqilganda bloklandi

### Klient xavfsizligi

- [ ] S1. Juftlangan kompyuterning qulf ekranida "⚙ Ulanish sozlamalari"
      tugmasi **ko'rinmaydi**
- [ ] S2. Juftlanmagan kompyuterda ulanish kodi kiritilayotganda tugma
      ko'rinadi va orqaga qaytarib yuboradi
- [ ] S3. Xizmat paroli o'rnatilgan: kombinatsiya bosilganda parol so'raydi
- [ ] S4. Noto'g'ri parol kiritilsa boshqaruv oynasi ochilmaydi
- [ ] S5. Kombinatsiya adminda o'zgartirilgan: eski kombinatsiya ishlamaydi,
      yangisi ishlaydi
- [ ] S6. Admin dasturi **o'chirib tashlangan** (o'rnatuvchi olib tashlangan):
      klient baribir qulflangan qoladi, pastda "Admin bilan aloqa yo'q"
- [ ] S7. Klient kompyuter qayta yuklandi — seans holati tiklandi, qulf
      o'z holicha qoldi

### Hisobot — kassa

- [ ] K1. "Bugun": "Smena boshidagi kassa … Kassada bo'lishi kerak" zanjiri
      to'liq chiqdi va haqiqiy kassaga to'g'ri keldi
- [ ] K2. "Kecha": boshlang'ich kassa qatori **chiqmadi**, o'rniga "Davr
      bo'yicha naqd o'zgarishi" va "Hozir kassada: …" izohi chiqdi
- [ ] K3. Bir kunda ikki smena: "Bugun" hisobotida ham yolg'on qoldiq
      chiqmadi
- [ ] K4. "Kassada naqd" kartasi Smena bo'limidagi kassa bilan bir xil

### Yuqori panel

- [ ] P1. Sozlamalar → Ko'rinish → "Yuqori panel tugmalari": belgini olib
      tashlasangiz tugma paneldan darhol yo'qoladi, qaytarsangiz qaytadi
- [ ] P2. Qatorni sudrab tartibini o'zgartirdingiz — panel ham o'zgardi
- [ ] P3. "Standart tartib" tugmasi hammasini qaytardi
- [ ] P4. Hamma tugmani o'chirib bo'lmaydi ("Kamida bitta tugma qolishi kerak")
- [ ] P5. Dastur qayta ochilganda tanlangan tartib saqlanib qoldi
- [ ] P6. "Ko'chirish": seansi bor bitta kompyuter tanlansa ishlaydi; ikkita
      tanlansa yoki seans bo'lmasa nofaol
- [ ] P7. Ko'chirgandan keyin vaqt, to'lov va bar xizmati yangi kompyuterda
- [ ] P8. "Xabar": bir nechta kompyuter tanlab yuborish ham ishlaydi

### Klient bildirishnomalari

- [ ] N1. Mijoz **o'ynab turganda** admindan xabar yuborildi — ekranning
      tepasida oyna chiqdi va o'yindan chiqarib yubormadi (fokus tortilmadi)
- [ ] N2. Xabar "Tushunarli" bosilmaguncha turdi
- [ ] N3. "Tugashidan oldin ogohlantirish" belgilangan daqiqada o'yin ustida
      ko'rindi va 8 soniyadan keyin o'zi yo'qoldi
- [ ] N4. Vaqt qo'shilganda "+N daqiqa qo'shildi" ko'rindi
- [ ] N5. Bonus va jarima berilganda xabar ko'rindi
- [ ] N6. Qulf ekranida (seanssiz) xabar avvalgidek qulf oynasida chiqdi
- [ ] N7. Seansdan qulfga o'tganda bildirishnoma oynasi yopildi

## G. Jarayonlar

- [ ] G1. Ro'yxat ochildi, protsessor/xotira ko'rsatkichlari mantiqiy
- [ ] G2. Runpad Pro va DUST2 qobig'i **himoyalangan** (yopib bo'lmaydi)
- [ ] G3. Oddiy ilova yopildi
- [ ] G4. Shablon yaratildi va qo'llandi ("Shu PCda yopish")
- [ ] G5. Oyna ochiq turganda o'yin kompyuterida sezilarli sekinlashuv **yo'q**

## H. Qulf ekrani va bloklash

- [ ] H0. Qulf ekranida kompyuter nomi (`PC 01`) soat kabi katta ko'rinadi;
      joylashuv (chap/markaz/split, yuqori/o'rta/past) va soat o'lchami
      o'zgartirilganda nom ham u bilan birga ko'chadi
- [ ] H0b. "Kompyuter nomini ko'rsatish" belgisi olinib yuborilganda nom
      klientda yo'qoldi, qayta belgilanganda qaytdi
- [ ] H1. Mavzu, fon rasmi, sarlavha va joylashuv o'zgartirildi → "Yuborish"
      bosilgach klientda ko'rindi
- [ ] H2. "Markazda" va "Hammasi chapda" joylashuvda kirish kartasi ham ko'chdi
- [ ] H3. Bloklash yoqilganda: Win, Alt+Tab, Alt+F4, Ctrl+Esc ishlamadi
- [ ] H4. Login va parol yozish **ishladi** (bloklash xalaqit bermadi)
- [ ] H5. Ctrl+Alt+K ishladi va boshqaruv oynasi ochildi
- [ ] H6. Qulf paytida Task Manager ochilmadi; qulf ochilgach qaytadan ochildi
- [ ] H7. Qobiq Task Manager'dan majburan yopildi → kiritish **tiklandi** va
      qobiq 3 soniyada qayta ochildi

## I. Klientni to'xtatish

- [ ] I1. Ctrl+Alt+K → "Klient dasturini to'xtatish" → qulf ekrani yopildi
- [ ] I2. Kompyuter qayta yuklandi — klient hamon to'xtatilgan holatda
- [ ] I3. "DUST2 klient" yorlig'i ochildi → hammasi tiklandi, admin bilan ulandi

## J. Bar, smena, hisobot

- [ ] J1. Mahsulot sotildi (kassaga / kompyuterga / qarzga)
- [ ] J2. Ombor qoldig'i kamaydi, chegaradan tushganda ogohlantirish chiqdi
- [ ] J3. Smena yopildi — kutilgan summa va farq to'g'ri
- [ ] J4. Hisobot ochildi va Excel'ga eksport qilindi (9 varaq)

## K. Backup

- [ ] K1. Backup papkasi tanlandi, fayl yozildi
- [ ] K2. Backupdan tiklash ishladi (sinov bazasida!)

## L. O'chirish (uninstall)

- [ ] L1. Klient o'chirildi: xizmat, yorliqlar va Firewall qoidasi ketdi
- [ ] L2. "Sozlamalar ham o'chirilsinmi?" savoli chiqdi
- [ ] L3. "Yo'q" tanlansa — qayta o'rnatgandan keyin kompyuter juftlangan qoldi
- [ ] L4. Task Manager o'chirish paytida bloklangan qolmadi

---

## Jurnallar

Muammo chiqsa, shu fayllarni saqlab qo'ying:

| Nima | Joyi |
|---|---|
| Admin | `%ProgramData%\DUST2\logs\admin-*.log` |
| Klient xizmati | `%ProgramData%\DUST2\logs\service-*.log` |
| Klient qobig'i | `%LOCALAPPDATA%\DUST2\logs\shell-*.log` |
