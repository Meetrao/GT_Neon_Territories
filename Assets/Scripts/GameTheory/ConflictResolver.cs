using System;
using System.Collections.Generic;
using System.Linq;
using NeonTerritories.Players;
using NeonTerritories.Territories;
using NeonTerritories.Strategies;

namespace NeonTerritories.GameTheory
{
    public class ConflictResolver
    {
        private TerritoryGraph _map;
        private Dictionary<string, Player> _players;

        public ConflictResolver(TerritoryGraph map, Dictionary<string, Player> players)
        {
            _map = map;
            _players = players;
        }

        /// <summary>
        /// Resolves all conflicts in a round.
        /// </summary>
        public void ResolveConflicts(List<Strategy> activeStrategies, Dictionary<string, List<string>> alliances)
        {
            // Group strategies by target territory
            Dictionary<string, List<Strategy>> territoryActions = new Dictionary<string, List<Strategy>>();

            foreach (var strategy in activeStrategies)
            {
                string targetId = GetTargetTerritoryId(strategy);
                if (string.IsNullOrEmpty(targetId)) continue;

                if (!territoryActions.ContainsKey(targetId))
                {
                    territoryActions[targetId] = new List<Strategy>();
                }
                territoryActions[targetId].Add(strategy);
            }

            // Resolve each territory's actions
            foreach (var territoryId in _map.Territories.Keys)
            {
                var territory = _map.GetTerritory(territoryId);
                var actions = territoryActions.ContainsKey(territoryId) ? territoryActions[territoryId] : new List<Strategy>();

                ResolveTerritoryConflict(territory, actions, alliances);
            }
        }

        private string GetTargetTerritoryId(Strategy strategy)
        {
            if (strategy is AttackStrategy attack) return attack.TargetTerritoryId;
            if (strategy is DefenseStrategy defense) return defense.TargetTerritoryId;
            if (strategy is ExpansionStrategy expansion) return expansion.TargetTerritoryId;
            if (strategy is InvestmentStrategy investment) return investment.TargetTerritoryId;
            if (strategy is SpyStrategy spy) return spy.TargetTerritoryId;
            return null;
        }

        private void ResolveTerritoryConflict(Territory territory, List<Strategy> actions, Dictionary<string, List<string>> alliances)
        {
            var attacks = actions.OfType<AttackStrategy>().ToList();
            var defenses = actions.OfType<DefenseStrategy>().ToList();
            var expansions = actions.OfType<ExpansionStrategy>().ToList();
            var investments = actions.OfType<InvestmentStrategy>().ToList();

            // 1. Resolve Investments first (if owned and not captured this round)
            if (investments.Count > 0 && attacks.Count == 0 && expansions.Count == 0)
            {
                foreach (var invest in investments)
                {
                    if (territory.OwnerId == invest.PlayerId)
                    {
                        var player = _players[invest.PlayerId];
                        territory.Invest(invest.ResourceCost);
                        player.StrategicScore += 20; // Strategic bonus for investing
                    }
                }
            }

            // 2. Resolve Expansion on Neutral Territories
            if (string.IsNullOrEmpty(territory.OwnerId))
            {
                if (expansions.Count > 0)
                {
                    ResolveExpansion(territory, expansions);
                }
                return;
            }

            // 3. Resolve Attacks on Occupied Territories
            if (attacks.Count > 0)
            {
                ResolveAttack(territory, attacks, defenses, alliances);
            }
            else if (defenses.Count > 0)
            {
                // Defender defended but no attacks occurred
                foreach (var def in defenses)
                {
                    var player = _players[def.PlayerId];
                    // Temporary defense boost
                    territory.CurrentDefense += (int)(def.EnergyInvested * 0.5f + def.CreditsInvested * 0.8f);
                }
            }
        }

        private void ResolveExpansion(Territory territory, List<ExpansionStrategy> expansions)
        {
            if (expansions.Count == 1)
            {
                // Uncontested expansion
                var exp = expansions[0];
                var player = _players[exp.PlayerId];
                
                territory.OwnerId = player.Id;
                player.AddTerritory(territory.Id);
                territory.CurrentDefense = territory.BaseDefense;
                player.StrategicScore += territory.StrategicValue; // Gain strategic value of territory
            }
            else
            {
                // Contested expansion (Game theoretic choice: Hawk-Dove / Competition)
                // Calculate expansion power for each
                var powerMap = new Dictionary<string, float>();
                foreach (var exp in expansions)
                {
                    var player = _players[exp.PlayerId];
                    float power = exp.EnergyInvested * player.AttackPowerModifier + exp.CreditsInvested * 1.5f;
                    powerMap[exp.PlayerId] = power;
                }

                // Find winner
                var sorted = powerMap.OrderByDescending(x => x.Value).ToList();
                string winnerId = sorted[0].Key;
                float winnerPower = sorted[0].Value;
                float runnerUpPower = sorted.Count > 1 ? sorted[1].Value : 0f;

                // Game theory check: If conflict is too severe, both waste resources and fail
                // If winner power is at least 15% higher than runner up, winner captures territory but suffers resource losses
                if (winnerPower >= runnerUpPower * 1.15f)
                {
                    var winner = _players[winnerId];
                    territory.OwnerId = winnerId;
                    winner.AddTerritory(territory.Id);
                    
                    // Reduced starting defense due to contest
                    int contestDamage = (int)(runnerUpPower * 0.3f);
                    territory.CurrentDefense = Math.Max(5, territory.BaseDefense - contestDamage);
                    winner.StrategicScore += territory.StrategicValue;

                    // Deduct reputation slightly for aggressive expansion contest
                    winner.Reputation = Math.Max(0, winner.Reputation - 2);
                }
                else
                {
                    // Severe conflict where no one wins (Chicken game crash)
                    // No one gets the territory, both lose resources, and minor strategic score penalty
                    foreach (var exp in expansions)
                    {
                        var player = _players[exp.PlayerId];
                        player.StrategicScore = Math.Max(0, player.StrategicScore - 10);
                        player.Reputation = Math.Max(0, player.Reputation - 5); // Hostile interaction
                    }
                }
            }
        }

