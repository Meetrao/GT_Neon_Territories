/**
 * Neon Territories: Multi-Agent Game-Theoretic Framework Simulation Runner
 * Standalone JavaScript Prototype for Fast Verification & Research Logging
 */

// --- 1. DATA STRUCTURES & DEFINITIONS ---

const FactionType = {
    NOVA: 'NOVA (Economic)',
    VORTEX: 'VORTEX (Military)',
    PULSE: 'PULSE (Tech)',
    SYNTH: 'SYNTH (Diplomatic)'
};

const TerritoryType = {
    Energy: 'Energy',
    Technology: 'Technology',
    Credit: 'Credit',
    Defense: 'Defense',
    Core: 'Core'
};

const ActionType = {
    Attack: 'Attack',
    Defend: 'Defend',
    Expand: 'Expand',
    Invest: 'Invest',
    Spy: 'Spy',
    Trade: 'Trade'
};

class PlayerResources {
    constructor(credits = 100, energy = 50, tech = 20, influence = 20) {
        this.credits = credits;
        this.energy = energy;
        this.tech = tech;
        this.influence = influence;
    }

    add(other) {
        this.credits += other.credits;
        this.energy += other.energy;
        this.tech += other.tech;
        this.influence += other.influence;
    }

    deduct(cost) {
        this.credits -= cost.credits;
        this.energy -= cost.energy;
        this.tech -= cost.tech;
        this.influence -= cost.influence;
    }

    canAfford(cost) {
        return this.credits >= cost.credits &&
               this.energy >= cost.energy &&
               this.tech >= cost.tech &&
               this.influence >= cost.influence;
    }

    clone() {
        return new PlayerResources(this.credits, this.energy, this.tech, this.influence);
    }

    toString() {
        return `Cr: ${this.credits} | En: ${this.energy} | Te: ${this.tech} | Inf: ${this.influence}`;
    }
}

class Player {
    constructor(id, name, faction) {
        this.id = id;
        this.name = name;
        this.faction = faction;
        this.resources = new PlayerResources();
        this.controlledTerritories = [];
        this.strategicScore = 0;
        this.reputation = 50; // 0 to 100
        this.actionPoints = 3;

        // Apply faction starting bonuses
        if (faction === FactionType.NOVA) {
            this.resources.credits += 50;
            this.creditGainModifier = 1.25;
        } else {
            this.creditGainModifier = 1.0;
        }

        if (faction === FactionType.VORTEX) {
            this.resources.energy += 30;
            this.attackPowerModifier = 1.2;
        } else {
            this.attackPowerModifier = 1.0;
        }

        if (faction === FactionType.PULSE) {
            this.resources.tech += 20;
            this.techGainModifier = 1.25;
        } else {
            this.techGainModifier = 1.0;
        }

        if (faction === FactionType.SYNTH) {
            this.resources.influence += 30;
            this.spyCostModifier = 0.75;
            this.reputation = 70;
        } else {
            this.spyCostModifier = 1.0;
        }
    }

    toString() {
        return `${this.name} (${this.faction}) | Score: ${this.strategicScore} | Rep: ${this.reputation} | [${this.resources.toString()}]`;
    }
}

class Territory {
    constructor(id, name, type, baseDefense, strategicValue) {
        this.id = id;
        this.name = name;
        this.type = type;
        this.ownerId = null; // Neutral initially
        this.baseDefense = baseDefense;
        this.currentDefense = baseDefense;
        this.strategicValue = strategicValue;
        this.investmentLevel = 0;
        this.neighbors = [];
        this.generationRate = new PlayerResources(0, 0, 0, 0);

        this.initGenRate();
    }

    initGenRate() {
        switch (this.type) {
            case TerritoryType.Energy:
                this.generationRate.energy = 30;
                break;
            case TerritoryType.Technology:
                this.generationRate.tech = 20;
                break;
            case TerritoryType.Credit:
                this.generationRate.credits = 40;
                break;
            case TerritoryType.Defense:
                this.generationRate.influence = 10;
                break;
            case TerritoryType.Core:
                this.generationRate.credits = 15;
                this.generationRate.energy = 15;
                this.generationRate.tech = 10;
                this.generationRate.influence = 10;
                break;
        }
    }

