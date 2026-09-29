// Klub Pult Server — admin kompyuter uchun Windows ilovasi (Electron).
// index.html shu papkadan ochiladi; klient kompyuterlar server.js orqali ulanadi.
const { app, BrowserWindow, Menu, shell, ipcMain, dialog } = require('electron');
const path = require('path');
const { execFile } = require('child_process');
const net = require('./server');
const lic = require('./license');

// SQLite baza — native modul yuklanmasa, dastur localStorage bilan ishlashda davom etadi.
let db = null, dbFile = '', dbErr = '';
try { db = require('./db'); } catch (e) { dbErr = e.message || String(e); }

// Operator parollari argon2id bilan hash qilinadi (native modul main jarayonda ishlaydi).
// Yuklanmasa, index.html WebCrypto PBKDF2 ga qaytadi — kirish buzilmaydi.
let argon2 = null;
try { argon2 = require('@node-rs/argon2'); } catch (e) { argon2 = null; }

if (!app.requestSingleInstanceLock()) app.quit();

let win, quitting = false;
function createWindow() {
  win = new BrowserWindow({
    width: 1400, height: 900, minWidth: 1024, minHeight: 640, show: false,
    title: 'Klub Pult', backgroundColor: '#1b1f24', autoHideMenuBar: true,
    webPreferences: { preload: path.join(__dirname, 'preload.js'), contextIsolation: true, nodeIntegration: false, spellcheck: false }
  });
  Menu.setApplicationMenu(null);
  win.loadFile(path.join(__dirname, 'index.html'));
  win.once('ready-to-show', () => { win.maximize(); win.show(); });
  win.webContents.setWindowOpenHandler(({ url }) => { if (/^https?:/.test(url)) shell.openExternal(url); return { action: 'deny' }; });
  win.webContents.on('before-input-event', (e, i) => {
    if (i.type === 'keyDown' && i.key === 'F11') win.setFullScreen(!win.isFullScreen());
    if (i.type === 'keyDown' && i.control && i.shift && i.key.toLowerCase() === 'i') win.webContents.toggleDevTools();
  });
  // server yopilsa klientlar oflayn rejimga oʻtadi — tasodifan yopib qoʻymaslik uchun soʻraymiz
  win.on('close', e => {
    if (quitting) return;
    const r = dialog.showMessageBoxSync(win, { type: 'question', buttons: ['Yopish', 'Bekor qilish'], defaultId: 1, cancelId: 1,
      title: 'Klub Pult', message: 'Klub Pult yopilsinmi?', detail: 'Server toʻxtaydi: klient kompyuterlar vaqtni oʻzi sanashda davom etadi, lekin yangi buyruqlar bormaydi.' });
    if (r !== 0) e.preventDefault(); else quitting = true;
  });
}

function toUi(ev) { if (win && !win.isDestroyed()) win.webContents.send('net:ev', ev); }

ipcMain.handle('net:start', (e, port) => {
  // litsenziya yoki sinov muddati tugagan boʻlsa server ochilmaydi
  if (lic.status().state === 'expired') { net.stop(); return { running: false, port: +port || 7777, err: 'Litsenziya: muddat tugagan — kalit kiriting', ips: [], clients: 0 }; }
  return net.start(port, toUi).then(i => { if (i.running) allowFirewall(i.port); return i; });
});
ipcMain.handle('net:info', () => net.info());
ipcMain.on('net:send', (e, id, msg) => net.send(id, msg));
ipcMain.on('net:close', (e, id) => net.close(id));
ipcMain.handle('net:wol', (e, mac) => net.wol(mac));
ipcMain.handle('app:version', () => app.getVersion());
ipcMain.on('lic:status', e => { try { e.returnValue = lic.status(); } catch (err) { e.returnValue = { state: 'expired', reason: 'error', machine: '', maxPcs: 0, err: String(err.message || err) }; } });
ipcMain.handle('lic:activate', (e, key) => lic.activate(key));
// har soatda tekshiramiz: muddat tugasa server toʻxtaydi va oyna xabardor qilinadi
setInterval(() => {
  let st; try { st = lic.status(); } catch (e) { return; }
  if (st.state === 'expired') net.stop();
  if (win && !win.isDestroyed()) win.webContents.send('lic:changed', st);
}, 3600e3).unref();
ipcMain.on('pw:ok', e => { e.returnValue = !!argon2; });
ipcMain.handle('pw:hash', (e, pw) => argon2.hash(String(pw)));
ipcMain.handle('pw:verify', async (e, hash, pw) => { try { return await argon2.verify(String(hash), String(pw)); } catch { return false; } });

// ---- SQLite baza (sinxron oʻqish preload uchun, yozish debounce) ----
let dbReady = false;
function ensureDb() {
  if (dbReady || !db) return dbReady;
  try { dbFile = db.init(app.getPath('userData')); dbReady = true; }
  catch (e) { dbErr = e.message || String(e); db = null; }
  return dbReady;
}
let saveTimer = null, pendingJson = null;
ipcMain.on('db:load', (e) => {
  if (!ensureDb()) { e.returnValue = { ok: false, error: dbErr }; return; }
  try { e.returnValue = { ok: true, file: dbFile, json: db.loadState() }; }
  catch (err) { e.returnValue = { ok: false, error: err.message || String(err) }; }
});
ipcMain.on('db:save', (e, json) => {
  if (!db) return;
  pendingJson = json;
  if (saveTimer) return;
  saveTimer = setTimeout(() => {
    saveTimer = null;
    const j = pendingJson; pendingJson = null;
    try { db.saveState(j); } catch (err) { /* keyingi saqlashda qayta urinadi */ }
  }, 400);
});
ipcMain.handle('db:arch.add', (e, rec) => { try { db && db.archiveAdd(rec); return true; } catch { return false; } });
ipcMain.handle('db:arch.list', () => { try { return db ? db.archiveList() : []; } catch { return []; } });
ipcMain.handle('db:arch.get', (e, id) => { try { return db ? db.archiveGet(id) : null; } catch { return null; } });
ipcMain.handle('db:arch.del', (e, id) => { try { db && db.archiveDel(id); return true; } catch { return false; } });
// dastur yopilishidan oldin kutayotgan yozuvni saqlaymiz
app.on('before-quit', () => { if (db && pendingJson != null) { try { db.saveState(pendingJson); } catch {} } });

// Windows Firewall: port uchun kiruvchi qoidani qoʻshishga urinish (administrator huquqi boʻlsa ishlaydi;
// boʻlmasa Windows oʻzi "Allow access" oynasini koʻrsatadi)
function allowFirewall(port) {
  if (process.platform !== 'win32') return;
  const name = `Klub Pult Server (TCP ${port})`;
  execFile('netsh', ['advfirewall', 'firewall', 'show', 'rule', `name=${name}`], err => {
    if (!err) return;
    execFile('netsh', ['advfirewall', 'firewall', 'add', 'rule', `name=${name}`, 'dir=in', 'action=allow', 'protocol=TCP', `localport=${port}`, 'profile=any'], () => {});
  });
}

app.on('second-instance', () => { if (win) { if (win.isMinimized()) win.restore(); win.focus(); } });
app.whenReady().then(() => { lic.init(app.getPath('userData')); createWindow(); });
app.on('before-quit', () => { quitting = true; net.stop(); });
app.on('window-all-closed', () => app.quit());
