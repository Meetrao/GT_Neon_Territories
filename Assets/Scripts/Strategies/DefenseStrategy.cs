using NeonTerritories.Players;
using NeonTerritories.Territories;

namespace NeonTerritories.Strategies
{
    public class DefenseStrategy : Strategy
    {
        public string TargetTerritoryId { get; private set; }
        
        public int EnergyInvested => ResourceCost.Energy;
        public int CreditsInvested => ResourceCost.Credits;

        public DefenseStrategy(string playerId, string targetTerritoryId, int energy, int credits)
            : base(playerId, ActionType.Defend, 1)
        {
            TargetTerritoryId = targetTerritoryId;
            
            ResourceCost.Energy = energy;
            ResourceCost.Credits = credits;
        }

        public override bool Validate(Player player, TerritoryGraph map)
        {
            if (!base.Validate(player, map))
                return false;

            // Must own target territory to defend it
            if (!player.ControlledTerritoryIds.Contains(TargetTerritoryId))
                return false;

            // Needs some defense investment
            if (EnergyInvested <= 0 && CreditsInvested <= 0)
                return false;

            return true;
        }

        public override string ToString()
        {
            return $"{base.ToString()} | Defend {TargetTerritoryId} with {EnergyInvested} Energy and {CreditsInvested} Credits";
        }
    }
}
