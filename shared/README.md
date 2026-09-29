# Tarmoq protokoli

Admin dasturi va klient agent WebSocket orqali JSON xabarlar almashadi.

`protocol.md` va `schemas/` — **3-bosqichda** to'ldiriladi (hozircha bo'sh).
Xabarlar ro'yxati va umumiy ko'rinish `docs/TOPSHIRIQ.md` ning 2.3-bo'limida,
haqiqiy oqim esa `docs/prototype/dust2-klient-prototip.html` dagi
"Admin bilan aloqa" jurnalida ko'rsatilgan.

Umumiy konvert:

```json
{ "type": "session.start", "id": "<uuid>", "ts": 1700000000000, "payload": { } }
```

Har bir buyruqqa `ack` yoki `error` javob qaytadi; yetib bormagan buyruqlar navbatda qoladi.
