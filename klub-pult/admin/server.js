// Klub Pult server: klient kompyuterlar WebSocket orqali ulanadi (LAN, standart port 7777).
// Server faqat "quvur" vazifasini bajaradi: xabarlarni oynaga (index.html) uzatadi va javoblarni
// klientga yuboradi. Butun biznes mantiq (seans, pul, juftlash) index.html ichida.
const { WebSocketServer } = require('ws');
const dgram = require('dgram');
const os = require('os');

let wss = null, port = 0, lastErr = '', seq = 0;
const socks = new Map(); // id -> ws
let emit = () => {};

function localIps() {
  const out = [];
  for (const [name, list] of Object.entries(os.networkInterfaces()))
    for (const a of list || []) if (a.family === 'IPv4' && !a.internal) out.push({ ip: a.address, name, mac: a.mac });
  return out;
}
function info() { return { running: !!wss && !lastErr, port, err: lastErr, ips: localIps(), clients: socks.size }; }

function stop() {
  if (!wss) return;
  for (const ws of socks.values()) try { ws.terminate(); } catch (e) {}
  socks.clear();
  try { wss.close(); } catch (e) {}
  wss = null;
}

function start(p, onEvent) {
  if (onEvent) emit = onEvent;
  p = Math.round(+p) || 7777;
  if (wss && p === port && !lastErr) return Promise.resolve(info());
  stop();
  port = p; lastErr = '';
  return new Promise(resolve => {
    const s = new WebSocketServer({ host: '0.0.0.0', port: p, path: '/agent', maxPayload: 1 << 20 });
    let done = false;
    const finish = () => { if (!done) { done = true; resolve(info()); } };
    s.on('listening', () => { wss = s; finish(); });
    s.on('error', e => {
      lastErr = e.code === 'EADDRINUSE' ? `${p}-port band (boshqa dastur ishlatyapti)` : (e.message || String(e));
      emit({ type: 'server', info: info() });
      finish();
    });
    s.on('connection', (ws, req) => {
      const id = 'c' + (++seq);
      const ip = String(req.socket.remoteAddress || '').replace(/^::ffff:/, '');
      socks.set(id, ws);
      ws.isAlive = true;
      ws.on('pong', () => { ws.isAlive = true; });
      ws.on('message', data => {
        let msg; try { msg = JSON.parse(String(data)); } catch (e) { return; }
        if (msg && typeof msg === 'object') emit({ type: 'msg', id, msg });
      });
      ws.on('close', () => { socks.delete(id); emit({ type: 'close', id }); });
      ws.on('error', () => {});
      emit({ type: 'open', id, ip });
    });
  });
}

// ulanish uzilganini tez aniqlash (kabel sugʻurilsa, kompyuter qotib qolsa)
setInterval(() => {
  for (const [id, ws] of socks) {
    if (!ws.isAlive) { try { ws.terminate(); } catch (e) {} continue; }
    ws.isAlive = false;
    try { ws.ping(); } catch (e) {}
  }
}, 10000).unref();

function send(id, msg) {
  const ws = socks.get(id);
  if (ws && ws.readyState === 1) try { ws.send(JSON.stringify(msg)); } catch (e) {}
}
function close(id) { const ws = socks.get(id); if (ws) try { ws.close(4000, 'replaced'); } catch (e) {} }

// Wake-on-LAN: magic packet (6×FF + 16×MAC) UDP broadcast, 9 va 7-portlar
function wol(mac) {
  const hex = String(mac || '').replace(/[^0-9a-f]/gi, '');
  if (hex.length !== 12) return Promise.resolve({ ok: false, err: 'MAC manzil notoʻgʻri' });
  const m = Buffer.from(hex, 'hex'), pkt = Buffer.alloc(102, 0xff);
  for (let i = 0; i < 16; i++) m.copy(pkt, 6 + i * 6);
  const targets = new Set(['255.255.255.255']);
  for (const a of localIps()) { // har bir tarmoq uchun subnet broadcast (masalan 192.168.1.255)
    const nm = (os.networkInterfaces()[a.name] || []).find(x => x.address === a.ip);
    if (nm && nm.netmask) targets.add(a.ip.split('.').map((o, i) => (+o | (~+nm.netmask.split('.')[i] & 255))).join('.'));
  }
  return new Promise(resolve => {
    const sock = dgram.createSocket('udp4');
    sock.on('error', e => { try { sock.close(); } catch (x) {} resolve({ ok: false, err: e.message }); });
    sock.bind(() => {
      sock.setBroadcast(true);
      let left = targets.size * 2;
      for (const t of targets) for (const p of [9, 7]) sock.send(pkt, p, t, () => { if (--left === 0) { sock.close(); resolve({ ok: true }); } });
    });
  });
}

module.exports = { start, stop, info, send, close, wol };
