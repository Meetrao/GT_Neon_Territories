using NeonTerritories.Players;
using NeonTerritories.Territories;

namespace NeonTerritories.Strategies
{
    public class AttackStrategy : Strategy
    {
        public string SourceTerritoryId { get; private set; }
        public string TargetTerritoryId { get; private set; }
        
        public int EnergyInvested => ResourceCost.Energy;
        public int CreditsInvested => ResourceCost.Credits;

        public AttackStrategy(string playerId, string sourceTerritoryId, string targetTerritoryId, int energy, int credits) 
            : base(playerId, ActionType.Attack, 1)
        {
            SourceTerritoryId = sourceTerritoryId;
            TargetTerritoryId = targetTerritoryId;
            
            ResourceCost.Energy = energy;
            ResourceCost.Credits = credits;
        }

        public override bool Validate(Player player, TerritoryGraph map)
        {
            // Standard validation (AP, resource checks)
            if (!base.Validate(player, map))
                return false;

            // Must own source territory
            if (!player.ControlledTerritoryIds.Contains(SourceTerritoryId))
                return false;

            // Target must be adjacent to source
            if (!map.AreNeighbors(SourceTerritoryId, TargetTerritoryId))
                return false;

            // Target cannot be owned by the same player
            var targetTerritory = map.GetTerritory(TargetTerritoryId);
            if (targetTerritory == null || targetTerritory.OwnerId == player.Id)
                return false;

            // Must invest at least some energy to attack
            if (EnergyInvested <= 0)
                return false;

            return true;
        }

        public override string ToString()
        {
            return $"{base.ToString()} | Attack {TargetTerritoryId} from {SourceTerritoryId} with {EnergyInvested} Energy and {CreditsInvested} Credits";
        }
    }
}
