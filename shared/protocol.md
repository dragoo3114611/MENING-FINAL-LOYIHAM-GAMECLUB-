# DUST2 tarmoq protokoli

Admin dasturi (server) va klient agent (klient) o'rtasidagi aloqa.

| | |
|---|---|
| Transport | WebSocket, `ws://<admin-ip>:<port>/agent` |
| Standart port | **7777** (Sozlamalar → Kompyuterlar → Server manzili) |
| Format | JSON (UTF-8), har bir xabar — bitta matn freym |
| Shifrlash | yo'q — faqat lokal tarmoq ([qaror 5](../docs/QARORLAR.md)) |
| Versiya | `1` |

---

## 1. Xabar konverti

Har bir xabar bir xil konvertga o'ralgan:

```json
{
  "v": 1,
  "type": "session.start",
  "id": "5f1c…",
  "ts": 1758451200000,
  "payload": { }
}
```

| Maydon | Turi | Izoh |
|---|---|---|
| `v` | butun son | protokol versiyasi (hozir `1`) |
| `type` | matn | xabar turi (quyidagi ro'yxat) |
| `id` | matn | UUID — javobni so'rov bilan bog'lash uchun |
| `ts` | butun son | yuboruvchi vaqti, UTC millisekund |
| `payload` | obyekt | xabar turiga bog'liq |

### Javoblar

Buyruq (server → klient yoki klient → server) qabul qilinganda javob qaytadi:

```json
{ "v":1, "type":"ack",   "id":"<uuid>", "ts":…, "payload": { "re":"<so'rov id>" } }
{ "v":1, "type":"error", "id":"<uuid>", "ts":…, "payload": { "re":"<so'rov id>", "code":"…", "message":"…" } }
```

Xato kodlari: `unauthorized`, `bad_request`, `not_found`, `conflict`, `internal`.

### Yetib bormagan buyruqlar

Klient ulanmagan bo'lsa, serverdagi buyruqlar **navbatda** qoladi (kompyuter
uchun alohida navbat, eng ko'pi 100 ta). Klient ulanishi bilan navbat
yuboriladi. Navbat faqat xotirada: admin dasturi qayta ishga tushsa,
o'rniga to'liq `sync` yuboriladi — u holatni baribir to'g'rilaydi.

---

## 2. Ulanish tartibi

### 2.1 Birinchi ulanish — juftlash

Klient kompyuterda operator admin IP manzili, port va **6 xonali ulanish kodi**ni
qo'lda kiritadi.

```
klient → server   pair.request { name, ip, mac, version, fastStartup }  + code
server → klient   pair.ok      { token, pcId, name, serverTime }
                  yoki
server → klient   pair.denied  { reason, message }
```

`pair.request.payload`:

| Maydon | Izoh |
|---|---|
| `name` | kompyuter nomi (adminda ro'yxatdagi nom bilan solishtiriladi) |
| `code` | 6 xonali ulanish kodi |
| `ip`, `mac` | agent aniqlagan manzillar (adminda bo'sh bo'lsa to'ldiriladi) |
| `version` | agent versiyasi |
| `fastStartup` | Windows'da "tez ishga tushirish" yoqilganmi (Wake-on-LAN uchun muhim) |

`mac` — **adminga boradigan yo'ldagi** tarmoq kartasining manzili. Agent avval
marshrutni aniqlaydi (UDP soketning lokal manzili orqali), keyin o'sha
kartaning MAC'ini oladi; virtual kartalar (VMware, VirtualBox, Hyper-V, WSL,
VPN) chetlab o'tiladi. Aks holda Wake-on-LAN mavjud bo'lmagan kartaga
yuborilardi va kompyuter yonmasdi.

`pair.denied.reason` qiymatlari:

| `reason` | Qachon |
|---|---|
| `bad_code` | kod noto'g'ri |
| `unknown_pc` | bunday nomli kompyuter adminda yo'q — **avval qo'lda qo'shilishi kerak** ([qaror 9](../docs/QARORLAR.md)) |
| `already_paired` | shu nomli kompyuter allaqachon juftlangan — avval adminda Sozlamalar → Kompyuterlar → **«Uzish»** bosiladi ([qaror 22](../docs/QARORLAR.md)) |
| `rate_limited` | kod ko'p marta noto'g'ri kiritildi: bitta IP'dan 10 daqiqada 5 ta xato → shu IP 10 daqiqaga, hammasi bo'lib 20 ta xato → juftlash 10 daqiqaga to'xtaydi. Blok vaqtida kod tekshirilmaydi, ulanish yopiladi |

Ulanish kodini admin o'zi belgilaydi (6 ta raqam, Sozlamalar → Kompyuterlar → Server
ulanishi → «O'zgartirish»); dastur kodni o'zi yaratmaydi. Standart kod — `888518`.

Token — 32 baytli tasodifiy qiymat (hex). Klient uni **DPAPI** bilan shifrlab
saqlaydi; server faqat SHA-256 hashini saqlaydi.

### 2.2 Keyingi ulanishlar

```
klient → server   hello    { token, name, ip, mac, version, fastStartup, state? }
server → klient   hello.ok { pcId, name, serverTime, session, lock, behaviour, agent }
```

`state` — klientda saqlangan seans holati (aloqa yo'q paytda o'zgargan bo'lishi
mumkin). Server uni o'z holati bilan solishtiradi va kerak bo'lsa `sync` yuboradi.

Token noto'g'ri bo'lsa — `error { code: "unauthorized" }` va server ulanishni yopadi.
Klient bunda ulanish oynasini qayta ko'rsatadi.

### 2.3 Heartbeat va qayta ulanish

- Klient har **20 soniyada** `heartbeat` yuboradi, server `ack` qaytaradi.
- Server **60 soniya** ichida heartbeat kelmasa, kompyuterni oflayn deb belgilaydi.
- Uzilganda klient eksponensial kechikish bilan qayta ulanadi:
  1s → 2s → 4s → 8s → 16s → 30s (keyin har 30 soniyada).

### 2.4 Agent sozlamalari

`hello.ok` va `sync` ichidagi `agent` obyekti:

| Maydon | Izoh |
|---|---|
| `servicePassHash` | qulfni qo'lda ochish va klientdagi ⚙ uchun parol hashi |
| `unlockCombo` | favqulodda qulfni ochish kombinatsiyasi (standart `Ctrl+Alt+P`) |

Hash formati — `pbkdf2$<takrorlar>$<salt base64>$<hash base64>`, SHA-256.
Agent uni o'zida saqlaydi va oflayn tekshiradi ([qaror 11](../docs/QARORLAR.md)).

---

## 3. Server → klient xabarlari

### Seans

| Tur | Payload | Izoh |
|---|---|---|
| `session.start` | `{ sessionId, mode, endsAt, remainingMs, rate, client? }` | `mode`: `pre` \| `open` \| `acc`. Ochiq vaqtda `endsAt` va `remainingMs` — `null` |
| `session.add_time` | `{ sessionId, endsAt, remainingMs, addedMin }` | ochiq vaqtda taymer paydo bo'lmaydi, faqat xabar ko'rsatiladi |
| `session.pause` | `{ sessionId }` | |
| `session.resume` | `{ sessionId, endsAt, remainingMs }` | |
| `session.end` | `{ sessionId, due }` | `due` > 0 bo'lsa qulf ekranida to'lov summasi ko'rsatiladi |
| `session.lock` | `{ due }` | vaqt tugadi yoki admin qulfladi |
| `sync` | `{ session, lock, behaviour, agent, serverTime }` | to'liq holat — ulanganda va aloqa tiklanganda |

**Vaqt hisobi:** server har doim **mutlaq tugash vaqtini** (`endsAt`, server soati
bo'yicha) va **qolgan millisekundlarni** (`remainingMs`) yuboradi. Agent qolgan
vaqtni monotonik soat bilan sanaydi ([qaror 1](../docs/QARORLAR.md)).

### Ko'rinish va boshqaruv

| Tur | Payload |
|---|---|
| `lock.config` | `{ theme, layout, valign, clockSize, titleSize, textSize, title, text, clock, pcName, login, wallpaperHash }` |
| `behaviour` (sync/hello.ok ichida) | `{ onExpire, offDelay, blockInput, warnMin, showWidget }` |

`lock.config.pcName` — qulf ekranida kompyuter nomi (`PC 01`) soat bilan bitta
ustunda, katta harflar bilan ko'rsatiladimi. Joylashuv va soat o'lchami nomga
ham qo'llanadi.
| `lock.wallpaper` | `{ hash, mime, dataBase64 }` — faqat klient so'raganda |
| `theme.set` | `{ id }` |
| `message` | `{ text }` — mijoz ekranida oyna |
| `process.list` | `{}` — jarayonlar ro'yxatini so'rash |
| `process.list.result` | `{ items: [{ pid, exe, name, cpu, ramMb, protected, kind, hang }], cpu, ramUsedMb, ramTotalMb }` |
| `process.kill` | `{ pids: [] }` — yopilgach agent yangi ro'yxatni o'zi yuboradi |
| `power.off` | `{ mode: "now" \| "after_session" }` |
| `power.reboot` | `{}` |
| `time.sync` | `{ serverTime }` — `time.request` ga javob |

**Kiritishni bloklash.** `blockInput` yoqilgan bo'lsa, qulf ekrani ochiq turganda
klaviatura va sichqoncha past darajali ilgaklar bilan to'siladi: Win, Alt+Tab,
Alt+Esc, Alt+F4, Ctrl+Esc, kontekst tugmasi va qulf oynasidan tashqaridagi
bosishlar. Login/parol yozish va favqulodda kombinatsiya (`unlockCombo`) hamisha
o'tadi. Qulf paytida Task Manager siyosati ham o'chiriladi va qulf ochilganda
tiklanadi ([qaror 7](../docs/QARORLAR.md), [qaror 12](../docs/QARORLAR.md)).
Seans ochiq bo'lganda va ulanish oynasida bloklash ishlamaydi.

**Jarayonlar.** `kind`: `sys` (tizim), `shell` (Runpad yoki DUST2 qobig'i), `app`.
`protected: true` bo'lgan jarayonlar yopilmaydi — agent ularni rad etadi.

Agent o'yin kompyuterida ishlagani uchun ro'yxat yig'ish ataylab yengil:
protsessor foizi oldingi so'rov bilan farqdan olinadi (kutish yo'q), jarayonning
fayl yo'li va tavsifi PID bo'yicha keshlanadi, "javob bermayapti" holati bitta
oynalar aylanishida aniqlanadi va har bir oynaga 60 ms dan ortiq kutilmaydi.
Ro'yxat eng ko'p 250 ta jarayon bilan cheklanadi. Uzoq tanaffusdan keyin
(30 soniyadan ortiq) birinchi so'rov 250 ms o'lchov oladi.

Admin so'rovni `process.list.result` kelguncha kutadi (8 soniya), javob bo'lmasa
operatorda "Klient javob bermadi" chiqadi. Oyna ochiq turganda ro'yxat 4 soniyada
bir yangilanadi; oyna yopilsa yoki dastur kichraytirilsa so'rov yuborilmaydi.

**Klientni vaqtincha to'xtatish.** Klient ekranida Ctrl+Alt+P → "Klient dasturini
to'xtatish" tanlanganda agent `paused` holatiga o'tadi: qulf ekrani ko'rsatilmaydi,
admin bilan ulanish uziladi (adminda kompyuter oflayn ko'rinadi) va qobiq qayta
ochilmaydi. Holat `agent.json` da saqlanadi. Qobiq yorliq orqali qo'lda ochilganda
avtomatik `resume` yuboradi va hammasi tiklanadi.

`showWidget` — seans davomida klient ekranidagi taymer oynachasi ko'rsatilsinmi
(Sozlamalar → Kompyuter xatti-harakati). `false` bo'lsa agent oynachani ochmaydi.

**Fon rasmi** har safar yuborilmaydi: `lock.config` faqat `wallpaperHash` ni olib
keladi, agent o'zidagi kesh bilan solishtiradi va farq bo'lsa `lock.wallpaper.request`
yuboradi ([qaror 4](../docs/QARORLAR.md)).

---

## 4. Klient → server xabarlari

| Tur | Payload | Izoh |
|---|---|---|
| `heartbeat` | `{ state? }` | ixtiyoriy qisqa holat |
| `session.started` | `{ sessionId }` | qulf ochildi, taymer ishga tushdi |
| `session.updated` | `{ sessionId, remainingMs }` | |
| `session.ended` | `{ sessionId, reason }` | `reason`: `expired` \| `admin` \| `logout` |
| `auth.login` | `{ login, password }` | mijoz qulf ekranidan kiradi |
| `message.shown` | `{ re }` | xabar ko'rsatildi |
| `client.unpair` | `{}` — klient o'zini uzdi; admin tokenni bekor qiladi |
| `client.request_time` | `{}` | "Vaqt so'rash" tugmasi |
| `client.call_admin` | `{}` | "Administratorni chaqirish" |
| `client.warning_shown` | `{ minutes }` | ogohlantirish ko'rsatildi |
| `process.list.result` | `{ items: [...], cpu, ramUsedMb, ramTotalMb }` | yuqoridagi jadvalga qarang |
| `time.request` | `{}` | admin vaqtini so'rash |
| `time.synced` | `{ driftMs }` | Windows soati to'g'rilandi |
| `lock.wallpaper.request` | `{}` | keshda rasm yo'q yoki hash boshqacha |

`auth.login` javobi:

```
server → klient   auth.ok     { login, balance, remainingMs, rate }
server → klient   auth.denied { reason }
```

`reason`: `bad_credentials` | `low_balance` | `busy` (kompyuter band yoki mijoz boshqa
kompyuterda) | `rate_limited` (parol ko'p marta noto'g'ri kiritildi: bitta kompyuterdan 5 daqiqada
5 ta xato yoki bitta akkauntga 15 daqiqada 10 ta xato — vaqtincha blok). `message` — mijozga
ko'rsatiladigan matn. Mijoz paroli adminda PBKDF2 hash ko'rinishida saqlanadi.

---

## 5. Vaqt sinxronizatsiyasi

Klient admin vaqtini quyidagi hollarda so'raydi:

1. ulanganda,
2. aloqa tiklanganda,
3. har **10 daqiqada**.

```
klient → server   time.request {}
server → klient   time.sync    { serverTime }
```

Farq **1 soniyadan katta** bo'lsa, agent Windows soatini `SetSystemTime` bilan
to'g'rilaydi va `time.synced { driftMs }` yuboradi. Admin buni kompyuter
qatorining "Soat" ustunida ko'rsatadi.

Seans taymeri Windows soatiga bog'liq emas (monotonik soat), shuning uchun soat
o'zgarishi vaqt hisobini buzmaydi.

---

## 6. Xavfsizlik

- Juftlashdan keyin har bir xabar token bilan bog'langan ulanish orqali ketadi;
  tokensiz ulanishdan faqat `pair.request` qabul qilinadi.
- Noma'lum `type` — `error { code: "bad_request" }`, ulanish yopilmaydi.
- Bitta kompyuter uchun bitta faol ulanish: yangi ulanish eskisini almashtiradi.
- Server faqat lokal tarmoq manzillarini tinglaydi.
- Parollar (operator va mijoz) bazada argon2id hash bilan; token SHA-256 hash bilan.

---

## 7. Sxemalar

`schemas/messages.json` — JSON Schema (draft 2020-12). Konvert va har bir
`payload` uchun `$defs` ichida ta'rif bor. Admin tomonidagi testlar shu sxema
bo'yicha tekshiradi.
