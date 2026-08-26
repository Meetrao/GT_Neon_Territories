using NeonTerritories.Players;
using NeonTerritories.Territories;

namespace NeonTerritories.Strategies
{
    public class InvestmentStrategy : Strategy
    {
        public string TargetTerritoryId { get; private set; }

        public InvestmentStrategy(string playerId, string targetTerritoryId, TerritoryGraph map)
            : base(playerId, ActionType.Invest, 1)
        {
            TargetTerritoryId = targetTerritoryId;
            CalculateDynamicCost(map);
        }

        private void CalculateDynamicCost(TerritoryGraph map)
        {
            var territory = map.GetTerritory(TargetTerritoryId);
            if (territory != null)
            {
                int level = territory.InvestmentLevel;
                // Cost scales with current investment level
                ResourceCost.Credits = 50 + (25 * level);
                ResourceCost.Energy = 25 + (15 * level);
            }
            else
            {
                // Fallback default cost
                ResourceCost.Credits = 50;
                ResourceCost.Energy = 25;
            }
        }

        public override bool Validate(Player player, TerritoryGraph map)
        {
            // Re-calculate cost to ensure it matches the actual territory state
            CalculateDynamicCost(map);

            if (!base.Validate(player, map))
                return false;

            // Must own target territory to invest in it
            if (!player.ControlledTerritoryIds.Contains(TargetTerritoryId))
                return false;

            return true;
        }

        public override string ToString()
        {
            return $"{base.ToString()} | Invest in {TargetTerritoryId} (Cost: {ResourceCost.Credits} Credits, {ResourceCost.Energy} Energy)";
        }
    }
}