        private void ResolveAttack(Territory territory, List<AttackStrategy> attacks, List<DefenseStrategy> defenses, Dictionary<string, List<string>> alliances)
        {
            string ownerId = territory.OwnerId;
            var owner = _players[ownerId];

            // 1. Calculate Defense Strength
            float defenseStrength = territory.CurrentDefense;
            
            // Add defense action contributions if owner defended
            var ownerDefense = defenses.FirstOrDefault(d => d.PlayerId == ownerId);
            if (ownerDefense != null)
            {
                defenseStrength += ownerDefense.EnergyInvested * 1.2f + ownerDefense.CreditsInvested * 1.8f;
            }

            // Check if owner's allies provide defensive support (if they chose to defend this territory too)
            var alliedDefenders = defenses.Where(d => d.PlayerId != ownerId).ToList();
            foreach (var alliedDef in alliedDefenders)
            {
                // Check if they have an active alliance agreement
                if (alliances.ContainsKey(ownerId) && alliances[ownerId].Contains(alliedDef.PlayerId))
                {
                    // Allied defense counts towards defense strength
                    defenseStrength += alliedDef.EnergyInvested * 0.8f + alliedDef.CreditsInvested * 1.2f;
                }
            }

            // 2. Calculate Attack Strength for each attacker/alliance
            var attackGroups = new Dictionary<string, float>(); // Key: PlayerId, Value: Power
            
            foreach (var atk in attacks)
            {
                var attacker = _players[atk.PlayerId];
                float power = atk.EnergyInvested * attacker.AttackPowerModifier + atk.CreditsInvested * 1.5f;
                attackGroups[atk.PlayerId] = power;
            }

            // Pool allied attacks
            var combinedAttackGroups = new Dictionary<string, float>();
            var processedAttackers = new HashSet<string>();

            foreach (var attackerId in attackGroups.Keys)
            {
                if (processedAttackers.Contains(attackerId)) continue;

                float groupPower = attackGroups[attackerId];
                List<string> groupMembers = new List<string> { attackerId };

                // Find allies also attacking this territory to combine power
                if (alliances.ContainsKey(attackerId))
                {
                    foreach (var allyId in alliances[attackerId])
                    {
                        if (attackGroups.ContainsKey(allyId) && allyId != ownerId)
                        {
                            groupPower += attackGroups[allyId];
                            groupMembers.Add(allyId);
                            processedAttackers.Add(allyId);
                        }
                    }
                }

                string groupLeader = attackerId; // Primary attacker is leader
                combinedAttackGroups[groupLeader] = groupPower;
                processedAttackers.Add(attackerId);
            }

            // 3. Compare Strengths
            var sortedAttacks = combinedAttackGroups.OrderByDescending(x => x.Value).ToList();
            string leadingAttackerId = sortedAttacks[0].Key;
            float totalAttackPower = sortedAttacks[0].Value;

            if (totalAttackPower > defenseStrength)
            {
                // Attack succeeds! Territory is captured.
                var leadingAttacker = _players[leadingAttackerId];
                
                // Remove from defender
                owner.RemoveTerritory(territory.Id);
                owner.StrategicScore = Math.Max(0, owner.StrategicScore - territory.StrategicValue);

                // Transfer ownership to leading attacker
                territory.OwnerId = leadingAttackerId;
                leadingAttacker.AddTerritory(territory.Id);

                // Set new defense level (damaged during capture)
                float ratio = defenseStrength / totalAttackPower; // 0.0 to 1.0
                int remainingDef = (int)(territory.BaseDefense * (1.0f - ratio));
                territory.CurrentDefense = Math.Max(5, remainingDef);

                // Strategic Score Updates
                leadingAttacker.StrategicScore += territory.StrategicValue + 50; // Conquest bonus
                
                // Penalty for betraying an alliance
                if (alliances.ContainsKey(ownerId) && alliances[ownerId].Contains(leadingAttackerId))
                {
                    leadingAttacker.Reputation = Math.Max(0, leadingAttacker.Reputation - 30); // Major trust drop
                    leadingAttacker.StrategicScore = Math.Max(0, leadingAttacker.StrategicScore - 100); // Betrayal score penalty
                }
                else
                {
                    // Normal aggression penalty to reputation
                    leadingAttacker.Reputation = Math.Max(0, leadingAttacker.Reputation - 10);
                }
            }
            else
            {
                // Defense succeeds! Territory is held.
                owner.StrategicScore += 30; // Successful defense score bonus

                // Defender's territory defense is degraded temporarily
                float ratio = totalAttackPower / defenseStrength;
                territory.CurrentDefense = Math.Max(5, (int)(territory.CurrentDefense * (1.0f - ratio * 0.5f)));

                // Aggressor penalty
                foreach (var atk in attacks)
                {
                    var attacker = _players[atk.PlayerId];
                    attacker.StrategicScore = Math.Max(0, attacker.StrategicScore - 20); // Penalty for failed assault
                }
            }
        }
    }
}
