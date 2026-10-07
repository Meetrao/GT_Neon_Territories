const sqlite3 = require('sqlite3').verbose();
const path = require('path');

const dbPath = path.join(__dirname, 'gt_matches.db');
const db = new sqlite3.Database(dbPath);

db.serialize(() => {
    db.run(`CREATE TABLE IF NOT EXISTS matches (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        roomCode TEXT,
        winner TEXT,
        score INTEGER,
        timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
    )`);
});

function saveMatch(roomCode, winner, score) {
    db.run(`INSERT INTO matches (roomCode, winner, score) VALUES (?, ?, ?)`, [roomCode, winner, score], function(err) {
        if (err) {
            console.error('[DB ERROR] Failed to save match:', err.message);
        } else {
            console.log(`[DB SUCCESS] Saved match ${this.lastID} for room ${roomCode}`);
        }
    });
}

function getMatchHistory(callback) {
    db.all(`SELECT * FROM matches ORDER BY timestamp DESC LIMIT 50`, [], (err, rows) => {
        if (err) {
            console.error('[DB ERROR] Failed to get match history:', err.message);
            callback([]);
        } else {
            callback(rows);
        }
    });
}

module.exports = { saveMatch, getMatchHistory };
