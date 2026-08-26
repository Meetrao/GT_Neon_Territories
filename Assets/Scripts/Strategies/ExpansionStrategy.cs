using NeonTerritories.Players;
using NeonTerritories.Territories;

namespace NeonTerritories.Strategies
{
    public class ExpansionStrategy : Strategy
    {
        public string SourceTerritoryId { get; private set; }
        public string TargetTerritoryId { get; private set; }
        
        public int EnergyInvested => ResourceCost.Energy;
        public int CreditsInvested => ResourceCost.Credits;

        public ExpansionStrategy(string playerId, string sourceTerritoryId, string targetTerritoryId, int energy, int credits)
            : base(playerId, ActionType.Expand, 1)
        {
            SourceTerritoryId = sourceTerritoryId;
            TargetTerritoryId = targetTerritoryId;

            ResourceCost.Energy = energy;
            ResourceCost.Credits = credits;
        }

        public override bool Validate(Player player, TerritoryGraph map)
        {
            if (!base.Validate(player, map))
                return false;

            // Must own source territory
            if (!player.ControlledTerritoryIds.Contains(SourceTerritoryId))
                return false;

            // Target must be adjacent
            if (!map.AreNeighbors(SourceTerritoryId, TargetTerritoryId))
                return false;

            // Target must be neutral
            var target = map.GetTerritory(TargetTerritoryId);
            if (target == null || !string.IsNullOrEmpty(target.OwnerId))
                return false;

            // Minimum cost to expand
            if (EnergyInvested < 15) // Expanding to new territory requires base energy
                return false;

            return true;
        }

        public override string ToString()
        {
            return $"{base.ToString()} | Expand to {TargetTerritoryId} from {SourceTerritoryId} with {EnergyInvested} Energy";
        }
    }
}
