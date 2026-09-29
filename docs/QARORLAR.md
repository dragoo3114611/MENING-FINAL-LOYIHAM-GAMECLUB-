# Qabul qilingan qarorlar

Loyiha davomida kelishib olingan, kodga ta'sir qiladigan qarorlar. Yangi qaror
qo'shilsa — shu faylga yoziladi.

## 1. Vaqt kimning hisobida (2026-09-17)

**Admin — yagona manba.** Admin seansning mutlaq tugash vaqtini saqlaydi va klientga
shuni yuboradi. Agent qolgan vaqtni monotonik soat bilan sanaydi, lekin ulanganda
har doim admindagi qiymat bilan sinxronlanadi.

**Vaqt har doim sanaladi** — kompyuter o'chiq bo'lsa ham, admin dasturi yopiq bo'lsa
ham. Ya'ni o'ynalgan vaqt = hozirgi vaqt − boshlangan vaqt − pauzalar (prototipdagidek).
Klub yopilib ertasiga ochilsa, ochiq qolgan seansning vaqti o'sha davrda ham sanaladi.

## 2. PlayStation zonasi (2026-09-17)

PS konsollarida Windows agent ishlamaydi. Bunday kompyuterlar **agentsiz** bo'ladi:
taymer va hisob faqat adminda yuritiladi, qulf ekrani va masofadan boshqaruv yo'q.
Bazada kompyuterning "agentsiz" belgisi bo'ladi; zal kartasida ham shunday ko'rsatiladi.

## 3. Runpad Pro (2026-09-17)

Runpad Pro klient kompyuterlarda **Windows qobig'i (shell) sifatida** o'rnatilgan.
Shuning uchun:
- agent qobiqni almashtirmaydi, faqat uning ustida ishlaydi;
- qulf ekrani va taymer oynachasi topmost oyna sifatida chiqadi;
- `explorer.exe` yo'qligini hisobga olish kerak (masalan, tray ikonkaga tayanib bo'lmaydi).

## 4. Qulf ekrani fon rasmi (2026-09-17)

Rasm har `lock.config` bilan yuborilmaydi. Admin rasmning **hash**ini yuboradi, agent
o'zida keshlangan rasm bilan solishtiradi va faqat farq bo'lsa rasmni alohida so'raydi.
Keshlangan rasm agentda diskda saqlanadi.

## 5. Shifrlash (2026-09-17)

