// index.html ga faqat shu kichik API ochiladi — Node.js toʻgʻridan-toʻgʻri berilmaydi.
const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('kpNet', {
  start: port => ipcRenderer.invoke('net:start', port),
  info: () => ipcRenderer.invoke('net:info'),
  send: (id, msg) => ipcRenderer.send('net:send', id, msg),
  close: id => ipcRenderer.send('net:close', id),
  wol: mac => ipcRenderer.invoke('net:wol', mac),
  on: fn => ipcRenderer.on('net:ev', (e, ev) => fn(ev)),
  version: () => ipcRenderer.invoke('app:version')
});

// SQLite baza: holatni sinxron oʻqiymiz (sahifa kodidan oldin), yozish debounce bilan.
const dbInfo = ipcRenderer.sendSync('db:load'); // { ok, file, json } yoki { ok:false }
contextBridge.exposeInMainWorld('kpDbInfo', { ok: !!dbInfo.ok, file: dbInfo.file || '' });
contextBridge.exposeInMainWorld('kpInitState', dbInfo.ok ? (dbInfo.json ?? null) : null);
if (dbInfo.ok) contextBridge.exposeInMainWorld('kpDb', {
  save: json => ipcRenderer.send('db:save', json),
  archiveAdd: rec => ipcRenderer.invoke('db:arch.add', rec),
  archiveList: () => ipcRenderer.invoke('db:arch.list'),
  archiveGet: id => ipcRenderer.invoke('db:arch.get', id),
  archiveDel: id => ipcRenderer.invoke('db:arch.del', id)
});