    invest() {
        this.investmentLevel++;
        this.currentDefense += 10;
        this.strategicValue += 15;

        switch (this.type) {
            case TerritoryType.Energy: this.generationRate.energy += 10; break;
            case TerritoryType.Technology: this.generationRate.tech += 8; break;
            case TerritoryType.Credit: this.generationRate.credits += 15; break;
            case TerritoryType.Defense: this.currentDefense += 15; this.generationRate.influence += 5; break;
            case TerritoryType.Core:
                this.generationRate.credits += 5;
                this.generationRate.energy += 5;
                this.generationRate.tech += 3;
                this.generationRate.influence += 3;
                break;
        }
    }
}

// --- 2. GAME THEORY UTILITY & PREDICTOR ---

class UtilityCalculator {
    static calculateUtility(player, creditDelta, energyDelta, techDelta, scoreDelta, repDelta) {
        let wCr = 1.0, wEn = 1.0, wTe = 1.2, wSc = 2.0, wRp = 0.5;

        if (player.resources.credits < 30) wCr *= 1.5;
        if (player.resources.energy < 20) wEn *= 2.0;

        if (player.faction === FactionType.NOVA) wCr *= 1.3;
        if (player.faction === FactionType.VORTEX) wEn *= 1.3;

        return (creditDelta * wCr) + (energyDelta * wEn) + (techDelta * wTe) + (scoreDelta * wSc) + (repDelta * wRp);
    }
}

class StrategyPredictor {
    constructor() {
        this.history = {};
        this.correctPredictions = 0;
        this.totalPredictions = 0;
    }

    record(playerId, action) {
        if (!this.history[playerId]) {
            this.history[playerId] = [];
        }
        this.history[playerId].push(action);
    }

    predict(playerId) {
        const hist = this.history[playerId];
        if (!hist || hist.length === 0) {
            return ActionType.Invest; // Default assumption
        }

        // Count action frequencies
        const counts = {};
        hist.forEach(act => {
            counts[act] = (counts[act] || 0) + 1;
        });

        // Get action with highest frequency
        let best = ActionType.Invest;
        let max = -1;
        for (const act in counts) {
            if (counts[act] > max) {
                max = counts[act];
                best = act;
            }
        }
        return best;
    }

    validatePrediction(playerId, actualAction) {
        const pred = this.predict(playerId);
        this.totalPredictions++;
        if (pred === actualAction) {
            this.correctPredictions++;
        }
    }

    getAccuracy() {
        return this.totalPredictions === 0 ? 0 : Math.round((this.correctPredictions / this.totalPredictions) * 100);
    }
}

// --- 3. DYNAMIC PAYOFF MATRIX CALCULATOR ---

class PayoffCalculator {
    static generateMatrix(playerA, playerB, territory) {
        // Simple 2x2 matrix simulation: Attack vs Defend, Invest etc.
        const actionsA = ["Attack", "Passive"];
        const actionsB = ["Defend", "Invest"];
        
        const payoffs = [
            [null, null],
            [null, null]
        ];

        for (let i = 0; i < 2; i++) {
            for (let j = 0; j < 2; j++) {
                const actA = actionsA[i];
                const actB = actionsB[j];
                let uA = 0, uB = 0;

                if (actA === "Attack" && actB === "Defend") {
                    // Hawk vs Hawk
                    const powerA = 30 * playerA.attackPowerModifier;
                    const powerB = territory.currentDefense + 24;
                    if (powerA > powerB) {
                        uA = UtilityCalculator.calculateUtility(playerA, 0, -30, 0, territory.strategicValue + 50, -10);
                        uB = UtilityCalculator.calculateUtility(playerB, 0, -20, 0, -territory.strategicValue, 0);
                    } else {
                        uA = UtilityCalculator.calculateUtility(playerA, 0, -30, 0, -20, 0);
                        uB = UtilityCalculator.calculateUtility(playerB, 0, -20, 0, 30, 0);
                    }
                } else if (actA === "Attack" && actB === "Invest") {
                    // Hawk vs Dove
                    uA = UtilityCalculator.calculateUtility(playerA, 0, -30, 0, territory.strategicValue + 50, -10);
                    uB = UtilityCalculator.calculateUtility(playerB, -50, -25, 0, -territory.strategicValue, 0);
                } else if (actA === "Passive" && actB === "Defend") {
                    uA = UtilityCalculator.calculateUtility(playerA, 0, 0, 0, 0, 0);
                    uB = UtilityCalculator.calculateUtility(playerB, 0, -20, 0, 0, 0);
                } else if (actA === "Passive" && actB === "Invest") {
                    uA = UtilityCalculator.calculateUtility(playerA, 0, 0, 0, 0, 0);
                    uB = UtilityCalculator.calculateUtility(playerB, -50, -25, 0, 20, 0);
                }

                payoffs[i][j] = { a: Math.round(uA), b: Math.round(uB) };
            }
        }

        return {
            actionsA,
            actionsB,
            payoffs,
            findNash() {
                const equilibria = [];
                for (let i = 0; i < 2; i++) {
                    for (let j = 0; j < 2; j++) {
                        const cell = payoffs[i][j];
                        
                        // Is A max in column j?
                        const aBest = cell.a >= payoffs[1 - i][j].a;
                        // Is B max in row i?
                        const bBest = cell.b >= payoffs[i][1 - j].b;

                        if (aBest && bBest) {
                            equilibria.push({ row: i, col: j, actA: actionsA[i], actB: actionsB[j] });
                        }
                    }
                }
                return equilibria;
            }
        };
    }
}

