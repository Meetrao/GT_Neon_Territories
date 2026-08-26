const { WebSocketServer } = require('ws');
const http = require('http');

const PORT = process.env.PORT || 8080;
const server = http.createServer((req, res) => {
    res.writeHead(200, { 'Content-Type': 'text/plain' });
    res.end('Neon Territories Matchmaking Server is Online.');
});

const wss = new WebSocketServer({ server });

// Active match rooms: roomCode -> { hostId, players: [ { id, ws, faction, state } ] }
const rooms = {};

// Helper to generate a 4-digit code
function generateRoomCode() {
    let code;
    do {
        code = Math.random().toString(36).substring(2, 6).toUpperCase();
    } while (rooms[code]);
    return code;
}

wss.on('connection', (ws) => {
    let currentRoom = null;
    let playerId = Math.random().toString(36).substring(2, 9);
    let playerFaction = null;

    ws.on('message', (message) => {
        try {
            const data = JSON.parse(message);

            switch (data.type) {
                case 'host': {
                    const code = generateRoomCode();
                    playerFaction = data.faction || 'NOVA';
                    
                    rooms[code] = {
                        hostId: playerId,
                        players: [{
                            id: playerId,
                            ws,
                            faction: playerFaction,
                            state: { x: 400, y: 100, score: 0 }
                        }]
                    };

                    currentRoom = code;
                    ws.send(JSON.stringify({
                        type: 'hosted',
                        roomCode: code,
                        playerId,
                        faction: playerFaction
                    }));
                    console.log(`[ROOM] Created Room ${code} by Host ${playerId}`);
                    break;
                }

                case 'join': {
                    const code = data.roomCode ? data.roomCode.toUpperCase() : '';
                    const room = rooms[code];

                    if (!room) {
                        ws.send(JSON.stringify({ type: 'error', message: 'Room code not found.' }));
                        return;
                    }

                    if (room.players.length >= 4) {
                        ws.send(JSON.stringify({ type: 'error', message: 'Room is full (max 4 players).' }));
                        return;
                    }

                    // Assign an available faction
                    const usedFactions = room.players.map(p => p.faction);
                    const availableFactions = ['NOVA', 'VORTEX', 'PULSE', 'SYNTH'].filter(f => !usedFactions.includes(f));
                    playerFaction = availableFactions[0] || 'SYNTH';

                    const newPlayer = {
                        id: playerId,
                        ws,
                        faction: playerFaction,
                        state: { x: 400, y: 100, score: 0 }
                    };

                    room.players.push(newPlayer);
                    currentRoom = code;

                    // Acknowledge join to joining player
                    ws.send(JSON.stringify({
                        type: 'joined',
                        roomCode: code,
                        playerId,
                        faction: playerFaction,
                        playersList: room.players.map(p => ({ id: p.id, faction: p.faction }))
                    }));

                    // Notify others in room
                    broadcastToRoom(code, {
                        type: 'player_joined',
                        id: playerId,
                        faction: playerFaction,
                        playersList: room.players.map(p => ({ id: p.id, faction: p.faction }))
                    }, playerId);

                    console.log(`[ROOM] Player ${playerId} (${playerFaction}) joined Room ${code}`);
                    break;
                }

                case 'start': {
                    if (currentRoom && rooms[currentRoom].hostId === playerId) {
                        broadcastToRoom(currentRoom, { type: 'started' });
                        console.log(`[GAME] Room ${currentRoom} started by Host`);
                    }
                    break;
                }

                case 'update_state': {
                    if (currentRoom && rooms[currentRoom]) {
                        // Relay state updates (positions, shooting, actions)
                        broadcastToRoom(currentRoom, {
                            type: 'player_state',
                            id: playerId,
                            state: data.state
                        }, playerId);
                    }
                    break;
                }

                case 'shoot': {
                    if (currentRoom && rooms[currentRoom]) {
                        broadcastToRoom(currentRoom, {
                            type: 'bullet_fired',
                            id: playerId,
                            bullet: data.bullet
                        }, playerId);
                    }
                    break;
                }

                case 'capture': {
                    if (currentRoom && rooms[currentRoom]) {
                        broadcastToRoom(currentRoom, {
                            type: 'sector_captured',
                            nodeId: data.nodeId,
                            owner: data.owner
                        });
                    }
                    break;
                }

                case 'creeps': {
                    // Host periodically syncs creep coordinates and game timer
                    if (currentRoom && rooms[currentRoom] && rooms[currentRoom].hostId === playerId) {
                        broadcastToRoom(currentRoom, {
                            type: 'creeps_sync',
                            creeps: data.creeps,
                            timeRemaining: data.timeRemaining
                        }, playerId);
                    }
                    break;
                }
            }
        } catch (e) {
            console.error('[WS ERROR] Failed to parse message:', e.message);
        }
    });

    ws.on('close', () => {
        if (currentRoom && rooms[currentRoom]) {
            const room = rooms[currentRoom];
            room.players = room.players.filter(p => p.id !== playerId);

            console.log(`[ROOM] Player ${playerId} left Room ${currentRoom}`);

            if (room.players.length === 0) {
                delete rooms[currentRoom];
                console.log(`[ROOM] Room ${currentRoom} closed (no active connections)`);
            } else {
                // If Host disconnected, migrate host
                if (room.hostId === playerId) {
                    room.hostId = room.players[0].id;
                    broadcastToRoom(currentRoom, {
                        type: 'host_migrated',
                        newHostId: room.hostId
                    });
                    console.log(`[ROOM] Host migrated to player ${room.hostId} in Room ${currentRoom}`);
                }

                broadcastToRoom(currentRoom, {
                    type: 'player_left',
                    id: playerId,
                    playersList: room.players.map(p => ({ id: p.id, faction: p.faction }))
                });
            }
        }
    });
});

function broadcastToRoom(roomCode, messageObj, excludePlayerId = null) {
    const room = rooms[roomCode];
    if (!room) return;

    const payload = JSON.stringify(messageObj);
    room.players.forEach(p => {
        if (p.id !== excludePlayerId && p.ws.readyState === 1) {
            p.ws.send(payload);
        }
    });
}

server.listen(PORT, () => {
    console.log(`[SERVER] Matchmaker listening on http://localhost:${PORT}`);
});
