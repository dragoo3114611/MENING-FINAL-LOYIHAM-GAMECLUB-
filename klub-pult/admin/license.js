// Klub Pult litsenziyasi.
// Kalitsiz: 5 kunlik sinov, eng koʻpi 5 ta klient kompyuter.
// Kalit: muallif shaxsiy kaliti (Ed25519) bilan imzolangan, shu admin kompyuterga bogʻlangan matn.
//   KP1.<payload base64url>.<imzo base64url>
//   payload = { v:1, n:"Klub nomi", m:"XXXX-XXXX-XXXX-XXXX", p:<PC soni, 0 = cheksiz>, e:<tugash ms, 0 = muddatsiz>, i:<berilgan ms> }
// Dasturda faqat ochiq kalit bor — undan yangi kalit yasab boʻlmaydi.
const crypto = require('crypto');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFileSync } = require('child_process');

const PUBLIC_KEY = `-----BEGIN PUBLIC KEY-----
MCowBQYDK2VwAyEANLKLjGPNi/HuY1O8x0XRfen7H695wtk/vRDtETL+dlM=
-----END PUBLIC KEY-----`;
const TRIAL_DAYS = 5, TRIAL_PCS = 5, DAY = 864e5;
const REG_KEY = 'HKCU\\Software\\KlubPult';

let dir = '', mcCache = '';

function init(userData) { dir = userData; }

// ---- kompyuter kodi: Windows MachineGuid dan (qayta oʻrnatishda ham oʻzgarmaydi) ----
function machineGuid() {
  if (process.platform === 'win32') {
    try {
      const out = execFileSync('reg', ['query', 'HKLM\\SOFTWARE\\Microsoft\\Cryptography', '/v', 'MachineGuid', '/reg:64'], { encoding: 'utf8', windowsHide: true });
      const m = /MachineGuid\s+REG_SZ\s+(\S+)/i.exec(out);
      if (m) return m[1];
    } catch (e) {}
  }
  try { return fs.readFileSync('/etc/machine-id', 'utf8').trim(); } catch (e) {}
  const macs = Object.values(os.networkInterfaces()).flat().filter(a => a && !a.internal && a.mac && a.mac !== '00:00:00:00:00:00').map(a => a.mac).sort();
  return os.hostname() + '|' + (macs[0] || '');
}
function machineCode() {
  if (!mcCache) mcCache = crypto.createHash('sha256').update('klubpult|' + machineGuid()).digest('hex').slice(0, 16).toUpperCase().match(/.{4}/g).join('-');
  return mcCache;
}

// ---- sinov yozuvi: uch joyda (userData, ProgramData, registry), HMAC bilan ----
const hmac = s => crypto.createHmac('sha256', 'kp-trial|' + machineCode()).update(s).digest('base64url').slice(0, 22);
const enc = r => { const b = Buffer.from(JSON.stringify(r)).toString('base64url'); return b + '.' + hmac(b); };
function dec(s) {
  const [b, h] = String(s || '').trim().split('.');
  if (!b || !h || hmac(b) !== h) return null;
  try { const r = JSON.parse(Buffer.from(b, 'base64url').toString()); return r && r.t > 0 ? r : null; } catch (e) { return null; }
}
function trialFiles() {
  const out = [path.join(dir, '.kp-t')];
  if (process.env.ProgramData) out.push(path.join(process.env.ProgramData, 'KlubPult', '.kp-t'));
  return out;
}
function readTrial() {
  const recs = [];
  for (const f of trialFiles()) { try { const r = dec(fs.readFileSync(f, 'utf8')); if (r) recs.push(r); } catch (e) {} }
  if (process.platform === 'win32') {
    try {
      const out = execFileSync('reg', ['query', REG_KEY, '/v', 'd'], { encoding: 'utf8', windowsHide: true });
      const m = /\bd\s+REG_SZ\s+(\S+)/.exec(out); const r = m && dec(m[1]); if (r) recs.push(r);
    } catch (e) {}
  }
  if (!recs.length) return null;
  return { t: Math.min(...recs.map(r => r.t)), s: Math.max(...recs.map(r => r.s || r.t)) };
}
function writeTrial(r) {
  const s = enc(r);
  for (const f of trialFiles()) { try { fs.mkdirSync(path.dirname(f), { recursive: true }); fs.writeFileSync(f, s); } catch (e) {} }
  if (process.platform === 'win32') {
    try { execFileSync('reg', ['add', REG_KEY, '/v', 'd', '/t', 'REG_SZ', '/d', s, '/f'], { windowsHide: true, stdio: 'ignore' }); } catch (e) {}
  }
}
// sinov boshlangan vaqt va oxirgi koʻrilgan vaqt (soatni orqaga surishni aniqlash uchun)
function trial(now) {
  let r = readTrial();
  if (!r) { r = { t: now, s: now }; writeTrial(r); }
  else if (now > r.s) { r.s = now; writeTrial(r); }
  return r;
}

// ---- kalit ----
function parseKey(key) {
  const k = String(key || '').replace(/\s+/g, '');
  const parts = k.split('.');
  if (parts.length !== 3 || parts[0] !== 'KP1') return { err: 'Kalit formati notoʻgʻri' };
  let ok = false;
  try { ok = crypto.verify(null, Buffer.from('KP1.' + parts[1]), PUBLIC_KEY, Buffer.from(parts[2], 'base64url')); } catch (e) {}
  if (!ok) return { err: 'Kalit yaroqsiz (imzo mos emas)' };
  let p;
  try { p = JSON.parse(Buffer.from(parts[1], 'base64url').toString()); } catch (e) { return { err: 'Kalit buzilgan' }; }
  if (!p || p.v !== 1) return { err: 'Kalit versiyasi notoʻgʻri' };
  if (p.m !== machineCode()) return { err: 'Bu kalit boshqa kompyuter uchun berilgan' };
  return { key: k, p };
}
const keyFile = () => path.join(dir, 'license.key');
function readKey() { try { return parseKey(fs.readFileSync(keyFile(), 'utf8')); } catch (e) { return null; } }

function status() {
  const now = Date.now(), machine = machineCode();
  const tr = trial(now);
  const clockBack = now + DAY < tr.s; // soat bir kundan koʻproq orqaga surilgan
  const k = readKey();
  if (k && k.p) {
    const p = k.p, base = { machine, name: p.n || '', maxPcs: +p.p || 0, expires: +p.e || 0, issued: +p.i || 0 };
    if (base.expires && (now > base.expires || clockBack)) return { state: 'expired', reason: clockBack ? 'clock' : 'key', ...base };
    return { state: 'licensed', ...base };
  }
  const endsAt = tr.t + TRIAL_DAYS * DAY;
  const base = { machine, maxPcs: TRIAL_PCS, endsAt, trialDays: TRIAL_DAYS };
  if (clockBack) return { state: 'expired', reason: 'clock', ...base };
  if (now >= endsAt) return { state: 'expired', reason: 'trial', ...base };
  return { state: 'trial', daysLeft: Math.ceil((endsAt - now) / DAY), ...base };
}

function activate(key) {
  const r = parseKey(key);
  if (r.err) return { ok: false, err: r.err };
  if (r.p.e && Date.now() > r.p.e) return { ok: false, err: 'Kalit muddati tugagan' };
  try { fs.writeFileSync(keyFile(), r.key); } catch (e) { return { ok: false, err: 'Kalitni saqlab boʻlmadi: ' + e.message }; }
  return { ok: true, status: status() };
}

module.exports = { init, status, activate, machineCode, parseKey, TRIAL_DAYS, TRIAL_PCS };
