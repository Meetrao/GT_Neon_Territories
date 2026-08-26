using System;

namespace NeonTerritories.Players
{
    [Serializable]
    public class PlayerResources
    {
        public int Credits;
        public int Energy;
        public int Technology;
        public int Influence;

        public PlayerResources(int credits = 0, int energy = 0, int technology = 0, int influence = 0)
        {
            Credits = credits;
            Energy = energy;
            Technology = technology;
            Influence = influence;
        }

        /// <summary>
        /// Adds another set of resources to this one.
        /// </summary>
        public void Add(PlayerResources other)
        {
            if (other == null) return;
            Credits += other.Credits;
            Energy += other.Energy;
            Technology += other.Technology;
            Influence += other.Influence;
        }

        /// <summary>
        /// Deducts resources. Returns true if successful. If preventNegative is true, 
        /// it will fail and return false if there are insufficient resources.
        /// </summary>
        public bool Deduct(PlayerResources cost, bool preventNegative = true)
        {
            if (cost == null) return true;

            if (preventNegative && !CanAfford(cost))
            {
                return false;
            }

            Credits -= cost.Credits;
            Energy -= cost.Energy;
            Technology -= cost.Technology;
            Influence -= cost.Influence;
            return true;
        }

        /// <summary>
        /// Checks if the player has enough resources to afford the cost.
        /// </summary>
        public bool CanAfford(PlayerResources cost)
        {
            if (cost == null) return true;
            return Credits >= cost.Credits &&
                   Energy >= cost.Energy &&
                   Technology >= cost.Technology &&
                   Influence >= cost.Influence;
        }

        /// <summary>
        /// Clone resource state.
        /// </summary>
        public PlayerResources Clone()
        {
            return new PlayerResources(Credits, Energy, Technology, Influence);
        }

        public override string ToString()
        {
            return $"Credits: {Credits}, Energy: {Energy}, Tech: {Technology}, Influence: {Influence}";
        }
    }
}
