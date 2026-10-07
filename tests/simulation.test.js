const { PlayerResources, Player, Territory, NeonSimulation } = require('../simulation_runner');

describe('PlayerResources', () => {
    test('can afford resources', () => {
        const res1 = new PlayerResources(100, 50, 30, 20);
        const cost = new PlayerResources(50, 20, 10, 0);
        expect(res1.canAfford(cost)).toBe(true);
    });

    test('cannot afford resources', () => {
        const res1 = new PlayerResources(20, 50, 30, 20);
        const cost = new PlayerResources(50, 20, 10, 0);
        expect(res1.canAfford(cost)).toBe(false);
    });

    test('deducts resources correctly', () => {
        const res1 = new PlayerResources(100, 50, 30, 20);
        const cost = new PlayerResources(50, 20, 10, 5);
        res1.deduct(cost);
        expect(res1.credits).toBe(50);
        expect(res1.energy).toBe(30);
        expect(res1.tech).toBe(20);
        expect(res1.influence).toBe(15);
    });
});

describe('Territory', () => {
    test('initializes correctly', () => {
        const t = new Territory('T01', 'Test Node', 'Energy', 100, 100);
        expect(t.id).toBe('T01');
        expect(t.baseDefense).toBe(100);
        expect(t.investmentLevel).toBe(0);
    });

    test('invest increases defense and value', () => {
        const t = new Territory('T01', 'Test Node', 'Energy', 100, 100);
        t.invest();
        expect(t.investmentLevel).toBe(1);
        expect(t.currentDefense).toBe(110);
        expect(t.strategicValue).toBe(115);
    });
});

describe('NeonSimulation', () => {
    test('initializes with 4 players', () => {
        const sim = new NeonSimulation();
        expect(Object.keys(sim.players).length).toBe(4);
    });
});
