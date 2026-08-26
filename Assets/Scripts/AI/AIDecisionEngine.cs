using System;
using System.Collections.Generic;
using System.Linq;
using NeonTerritories.Players;
using NeonTerritories.Territories;
using NeonTerritories.Strategies;

namespace NeonTerritories.AI
{
    public class AIDecisionEngine
    {
        private Random _rng;

        public AIDecisionEngine()
        {
            _rng = new Random();
        }

        /// <summary>
        /// Generates a list of valid strategies for an AI player during their turn.
        /// Spends the player's Action Points (AP) based on faction behavior and map context.
        /// </summary>
        public List<Strategy> DetermineRoundActions(Player aiPlayer, TerritoryGraph map, List<Player> allPlayers, StrategyPredictor predictor, List<string> allies)
        {
            List<Strategy> selectedActions = new List<Strategy>();
            int iterations = 0; // Guard to prevent infinite loops

            // Clone AP and resources for local planning simulation
            int workingAP = aiPlayer.ActionPoints;
            PlayerResources workingResources = aiPlayer.Resources.Clone();

            while (workingAP > 0 && iterations < 10)
            {
                iterations++;
                Strategy action = null;

                // Faction-specific behavioral routing
                switch (aiPlayer.Faction)
                {
                    case FactionType.VORTEX:
                        action = PlanVortexAction(aiPlayer, map, allPlayers, predictor, workingResources, allies);
                        break;
                    case FactionType.NOVA:
                        action = PlanNovaAction(aiPlayer, map, allPlayers, predictor, workingResources, allies);
                        break;
                    case FactionType.PULSE:
                        action = PlanPulseAction(aiPlayer, map, allPlayers, predictor, workingResources, allies);
                        break;
                    case FactionType.SYNTH:
                        action = PlanSynthAction(aiPlayer, map, allPlayers, predictor, workingResources, allies);
                        break;
                }

                // If no faction specific action was viable, fallback to generic action
                if (action == null)
                {
                    action = PlanGenericAction(aiPlayer, map, workingResources);
                }

                if (action != null && action.Validate(aiPlayer, map))
                {
                    selectedActions.Add(action);
                    workingAP -= action.APCost;
                    workingResources.Deduct(action.ResourceCost);
                }
                else
                {
                    // No action possible due to resource constraints
                    break;
                }
            }

            return selectedActions;
        }

        private Strategy PlanVortexAction(Player ai, TerritoryGraph map, List<Player> players, StrategyPredictor predictor, PlayerResources res, List<string> allies)
        {
            // VORTEX: Highly aggressive military faction.
            // Looks for adjacent weak targets to Attack.
            
            foreach (var myTerritoryId in ai.ControlledTerritoryIds)
            {
                var myTerritory = map.GetTerritory(myTerritoryId);
                if (myTerritory == null) continue;

                foreach (var neighborId in myTerritory.NeighborIds)
                {
                    var neighbor = map.GetTerritory(neighborId);
                    if (neighbor == null || neighbor.OwnerId == ai.Id || allies.Contains(neighbor.OwnerId)) 
                        continue; // Skip owned/allied territories

                    // Attack check: If we have enough energy
                    int energyRequired = 25;
                    if (res.Energy >= energyRequired)
                    {
                        // Target found!
                        return new AttackStrategy(ai.Id, myTerritoryId, neighborId, energyRequired, 10);
                    }
                }
            }

            return null;
        }

        private Strategy PlanNovaAction(Player ai, TerritoryGraph map, List<Player> players, StrategyPredictor predictor, PlayerResources res, List<string> allies)
        {
            // NOVA: Wealth accumulation. Prioritizes investments in high-yield resource nodes.
            foreach (var myTerritoryId in ai.ControlledTerritoryIds)
            {
                var territory = map.GetTerritory(myTerritoryId);
                if (territory == null) continue;

                // Upgrade economic zones
                if (territory.Type == TerritoryType.Credit || territory.Type == TerritoryType.Core)
                {
                    int creditCost = 50 + (25 * territory.InvestmentLevel);
                    int energyCost = 25 + (15 * territory.InvestmentLevel);

                    if (res.Credits >= creditCost && res.Energy >= energyCost)
                    {
                        return new InvestmentStrategy(ai.Id, myTerritoryId, map);
                    }
                }
            }

            return null;
        }

