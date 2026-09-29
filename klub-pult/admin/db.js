// Klub Pult — haqiqiy SQLite baza (admin kompyuterda %AppData%/klubpult.db).
// Butun holat (S) shu faylda saqlanadi: brauzer localStorage'idan koʻra ishonchli,
// tozalansa yoʻqolmaydi, zaxiralanadi. Arxiv nusxalar ham shu bazada.
// Native modul (better-sqlite3) yuklanmasa, main.js buni ushlaydi va dastur
// localStorage'ga qaytadi — hech narsa buzilmaydi.
const path = require('path');
const Database = require('better-sqlite3');

let db = null;

function init(dir) {
  const file = path.join(dir, 'klubpult.db');
  db = new Database(file);
  db.pragma('journal_mode = WAL');       // yozish tez va ishonchli
  db.pragma('synchronous = NORMAL');
  db.exec(`
    CREATE TABLE IF NOT EXISTS state (
      id INTEGER PRIMARY KEY CHECK (id = 1),
      json TEXT NOT NULL,
      updated_at INTEGER NOT NULL
    );
    CREATE TABLE IF NOT EXISTS archives (
      id TEXT PRIMARY KEY,
      at INTEGER NOT NULL,
      reason TEXT,
      user TEXT,
      size INTEGER,
      json TEXT NOT NULL
    );
  `);
  return file;
}

const loadState = () => {
  const row = db.prepare(`SELECT json FROM state WHERE id = 1`).get();
  return row ? row.json : null;
};

const saveStmt = () =>
  db.prepare(`INSERT INTO state (id, json, updated_at) VALUES (1, @json, @t)
              ON CONFLICT(id) DO UPDATE SET json = @json, updated_at = @t`);

function saveState(json) {
  saveStmt().run({ json: String(json), t: Date.now() });
}

function archiveAdd(rec) {
  db.prepare(
    `INSERT OR REPLACE INTO archives (id, at, reason, user, size, json)
     VALUES (@id, @at, @reason, @user, @size, @json)`
  ).run({
    id: String(rec.id), at: rec.at | 0, reason: rec.reason || '', user: rec.user || '',
    size: rec.size | 0, json: String(rec.data || '')
  });
  // eng koʻpi 40 ta arxiv: eskilarini oʻchiramiz
  db.prepare(
    `DELETE FROM archives WHERE id NOT IN (SELECT id FROM archives ORDER BY at DESC LIMIT 40)`
  ).run();
}

const archiveList = () =>
  db.prepare(`SELECT id, at, reason, user, size FROM archives ORDER BY at DESC`).all();

const archiveGet = (id) => {
  const r = db.prepare(`SELECT id, at, reason, user, size, json FROM archives WHERE id = ?`).get(String(id));
  return r ? { id: r.id, at: r.at, reason: r.reason, user: r.user, size: r.size, data: r.json } : null;
};

const archiveDel = (id) => { db.prepare(`DELETE FROM archives WHERE id = ?`).run(String(id)); };

module.exports = { init, loadState, saveState, archiveAdd, archiveList, archiveGet, archiveDel };