// --- 4. GAME SIMULATOR CORE ---

class NeonSimulation {
    constructor() {
        this.players = {
            P1: new Player('P1', 'Nova Corp', FactionType.NOVA),
            P2: new Player('P2', 'Vortex Syndicate', FactionType.VORTEX),
            P3: new Player('P3', 'Pulse Network', FactionType.PULSE),
            P4: new Player('P4', 'Synth Nexus', FactionType.SYNTH)
        };

        this.territories = {
            T01: new Territory('T01', 'Energy Grid North', TerritoryType.Energy, 20, 60),
            T02: new Territory('T02', 'Silicon Valley East', TerritoryType.Technology, 25, 70),
            T03: new Territory('T03', 'Financial Sector North', TerritoryType.Credit, 15, 65),
            T04: new Territory('T04', 'Defense Citadel West', TerritoryType.Defense, 60, 80),
            T05: new Territory('T05', 'Industrial Belt', TerritoryType.Energy, 30, 75),
            T06: new Territory('T06', 'Commercial Hub East', TerritoryType.Credit, 20, 70),
            T07: new Territory('T07', 'Research Lab South', TerritoryType.Technology, 35, 85),
            T08: new Territory('T08', 'Central Core', TerritoryType.Core, 80, 150),
            T09: new Territory('T09', 'Harbor District', TerritoryType.Credit, 20, 65),
            T10: new Territory('T10', 'Tech Plaza West', TerritoryType.Technology, 25, 70),
            T11: new Territory('T11', 'Power Grid West', TerritoryType.Energy, 20, 60),
            T12: new Territory('T12', 'Defense Gate East', TerritoryType.Defense, 55, 75),
            T13: new Territory('T13', 'Neon Boulevard', TerritoryType.Credit, 18, 68),
            T14: new Territory('T14', 'Sub-Core Sub-Zero', TerritoryType.Core, 70, 130)
        };

        this.predictor = new StrategyPredictor();
        this.round = 1;
        this.maxRounds = 15;
        this.alliances = []; // Array of { pA, pB, duration }

        this.setupMapConnections();
        this.distributeStartingTerritories();
    }

    setupMapConnections() {
        const connect = (a, b) => {
            this.territories[a].neighbors.push(b);
            this.territories[b].neighbors.push(a);
        };
        connect('T01', 'T02'); connect('T01', 'T03'); connect('T01', 'T04');
        connect('T02', 'T05'); connect('T02', 'T08'); connect('T03', 'T06');
        connect('T03', 'T08'); connect('T04', 'T05'); connect('T04', 'T11');
        connect('T05', 'T07'); connect('T05', 'T08'); connect('T06', 'T07');
        connect('T06', 'T08'); connect('T06', 'T09'); connect('T07', 'T10');
        connect('T09', 'T12'); connect('T09', 'T13'); connect('T10', 'T11');
        connect('T10', 'T14'); connect('T11', 'T14'); connect('T08', 'T13');
        connect('T08', 'T14'); connect('T12', 'T13'); connect('T12', 'T14');
        connect('T13', 'T14');
    }

    distributeStartingTerritories() {
        const assign = (tId, pId) => {
            this.territories[tId].ownerId = pId;
            this.players[pId].controlledTerritories.push(tId);
        };
        assign('T01', 'P1'); assign('T03', 'P1');
        assign('T04', 'P2'); assign('T11', 'P2');
        assign('T02', 'P3'); assign('T10', 'P3');
        assign('T09', 'P4'); assign('T12', 'P4');
    }