        private Strategy PlanPulseAction(Player ai, TerritoryGraph map, List<Player> players, StrategyPredictor predictor, PlayerResources res, List<string> allies)
        {
            // PULSE: Technological advantage. Prioritizes research and spying on aggressive factions.
            var vortexPlayer = players.FirstOrDefault(p => p.Faction == FactionType.VORTEX);
            if (vortexPlayer != null && vortexPlayer.Id != ai.Id && res.Technology >= 15)
            {
                // Spy on the dangerous military player to predict their attacks
                return new SpyStrategy(ai.Id, vortexPlayer.Id, null, 10, ai);
            }

            // Otherwise, invest in Technology nodes
            foreach (var myTerritoryId in ai.ControlledTerritoryIds)
            {
                var territory = map.GetTerritory(myTerritoryId);
                if (territory != null && territory.Type == TerritoryType.Technology)
                {
                    int creditCost = 50 + (25 * territory.InvestmentLevel);
                    int energyCost = 25 + (15 * territory.InvestmentLevel);

                    if (res.Credits >= creditCost && res.Energy >= energyCost)
                    {
                        return new InvestmentStrategy(ai.Id, myTerritoryId, map);
                    }
                }
            }

            return null;
        }

        private Strategy PlanSynthAction(Player ai, TerritoryGraph map, List<Player> players, StrategyPredictor predictor, PlayerResources res, List<string> allies)
        {
            // SYNTH: Information and territorial expansion.
            // Prioritizes capturing adjacent neutral sectors.
            foreach (var myTerritoryId in ai.ControlledTerritoryIds)
            {
                var myTerritory = map.GetTerritory(myTerritoryId);
                if (myTerritory == null) continue;

                foreach (var neighborId in myTerritory.NeighborIds)
                {
                    var neighbor = map.GetTerritory(neighborId);
                    if (neighbor != null && string.IsNullOrEmpty(neighbor.OwnerId))
                    {
                        // Expand to neutral
                        if (res.Energy >= 20)
                        {
                            return new ExpansionStrategy(ai.Id, myTerritoryId, neighborId, 20, 0);
                        }
                    }
                }
            }

            return null;
        }

        private Strategy PlanGenericAction(Player ai, TerritoryGraph map, PlayerResources res)
        {
            // Fallback strategy: Expand if possible, else Invest, else Defend
            foreach (var myTerritoryId in ai.ControlledTerritoryIds)
            {
                var myTerritory = map.GetTerritory(myTerritoryId);
                if (myTerritory == null) continue;

                // 1. Try to Expand to neutral
                foreach (var neighborId in myTerritory.NeighborIds)
                {
                    var neighbor = map.GetTerritory(neighborId);
                    if (neighbor != null && string.IsNullOrEmpty(neighbor.OwnerId) && res.Energy >= 20)
                    {
                        return new ExpansionStrategy(ai.Id, myTerritoryId, neighborId, 20, 0);
                    }
                }

                // 2. Try to Invest
                int creditCost = 50 + (25 * myTerritory.InvestmentLevel);
                int energyCost = 25 + (15 * myTerritory.InvestmentLevel);
                if (res.Credits >= creditCost && res.Energy >= energyCost)
                {
                    return new InvestmentStrategy(ai.Id, myTerritoryId, map);
                }

                // 3. Try to Defend
                if (res.Energy >= 15)
                {
                    return new DefenseStrategy(ai.Id, myTerritoryId, 15, 0);
                }
            }

            return null;
        }
    }
}
