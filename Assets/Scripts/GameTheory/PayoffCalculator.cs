using System;
using System.Collections.Generic;
using NeonTerritories.Players;
using NeonTerritories.Territories;
using NeonTerritories.Strategies;

namespace NeonTerritories.GameTheory
{
    public class PayoffCalculator
    {
        private TerritoryGraph _map;

        public PayoffCalculator(TerritoryGraph map)
        {
            _map = map;
        }

        /// <summary>
        /// Generates a dynamic payoff matrix between two players contesting a territory.
        /// Simulates combinations of Attack, Defend, and Passive choices.
        /// </summary>
        public PayoffMatrix GeneratePayoffMatrix(Player playerA, Player playerB, Territory territory)
        {
            // Rows: Player A actions. Cols: Player B actions.
            // Let's assume A is the potential attacker, and B is the owner of the territory.
            // If territory is neutral, they are competing to Expand.
            
            bool isBOnDefense = (territory.OwnerId == playerB.Id);
            bool isAOnDefense = (territory.OwnerId == playerA.Id);

            string[] actionsA;
            string[] actionsB;

            if (isBOnDefense)
            {
                actionsA = new string[] { "Attack (Aggressive)", "Spy/Passive" };
                actionsB = new string[] { "Defend (Fortify)", "Invest (Economy)" };
            }
            else if (isAOnDefense)
            {
                actionsA = new string[] { "Defend (Fortify)", "Invest (Economy)" };
                actionsB = new string[] { "Attack (Aggressive)", "Spy/Passive" };
            }
            else
            {
                // Contested expansion on neutral territory
                actionsA = new string[] { "Expand (Secure)", "Passive" };
                actionsB = new string[] { "Expand (Secure)", "Passive" };
            }

            PayoffMatrix matrix = new PayoffMatrix(playerA.Id, playerB.Id, actionsA, actionsB);

            // Populate the matrix by simulating each cell
            for (int i = 0; i < actionsA.Length; i++)
            {
                for (int j = 0; j < actionsB.Length; j++)
                {
                    var outcome = SimulateCellOutcome(playerA, playerB, territory, actionsA[i], actionsB[j]);
                    matrix.SetPayoff(i, j, outcome.Item1, outcome.Item2);
                }
            }

            return matrix;
        }

        private Tuple<float, float> SimulateCellOutcome(Player playerA, Player playerB, Territory territory, string actionA, string actionB)
        {
            // We simulate the utility change for both players.
            // Default baseline: no changes
            int creditA = 0, energyA = 0, techA = 0, scoreA = 0, repA = 0;
            int creditB = 0, energyB = 0, techB = 0, scoreB = 0, repB = 0;

            // Scenario 1: A Attacks B's Owned Territory
            if (territory.OwnerId == playerB.Id)
            {
                bool aAttacks = actionA.Contains("Attack");
                bool bDefends = actionB.Contains("Defend");

                if (aAttacks && bDefends)
                {
                    // Hawk vs Hawk (Attack vs Defend)
                    // Costs
                    energyA = -30;
                    energyB = -20;

                    // Combat strength comparison (simulated average check)
                    float attackPower = 30 * playerA.AttackPowerModifier;
                    float defensePower = territory.CurrentDefense + 20 * 1.2f;

                    if (attackPower > defensePower)
                    {
                        // A succeeds
                        scoreA += territory.StrategicValue + 50;
                        scoreB -= territory.StrategicValue;
                        repA -= 10;
                    }
                    else
                    {
                        // B defends successfully
                        scoreA -= 20;
                        scoreB += 30;
                    }
                }
                else if (aAttacks && !bDefends)
                {
                    // Hawk vs Dove (Attack vs Invest)
                    // B is caught investing while A attacks
                    energyA = -30;
                    creditB = -50; // Lost investment resources

                    float attackPower = 30 * playerA.AttackPowerModifier;
                    float defensePower = territory.CurrentDefense; // No active defense boost

                    if (attackPower > defensePower)
                    {
                        scoreA += territory.StrategicValue + 50;
                        scoreB -= territory.StrategicValue;
                        repA -= 10;
                    }
                    else
                    {
                        scoreA -= 20;
                        scoreB += 30;
                    }
                }
                else if (!aAttacks && bDefends)
                {
                    // Dove vs Hawk (Spy vs Defend)
                    // A is passive/spying, B wasted resources defending
                    techA = -10; // Spy cost
                    energyB = -20; // Wasted defense
                    techA += 5; // Gained intelligence
                }
                else if (!aAttacks && !bDefends)
                {
                    // Dove vs Dove (Spy vs Invest)
                    // Peaceful outcome. A gathers intel, B upgrades economy.
                    techA = -10;
                    techA += 5;

                    // B Invests successfully
                    creditB = -50;
                    energyB = -25;
                    scoreB += 20;
                }
            }
            // Scenario 2: Contested expansion on Neutral Territory
            else if (string.IsNullOrEmpty(territory.OwnerId))
            {
                bool aExpands = actionA.Contains("Expand");
                bool bExpands = actionB.Contains("Expand");

                if (aExpands && bExpands)
                {
                    // Both try to expand (Conflict / Chicken Game)
                    energyA = -20;
                    energyB = -20;
                    repA -= 5;
                    repB -= 5;

                    // Power comparison
                    float powerA = 20 * playerA.AttackPowerModifier;
                    float powerB = 20 * playerB.AttackPowerModifier;

                    if (powerA > powerB * 1.15f)
                    {
                        scoreA += territory.StrategicValue;
                    }
                    else if (powerB > powerA * 1.15f)
                    {
                        scoreB += territory.StrategicValue;
                    }
                    else
                    {
                        // Severe conflict crash, both fail
                        scoreA -= 10;
                        scoreB -= 10;
                    }
                }
                else if (aExpands && !bExpands)
                {
                    // A gets the territory uncontested
                    energyA = -20;
                    scoreA += territory.StrategicValue;
                }
                else if (!aExpands && bExpands)
                {
                    // B gets the territory uncontested
                    energyB = -20;
                    scoreB += territory.StrategicValue;
                }
                // If both passive, nothing changes
            }

            float utilityA = UtilityCalculator.CalculateUtility(playerA, creditA, energyA, techA, scoreA, repA);
            float utilityB = UtilityCalculator.CalculateUtility(playerB, creditB, energyB, techB, scoreB, repB);

            return new Tuple<float, float>(utilityA, utilityB);
        }
    }
}