    areAllied(pA, pB) {
        return this.alliances.some(al => (al.pA === pA && al.pB === pB) || (al.pA === pB && al.pB === pA));
    }

    runRound() {
        console.log(`\n\x1b[35m=== ROUND ${this.round} / ${this.maxRounds} ===\x1b[0m`);

        // 1. Generation
        for (const pId in this.players) {
            const player = this.players[pId];
            player.actionPoints = 3;
            const gains = new PlayerResources(0, 0, 0, 0);

            player.controlledTerritories.forEach(tId => {
                gains.add(this.territories[tId].generationRate);
            });

            // Faction gains
            gains.credits = Math.round(gains.credits * player.creditGainModifier);
            gains.tech = Math.round(gains.tech * player.techGainModifier);

            player.resources.add(gains);
            console.log(`- ${player.name} generated: +Cr: ${gains.credits}, +En: ${gains.energy}, +Te: ${gains.tech}`);
        }

        // 2. Automated Diplomatic Agreements
        for (const pIdA in this.players) {
            for (const pIdB in this.players) {
                if (pIdA >= pIdB) continue;
                if (this.areAllied(pIdA, pIdB)) continue;

                const repA = this.players[pIdA].reputation;
                const repB = this.players[pIdB].reputation;

                if (repA >= 45 && repB >= 45) {
                    this.alliances.push({ pA: pIdA, pB: pIdB, duration: 3 });
                    console.log(`\x1b[36m[Alliance] ${this.players[pIdA].name} and ${this.players[pIdB].name} formed a Non-Aggression Pact (3 Rounds)\x1b[0m`);
                }
            }
        }

        // 3. AI Decisions (Simulated)
        const roundActions = [];
        for (const pId in this.players) {
            const player = this.players[pId];
            const action = this.determineAIAction(player);
            roundActions.push(action);

            // Record & Predict validation
            this.predictor.validatePrediction(pId, action.type);
            this.predictor.record(pId, action.type);
        }

        // 4. Resolve Actions
        this.resolveRoundActions(roundActions);

        // 5. Cleanup
        this.alliances.forEach(al => al.duration--);
        this.alliances = this.alliances.filter(al => al.duration > 0);

        for (const pId in this.players) {
            const player = this.players[pId];
            // Reputation decay
            player.reputation = Math.round(player.reputation * 0.95 + 50 * 0.05);
            
            // Recalculate score
            let score = 0;
            player.controlledTerritories.forEach(tId => {
                score += this.territories[tId].strategicValue;
            });
            score += Math.round(player.resources.credits * 0.1);
            score += Math.round(player.resources.energy * 0.1);
            score += Math.round(player.resources.tech * 0.2);
            score += Math.round(player.resources.influence * 0.2);
            
            // Add alliance bonus
            const alliesCount = this.alliances.filter(al => al.pA === pId || al.pB === pId).length;
            score += alliesCount * 30;

            player.strategicScore = score;
        }

        // 6. Showcase central core matrix every 5 rounds
        if (this.round % 5 === 0) {
            this.showcaseGameTheoryMatrix();
        }

        this.round++;
    }

    determineAIAction(player) {
        // VORTEX attacks if it has energy, others invest or expand
        if (player.faction === FactionType.VORTEX && player.resources.energy >= 30) {
            // Find a target adjacent territory owned by someone else
            for (const myT of player.controlledTerritories) {
                for (const neighbor of this.territories[myT].neighbors) {
                    const target = this.territories[neighbor];
                    if (target.ownerId && target.ownerId !== player.id && !this.areAllied(player.id, target.ownerId)) {
                        return { type: ActionType.Attack, playerId: player.id, source: myT, target: neighbor, cost: new PlayerResources(0, 30, 0, 0) };
                    }
                }
            }
        }

        // Generic Expand to neutral
        for (const myT of player.controlledTerritories) {
            for (const neighbor of this.territories[myT].neighbors) {
                const target = this.territories[neighbor];
                if (!target.ownerId && player.resources.energy >= 20) {
                    return { type: ActionType.Expand, playerId: player.id, source: myT, target: neighbor, cost: new PlayerResources(0, 20, 0, 0) };
                }
            }
        }

        // Generic Invest
        const myT = player.controlledTerritories[0];
        return { type: ActionType.Invest, playerId: player.id, target: myT, cost: new PlayerResources(50, 25, 0, 0) };
    }