LAN ichida **oddiy WebSocket (ws://)** yetarli — WSS ishlatilmaydi. Himoya:
- juftlash tokeni har bir xabarda tekshiriladi;
- token klientda DPAPI bilan shifrlangan holda saqlanadi;
- parollar (operator va mijoz) bazada hash bilan saqlanadi;
- noma'lum yoki tokensiz klient buyruqlari rad etiladi.

## 6. Windows Firewall (2026-09-17)

Qoidani **o'rnatuvchi** qo'shadi (o'rnatish paytida allaqachon administrator huquqi bor),
dastur ishga tushganda UAC so'ramaydi. Admin dasturi faqat qoida yo'qligini tekshiradi
va yo'q bo'lsa ogohlantirish ko'rsatadi.

## 7. Ctrl+Alt+Del va Task Manager (2026-09-17)

Ctrl+Alt+Del ni hook bilan to'sib bo'lmaydi. Qulf paytida Task Manager registry
siyosati bilan o'chiriladi va qulf ochilganda qaytariladi. **Failsafe:** xizmat har
ishga tushganda (qulf faol bo'lmasa) siyosatni majburan tiklaydi — agent yiqilib
qolsa ham kompyuter "Task Manager o'chiq" holatda qolib ketmaydi.

**Amalga oshirilishi (6-bosqich).** Siyosat `HKCU` da saqlanadi, xizmat esa
LocalSystem huquqi bilan ishlaydi va boshqa foydalanuvchining `HKCU` iga
to'g'ridan-to'g'ri yoza olmaydi. Shuning uchun siyosatni qobiq qo'yadi va oladi:
qobiq har ishga tushganda uni majburan tiklaydi, yopilganda ham tiklaydi, xizmat
esa qobiqni yiqilsa 3 soniyada qayta ochadi. Natija qarorda yozilganidek —
"Task Manager o'chiq" holat qolib ketmaydi. O'chirish (uninstall) paytida ham
siyosat tozalanadi.

## 8. Standart ma'lumotlar (2026-09-17)

Bo'sh bazada faqat: bitta **Standart** zona (10 000 so'm/soat), bitta parolsiz
**Admin** operatori va standart **chiqim turlari** (Tovar xaridi, Ish haqi / avans,
Kommunal, Xo'jalik, Ta'mirlash, Boshqa). Ilova shablonlari va boshqa demo
ma'lumotlar yozilmaydi.

## 9. Kompyuterni ro'yxatga qo'shish (2026-09-21)

Klient agent o'zini avtomatik ro'yxatga **qo'shmaydi**. Kompyuter avval adminda
qo'lda yaratilgan bo'lishi kerak (Sozlamalar → Kompyuterlar). Juftlashda agent
yuborgan nom shu ro'yxatdagi nom bilan solishtiriladi:

- nom topilsa va kod to'g'ri bo'lsa — juftlanadi, token beriladi;
- nom topilmasa — `pair.denied` va operatorga tushunarli sabab qaytariladi.

## 10. Admin bilan aloqa yo'q bo'lganda (2026-09-21)

Agent adminni topa olmasa va o'zida saqlangan ochiq seans bo'lmasa — kompyuter
**qulflanadi**. Ya'ni standart holat "qulflangan": pulsiz o'ynab bo'lmaydi.

Saqlangan ochiq seans bo'lsa, agent uni monotonik soat bilan davom ettiradi va
vaqt tugaguncha kompyuter ochiq qoladi (admin yopiq bo'lsa ham).

## 11. Favqulodda qulfni ochish (2026-09-21)

Klient kompyuterda qulfni qo'lda ochish uchun:

- **Kombinatsiya:** `Ctrl + Alt + P` (adminda o'zgartirish mumkin)
- **Parol:** alohida **xizmat paroli** — adminda Sozlamalar → Kompyuterlar bo'limida
  o'rnatiladi va agentlarga yuboriladi. Agent uni hash bilan saqlaydi, shuning uchun
  admin dasturi o'chiq bo'lsa ham ishlaydi.

Xuddi shu parol klient ekranidagi ⚙ (ulanish sozlamalari) tugmasi uchun ham
ishlatiladi. Parol o'rnatilmagan bo'lsa, ikkala amal ham tasdiqlash bilan ochiladi.

## 12. Kiritishni bloklash — qattiq variant (2026-09-22)

Qulf ekrani ochiq bo'lganda klaviatura va sichqoncha past darajali ilgaklar bilan
to'siladi (WH_KEYBOARD_LL / WH_MOUSE_LL): Win, Alt+Tab, Alt+Esc, Alt+F4, Ctrl+Esc,
kontekst tugmasi va qulf oynasidan tashqaridagi bosishlar. Oddiy yozuv (login va
parol) va favqulodda kombinatsiya (Ctrl+Alt+P) hamisha o'tadi.

Xavfsizlik: ilgaklar jarayonga bog'langan — qobiq yiqilsa yoki yopilsa Windows
ularni o'zi olib tashlaydi, ya'ni kompyuter "kiritish bloklangan" holda qolib
ketmaydi. Qaysi tugma to'silishi sof mantiq sifatida `InputPolicy` sinfida va
testlar bilan qoplangan.

## 13. Wake-on-LAN: MAC va paket yo'llari (2026-09-22)

**Muammo:** masofadan yoqish ishlamasdi.

**Sabab 1 — noto'g'ri MAC.** Agent "birinchi ishlayotgan karta" MAC'ini yuborardi.
VirtualBox, VMware, Hyper-V, WSL yoki VPN o'rnatilgan kompyuterda bu ko'pincha
virtual karta bo'lib chiqadi; unga yuborilgan sehrli paket hech qachon
kompyuterni uyg'otmaydi.

**Yechim:** agent avval **adminga boradigan yo'l**ni aniqlaydi (UDP soket
"ulanadi" — hech narsa yuborilmaydi, Windows shunchaki marshrutni tanlaydi),
keyin o'sha lokal manzil egasi bo'lgan kartaning MAC'ini oladi. Zaxira yo'lda
virtual kartalar nomi/tavsifi bo'yicha chetlab o'tiladi, simli karta birinchi
turadi.

**Sabab 2 — paket faqat bitta yo'nalishga ketardi.** `255.255.255.255` ga
yuborilgan paket faqat standart marshrut kartasidan chiqadi; admin kompyuterda
Wi-Fi + kabel yoki VPN bo'lsa, klient turgan tarmoqqa yetib bormaydi.

**Yechim:** paket har bir lokal IPv4 kartadan alohida soket bilan, ham umumiy,
ham o'sha tarmoqning broadcast manziliga, **9 va 7-portlarga** yuboriladi;
kompyuterning oxirgi ma'lum IP manziliga ham. Nechta paket ketgani jurnalga
yoziladi — "ishlamadi" ni tekshirish uchun shu qator kerak.

**Sabab 3 — fast startup.** Windows "tez ishga tushirish" yoqilgan bo'lsa
`shutdown /s` kompyuterni to'liq o'chirmaydi (gibrid uyqu) va ko'p kartalar
sehrli paketni qabul qilmaydi. Agent buni ulanishda aniqlab adminga aytadi,
admin esa jurnalga ogohlantirish yozadi. Sozlamani o'zimiz o'zgartirmaymiz —
bu butun kompyuterga taalluqli tizim sozlamasi.

## 14. Pauza kompyuterni qulflaydi (2026-09-22)

Vaqtni to'xtatish faqat taymerni to'xtatsa, mijoz pauza davomida bemalol
o'ynayverardi — ya'ni pauza bepul vaqtga aylanardi.

Endi seans pauzaga o'tganda klient ekrani `locked` holatiga o'tadi: qulf ekrani
ochiladi, taymer oynachasi yashiriladi va (sozlamada yoqilgan bo'lsa) klaviatura
bilan sichqoncha to'siladi — bloklash allaqachon `locked` holatiga bog'langan.
Qulf ekranida login o'rniga "Vaqt to'xtatilgan" deb yoziladi: seans allaqachon
ochiq, qolgan vaqt saqlanib turibdi.

Holat diskka ham yoziladi, shuning uchun pauza paytida kompyuter o'chib yonsa
ham qulflangan holda ochiladi.

## 15. Navbatdagi buyruqlar eskirmaydi (2026-09-22)

Klient ulanmagan paytda yuborilgan buyruqlar navbatda muddatsiz saqlanardi va
kompyuter keyingi safar ulanganda hammasi birdan yuborilardi. Natijada operator
o'chiq kompyuterga "O'chirish" bosgan bo'lsa, kompyuter yonganidan bir necha
soniya keyin yana o'chib qolardi — jurnalda bu "aloqa o'zidan o'zi uzildi"
bo'lib ko'rinardi.

Endi:

- **quvvat, jarayon, seans va ko'rinish buyruqlari umuman navbatga qo'yilmaydi**
  (`power.off`, `power.reboot`, `process.*`, `session.*`, `time.sync`,
  `lock.config`, `theme.set`) — ular faqat ulangan klient uchun ma'noga ega,
  seans holati va sozlamalar esa ulanish paytida `hello.ok` / `sync` ichida
  to'liq yuboriladi;
- qolganlari (masalan operator xabari) **60 soniya** saqlanadi, undan keyin
  eskirgan deb tashlanadi va jurnalga yoziladi.

## 16. Ulanish tsikli hech qachon jim to'xtamaydi (2026-09-22)

**Nosozlik:** admin dasturi yopilib qayta ochilgandan keyin klient boshqa
ulanmadi. Klient jurnalining oxirgi qatori `Ulanmoqda: ws://…` bo'lib qoldi —
undan keyin **umuman hech narsa yozilmadi**, xizmat esa ishlashda davom etdi.

**Sabab:** ulanishning o'z taymeri (10 soniya) ham, xizmatning to'xtashi ham
`OperationCanceledException` tashlaydi. Tsiklda ular ajratilmagan edi:

```csharp
catch (OperationCanceledException) { return; }   // ← taymer ham shu yerga tushardi
```

Admin yopilgan paytda ulanish osilib qoldi, 10 soniyadan keyin taymer ishladi
va tsikl **butunlay chiqib ketdi**. Shell va quvur tsikllari ishlayotgani uchun
xizmat to'xtamadi, shuning uchun "agent to'xtatildi" xabari ham yozilmadi.

**Yechim:**

- `catch (OperationCanceledException) when (ct.IsCancellationRequested)` — faqat
  xizmat to'xtayotganda chiqamiz; taymer ishlasa ogohlantirish yozib, qayta
  urinamiz;
- ulanish taymeri sozlanadigan bo'ldi (`ConnectTimeout`) — sinovda qisqartiriladi;
- himoya qatlami: `AgentWorker.SuperviseAsync` — tsikl qandaydir sabab bilan
  tugab qolsa, xato jurnalga yoziladi va tsikl qayta ishga tushiriladi.

Sinov: TCP ulanishni qabul qilib javob bermaydigan soxta server bilan tsikl
kamida ikki marta urinishi tekshiriladi.

## 17. Avto yoqish: paket haqiqatan ketadi va takrorlanadi (2026-09-23)

**Muammo.** "Kompyuter o'chiq bo'lsa, avtomatik yoqilsin" sozlamasi amalda
ishlamasdi. Seans ochish yo'lida jurnalga `Avto yoqish: Wake-on-LAN yuborildi`
deb **yozilar**, lekin paket hech qachon **ketmasdi** — haqiqiy paketni
yuboradigan `pcs.wake()` faqat qo'lda bosiladigan "Yoqish" tugmasidan va
`reboot()` dan chaqirilardi. Vaqt qo'shilganda esa yoqish mantig'i umuman
yozilmagan edi, garchi sozlama tagida "Vaqt qo'shilganda yoki seans ochilganda
… yuboriladi" deb turgan bo'lsa ham.

Jurnaldagi yolg'on yozuv alohida zarar keltirdi: operator "yuborildi" deb
o'qib, muammoni BIOS va tarmoq kartasidan qidirdi.

**Yechim.**

- Paket yuborish `src/main/net/wake.ts` ga ajratildi (`sendWake`). Alohida
  modul, chunki uni ham `services/pcs.ts`, ham `services/sessions.ts`
  ishlatadi — bir-birini import qilsa aylanma bog'liqlik bo'lardi.
- `sendWake` jurnalga **natijaga qarab** yozadi: MAC yo'q bo'lsa "yuborildi"
  deb yozilmaydi.
- `startOperatorSession`, `startAccountSession` va `addTime` haqiqiy paket
  yuboradi. Paket tranzaksiyadan **keyin** ketadi — seans yozuvlari saqlangach.
- `addTime` da shart: avto yoqish yoqilgan, agentsiz qurilma emas, operator
  o'zi o'chirmagan (`pc_off`) va agent hozir oflayn.

**Takroriy urinish.** Tok uzilib qaytganda kompyuter o'zi yonmaydi (BIOS'da
`Restore AC Power Loss` = `Last State` qo'yilmagan bo'lsa), lekin standby toki
tiklangach tarmoq kartasi sehrli paketni qabul qila boshlaydi — bir necha
o'n soniyadan keyin. Bitta paket bunga ulgurmaydi.

Shuning uchun seans ochiq, lekin agent oflayn kompyuterga **har 60 soniyada,
10 daqiqa davomida** paket yuboriladi (`net/live.ts`: `armWakeRetry`,
`dueWakeRetries`). Urinish to'xtaydi: agent ulanganda, seans yopilganda,
operator kompyuterni o'zi o'chirganda (`pc_off`) yoki muddat tugaganda.

Urinish **boshlanadi**: seans ochilganda o'chiq kompyuterda, vaqt qo'shilganda
agent oflayn bo'lsa, va agent seans ochiq turib yo'qolganda (`server.ts`
`markOffline`) — oxirgisi aynan tok uzilishi holati. Birinchi urinish darhol
emas, bir daqiqadan keyin: oddiy qayta yuklash bunga ulguradi va urinishsiz
o'ziga keladi.

Hammasi `autoWake` sozlamasiga bo'ysunadi — o'chirilgan bo'lsa hech qanday
avtomatik paket yuborilmaydi.

### Shu qatorda topilgan yana ikki xato (2026-09-23)

**`resumeFromOff` ham paket yubormasdi.** Admindan o'chirilgan (seansi ochiq)
kompyuterda "Yoqish" bosilganda jurnalga `Wake-on-LAN yuborildi` deb yozilar,
lekin paket ketmasdi — aynan yuqoridagi xatoning nusxasi. Endi `sendWake`
ishlatiladi.

**"Qulflash, keyin o'chirish" to'lov kutgan seansda ishlamasdi.** Vaqt o'zi
tugaganda, lekin bar yoki qarz to'lanmagan bo'lsa, seans yopilmay `expired = 1`
bo'lib qolardi va operator uni **qo'lda** yakunlardi. Qo'lda yakunlashda
`reason !== 'expire'` bo'lgani uchun `off_at` qo'yilmas, kompyuter hech qachon
o'chmasdi.

Endi `closeSessionRow` da `timeRanOut = reason === 'expire' || s.expired`:
vaqti o'zi tugagan seans qachon yopilishidan qat'i nazar "Vaqt tugaganda"
qoidasi amal qiladi. Operator hali vaqti tugamagan seansni qo'lda yakunlasa —
oldingidek hech narsa rejalashtirilmaydi (u ataylab yakunladi).

`Ekran qulflandi` xabari esa faqat vaqt aynan tugagan paytda yoziladi, to'lov
kutgan seans yakunlanganda takrorlanmaydi.

### "Qulflash, keyin o'chirish" — klient tomonida zaxira (2026-09-23)

Kechiktirilgan o'chirishni admin boshqaradi: seans yopilganda bazaga "shu
vaqtdan keyin o'chsin" belgisi (`off_at`) qo'yiladi va vaqti kelganda
`power.off` yuboriladi.

Ammo agentda `offDelay` maydoni **umuman ishlatilmasdi** — u faqat protokol
DTO'sida turardi. Admin dasturi yopiq yoki tarmoq uzilgan bo'lsa hech kim
buyruq yubormasdi va kompyuter qulflangan holda cheksiz yonib turardi.

`Dust2Agent.Common/ExpirePolicy.cs` qo'shildi (sof mantiq, testlar bilan):
vaqt tugaganda agent o'zi ham o'chish vaqtini hisoblaydi va asosiy tsiklda
tekshirib turadi. Bu **zaxira** yo'l — admin ulangan bo'lsa buyruq baribir
undan keladi; ikkalasi ishlasa ham kompyuter bir marta o'chadi.

O'chirish bekor bo'ladi: yangi vaqt ochilsa (sozlamada ham "yangi vaqt
ochilmasa" deb yozilgan) yoki klient dasturi Ctrl+Alt+P bilan to'xtatilgan
bo'lsa. Sozlamadagi qiymat 1–120 daqiqa oralig'iga tushiriladi.

## 18. Ulanish sozlamalari faqat kombinatsiya orqali (2026-09-23)

Qulf ekranining pastki o'ng burchagida **"⚙ Ulanish sozlamalari"** tugmasi
har doim turardi — hech qanday shartsiz (`LockWindow.xaml`).

Bu ikki jihatdan noto'g'ri edi:

- mijoz qulflangan kompyuter oldida o'tirib admin IP manzili, porti va
  kompyuter nomini ochib ko'ra olardi;
- **xizmat paroli o'rnatilmagan bo'lsa** (standart holat — `servicePassHash: ''`)
  uni o'zgartira ham olardi: admin manzilini boshqa narsaga almashtirsa,
  kompyuter "adminsiz" qolardi.

Endi tugma **faqat juftlanmagan** kompyuterda ko'rinadi
(`ShellScreen.ShowSetupButton`). Juftlangandan keyin bu oynaga yagona yo'l —
maxfiy kombinatsiya (standarti `Ctrl+Alt+P`, adminda o'zgartiriladi).

Juftlanmagan kompyuterda tugma kerak bo'lib qoladi: ulanish kodi
kiritilayotganda ekran `locked` bo'ladi va orqaga qaytish yo'li shu.

Birinchi sozlash buzilmaydi — juftlanmagan kompyuterda ekran allaqachon
`setup` bo'ladi, sozlamalar to'g'ridan-to'g'ri ochiladi.

### Ochiq qolgan xavf

Xizmat paroli o'rnatilmagan bo'lsa, kombinatsiya **parolsiz** ishlaydi va
"Dasturni to'xtatish" orqali kompyuterni ochib yuborish mumkin
(`LockWindow.xaml.cs` — `if (!_state.HasServicePassword) ShowActions()`).

Shuning uchun parolni o'rnatish shart: Sozlamalar → Kompyuterlar →
"Klient xizmat paroli". Adminda u o'rnatilmagan bo'lsa qizil "Yo'q" deb
turadi.

## 19. Kassa qoldig'i faqat to'g'ri chiqqanda ko'rsatiladi (2026-09-23)

Hisobotdagi "Kassa harakati" bloki boshlang'ich kassani **har doim hozirgi
ochiq smenadan** olardi:

```ts
openCash: currentShift(db)?.open_cash ?? 0
```

Natijada o'tgan davr hisobotida butunlay yolg'on raqam chiqardi. Sinovda:
kechagi smena 100 000 bilan ochilgan, 50 000 naqd tushgan; bugungi smena
999 999 bilan ochilgan. "Kecha" hisobotida ekranda **1 049 999** deb turardi
(to'g'risi 150 000). Bir kunda ikki smena bo'lsa "Bugun" ham buzilardi:
birinchi smenaning harakati ikkinchisining boshlang'ich kassasi ustiga
qo'shilib ketardi.

**Yechim — o'zini o'zi tekshiradigan qoida.** Zanjir (boshlang'ich + davr
harakati = bo'lishi kerak) faqat raqamlar haqiqatan kassadagi hozirgi naqdga
to'g'ri kelganda ko'rsatiladi:

```ts
return s.open_cash + T.cash === kassaNow(db) ? s.open_cash : null;
```

Mos kelmasa `null` qaytadi va ekranda boshlang'ich/"bo'lishi kerak" qatorlari
umuman chiqmaydi — o'rniga **davr bo'yicha naqd o'zgarishi** va "Hozir
kassada: X" izohi ko'rsatiladi. Shu bilan noto'g'ri qoldiq chiqishi mumkin
emas: ishonch hosil qilib bo'lmasa, da'vo qilinmaydi.

"Kassada naqd" kartasi esa endi har doim `kassaNow` ni ko'rsatadi — bu "hozir
kassada qancha bor" degan fakt, davrga bog'liq emas.

## 20. Yuqori panel tugmalari sozlanadi (2026-09-23)

Tugmalar `index.html` da qo'lda yozilgan edi. Har bir klubda ishlash tartibi
boshqacha: kimdir "Jarayonlar" ni umuman ishlatmaydi, kimdirga "Xabar" har
kuni kerak. Shuning uchun ro'yxat sozlamaga chiqarildi.

- `common/pcbar.ts` — tugmalar katalogi (id, nom, izoh, guruh, belgi). Panel
  ham, Sozlamalardagi tahrirlash ro'yxati ham shu bitta manbadan quriladi.
- `ui.toolbar: string[]` — ko'rinadigan tugmalar, tartib bilan. Ro'yxatda
  yo'q tugma chiqmaydi.
- `normalizeBar()` saqlangan ro'yxatni tozalaydi: noma'lum id'lar va
  takrorlar tashlanadi, bo'sh qolsa standart ro'yxat qaytadi — panel
  butunlay yo'qolib qolmasin.
- Ajratgich chiziqlari guruh almashganda o'zi qo'yiladi, shuning uchun
  tartib o'zgarsa ham ko'rinish tartibli qoladi.
- Panel faqat ro'yxat o'zgarganda qayta quriladi (`barSig`): har soniyalik
  yangilanishda DOM almashtirilsa, fokus va hover yo'qolib ketardi.

**Yangi ikki tugma.** "Ko'chirish" — mijoz vaqtini boshqa bo'sh kompyuterga
o'tkazish (aynan bitta ochiq seans kerak). "Xabar" — mijoz ekraniga matn
yuborish (bir nechta kompyuterga ham bo'ladi). Ikkalasi ham yon paneldagi
oynalarning o'zini ishlatadi: `moveDialog` va `messageDialog` umumiy qilindi,
kod ikki joyda takrorlanmaydi.

## 21. Seans davomida bildirishnomalar alohida oynada (2026-09-23)

**Muammo.** Admindan yuborilgan xabar mijozga ko'rinmasdi. Sabab: seans
ochiq bo'lganda qulf oynasi yashiriladi (`App.OnState` — `w.Hide()`), xabar
esa aynan o'sha oynaga yozilardi (`ShowMessage` → `MessageOverlay`).

Bu faqat xabarga tegishli emasdi — toastlar ham o'sha yashirin oynaga
borardi (`_agent.Toast += t => Primary()?.ShowToast(t)`). Demak seans
davomida mijoz **hech narsa ko'rmasdi**:

- admindan xabar;
- "N daqiqa qoldi" ogohlantirishi (`warnMin` sozlamasi);
- "+30 daqiqa qo'shildi", bonus va jarima xabarlari;
- qobiqning o'z xatolari (`ShowFault`).

**Yechim.** `NoticeWindow` — o'yin ustida turadigan kichik oyna. Ekranning
tepa o'rtasida chiqadi, `ShowActivated="False"` bilan fokusni tortib olmaydi
(aks holda o'yindan chiqarib yuborardi).

- Ogohlantirish 8 soniyadan keyin o'zi yo'qoladi.
- **Admin xabari** esa mijoz "Tushunarli" tugmasini bosmaguncha turadi —
  operator yozgan narsa e'tibordan chetda qolmasin.

Qaysi oyna ishlatilishini `ShellScreen.NoticeInOwnWindow(screen)` hal qiladi:
seans ochiq bo'lsa yangi oyna, qolgan hollarda qulf oynasining o'z qatlami
(u butun ekranni egallaydi va chiroyliroq). Qulf ekraniga o'tilganda yangi
oyna yopiladi — ikkita "topmost" oyna bir-birini to'smasin.

## 22. Xavfsizlik tuzatishlari (2026-09-29)

**Muammo.** Kod tekshiruvida quyidagilar topildi:

- Xizmat paroli faqat qobiq oynasida so'ralardi. Xizmat quvurdan kelgan `pause`,
  `unpair` va `connect` ni tekshiruvsiz bajarardi, quvur esa hamma foydalanuvchilarga
  ochiq edi. Seans paytida mijoz kichik skript bilan qulfni butunlay o'chira olardi.
- Juftlash kodi va mijoz paroliga urinishlar cheklanmagan edi. 6 xonali kod LAN'da
  tez topiladi. Keyin shu nom bilan juftlangan kompyuter o'rnini egallab, xizmat
  paroli hashini olish mumkin edi.
- Operator, admin va mijoz parollari bazada va backup'da ochiq matnda turardi.
  Mijoz paroli tahrirlash oynasida ko'rinardi.

**Yechim.**

- **Xizmat o'zi tekshiradi.** `ShellCommandPolicy` — admin parol o'rnatgan bo'lsa,
  `pause`/`unpair`/`connect` uchun parol shart. `ServicePasswordGate` to'g'ri
  paroldan keyin 5 daqiqa ruxsat beradi. Ruxsat qobiq uzilganda va parol
  o'zgarganda bekor bo'ladi. 5 ta xatodan keyin 1 → 2 → 4 → 8 → 15 daqiqa blok
  qo'yiladi. Qobiq ham bunga moslashdi: ulanish sozlamasini saqlash va uzishdan
  oldin parol so'raydi, agar yaqinda kiritilmagan bo'lsa.
- **Quvurga faqat qobiq ulanadi.** Xizmat rejimida ulangan jarayonning yo'li
  (`GetNamedPipeClientProcessId` + `QueryFullProcessImageName`) xizmat yonidagi
  `Dust2Agent.Shell.exe` bilan solishtiriladi (`ShellIdentity`). Konsol (sinov)
  rejimida bu tekshiruv o'chiq.
- **Ulanish kodini admin o'zi belgilaydi.** Dastur kodni o'zi yaratmaydi: «Yangi kod»
  (tasodifiy) tugmasi o'rniga «O'zgartirish» qo'yildi (6 ta raqam, faqat admin).
  Standart `888518` qoldi, u o'zgartirilmaguncha ogohlantirish ko'rinadi. Standart
  xizmat paroli `0000` ham o'zgarmadi.
- **Urinishlar cheklandi.** Noto'g'ri kod: bitta IP'dan 10 daqiqada 5 ta → shu
  IP'ga 10 daqiqa blok; hammasi bo'lib 20 ta → juftlash 10 daqiqaga to'xtaydi.
  Mijoz paroli: bitta kompyuterdan 5 daqiqada 5 ta, bitta akkauntga 15 daqiqada
  10 ta. Bitta kompyuterdan bir vaqtda faqat bitta tekshiruv o'tadi, shuning uchun
  parallel so'rovlar cheklovni chetlab o'tolmaydi. Blok vaqtida rad etishlar
  jurnalni to'ldirmaydi. Yangi sabab: `rate_limited`.
- **Qayta juftlash — faqat «Uzish» dan keyin.** Juftlangan kompyuter nomi bilan
  kelgan `pair.request` rad etiladi (`already_paired`, protokolda oldindan
  yozilgan edi). Kompyuterlar jadvaliga «Uzish» tugmasi qo'shildi; seans ochiq
  bo'lsa ishlamaydi.
- **Parollar hash qilinadi.** Format klient agentdagi xizmat paroli bilan bir xil:
  `pbkdf2$120000$salt$hash` (SHA-256). Eski ochiq parollar dastur ochilganda fonda
  hashga o'tkaziladi, shu vaqt ichida eski parol bilan kirish ham ishlaydi.
  `defPw` belgisi eski prototip parollari (admin / 1111) haqidagi eslatmani saqlab
  qoladi. Parolsiz mijoz akkauntiga qulf ekranidan kirib bo'lmaydi.
- Mayda tuzatishlar: ikkinchi nusxa endi oyna ham, server ham ochmaydi;
  arxiv vaqti 32 bitga qirqilmaydi (`db.js`); ochilmay qolgan WebSocket server
  yopiladi.

**Keyinga qoldirildi.**

- Tarmoq trafigini shifrlash (`wss://` va sertifikat izini tekshirish) — ikkala
  dasturni bir vaqtda yangilashni talab qiladi.
- Qobiq tomonida serverni tekshirish (quvur nomini boshqa dastur egallab olishi)
  va qobiqni ketma-ket yopib turishga qarshi himoya.