    resolveRoundActions(actions) {
        actions.forEach(act => {
            const player = this.players[act.playerId];
            if (player.resources.canAfford(act.cost)) {
                player.resources.deduct(act.cost);

                if (act.type === ActionType.Invest) {
                    const territory = this.territories[act.target];
                    territory.invest();
                    console.log(`- ${player.name} Invested in [${territory.id}] ${territory.name}. Upgraded to Level ${territory.investmentLevel}`);
                } 
                else if (act.type === ActionType.Expand) {
                    const territory = this.territories[act.target];
                    if (!territory.ownerId) {
                        territory.ownerId = player.id;
                        player.controlledTerritories.push(territory.id);
                        console.log(`- ${player.name} successfully Expanded into neutral sector [${territory.id}] ${territory.name}`);
                    }
                } 
                else if (act.type === ActionType.Attack) {
                    const territory = this.territories[act.target];
                    const defender = this.players[territory.ownerId];
                    
                    const attackPower = 30 * player.attackPowerModifier;
                    const defensePower = territory.currentDefense;

                    console.log(`- \x1b[31m[COMBAT] ${player.name} Attacks ${defender.name} at [${territory.id}] (Atk Strength: ${attackPower} vs Def: ${defensePower})\x1b[0m`);

                    if (attackPower > defensePower) {
                        // Capture
                        defender.controlledTerritories = defender.controlledTerritories.filter(id => id !== territory.id);
                        territory.ownerId = player.id;
                        player.controlledTerritories.push(territory.id);
                        territory.currentDefense = Math.max(5, Math.round(territory.baseDefense * 0.4));
                        player.reputation = Math.max(0, player.reputation - 15);
                        console.log(`  => Victory! ${player.name} captured [${territory.id}] ${territory.name}`);
                    } else {
                        console.log(`  => Defeated! ${defender.name} held their ground.`);
                    }
                }
            }
        });
    }

    showcaseGameTheoryMatrix() {
        console.log(`\n\x1b[32m=== DYNAMIC PAYOFF MATRIX ANALYSIS (CENTRAL CORE T08) ===\x1b[0m`);
        const matrix = PayoffCalculator.generateMatrix(this.players.P2, this.players.P1, this.territories.T08);
        console.log(`Rows: ${this.players.P2.name} (VORTEX) | Columns: ${this.players.P1.name} (NOVA)`);
        
        console.log(`\t\t${matrix.actionsB[0]}\t\t${matrix.actionsB[1]}`);
        console.log(`${matrix.actionsA[0]}\t(${matrix.payoffs[0][0].a}, ${matrix.payoffs[0][0].b})\t\t(${matrix.payoffs[0][1].a}, ${matrix.payoffs[0][1].b})`);
        console.log(`${matrix.actionsA[1]}\t(${matrix.payoffs[1][0].a}, ${matrix.payoffs[1][0].b})\t\t(${matrix.payoffs[1][1].a}, ${matrix.payoffs[1][1].b})`);
        
        const nash = matrix.findNash();
        console.log(`Pure Strategy Nash Equilibria:`);
        nash.forEach(eq => {
            console.log(`  => Strategy Profile [${eq.actA}, ${eq.actB}] with utility payoff values: ${JSON.stringify(matrix.payoffs[eq.row][eq.col])}`);
        });
        console.log(`\x1b[32m=========================================================\x1b[0m\n`);
    }

    runAll() {
        console.log(`=================================================`);
        console.log(` NEON TERRITORIES - GAME-THEORETIC SIMULATOR v1.0`);
        console.log(`=================================================`);
        
        while (this.round <= this.maxRounds) {
            this.runRound();
        }

        console.log(`\n\x1b[33m=== SIMULATION COMPLETED STANDINGS ===\x1b[0m`);
        Object.values(this.players)
            .sort((a, b) => b.strategicScore - a.strategicScore)
            .forEach((p, idx) => {
                console.log(`${idx + 1}. ${p.name} (${p.faction}): Final Score: ${p.strategicScore} | Territories Owned: ${p.controlledTerritories.length}`);
            });
        
        console.log(`\nPrediction Accuracy of AI Predictor: \x1b[36m${this.predictor.getAccuracy()}%\x1b[0m`);
    }
}

module.exports = {
    PlayerResources,
    Player,
    Territory,
    UtilityCalculator,
    StrategyPredictor,
    PayoffCalculator,
    NeonSimulation
};

// Start simulation immediately if run via terminal
if (require.main === module) {
    const sim = new NeonSimulation();
    sim.runAll();
}
