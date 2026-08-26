using System;
using NeonTerritories.Players;

namespace NeonTerritories.GameTheory
{
    public class UtilityCalculator
    {
        /// <summary>
        /// Calculates the utility value of a hypothetical state change for a player.
        /// Incorporates faction biases and marginal utility of resources.
        /// </summary>
        public static float CalculateUtility(Player player, int creditDelta, int energyDelta, int techDelta, int scoreDelta, int reputationDelta)
        {
            // Weights determine how valuable each resource is to the player
            float wCredits = 1.0f;
            float wEnergy = 1.0f;
            float wTech = 1.2f; // Tech is strategically valuable for prediction
            float wScore = 2.0f; // Score is the winning objective
            float wRep = 0.5f;

            // Marginal utility: If a player is low on a resource, its value increases
            if (player.Resources.Credits < 30) wCredits *= 1.5f;
            if (player.Resources.Energy < 20) wEnergy *= 2.0f;
            if (player.Resources.Technology < 10) wTech *= 1.8f;

            // Faction preference modifiers
            switch (player.Faction)
            {
                case FactionType.NOVA:
                    wCredits *= 1.3f; // Prefers wealth
                    break;
                case FactionType.VORTEX:
                    wEnergy *= 1.3f; // Prefers power/military resources
                    break;
                case FactionType.PULSE:
                    wTech *= 1.4f; // Prefers innovation
                    break;
                case FactionType.SYNTH:
                    wRep *= 1.5f; // Prefers high reputation/diplomacy
                    break;
            }

            // Sum up weighted utilities
            float utility = (creditDelta * wCredits) +
                            (energyDelta * wEnergy) +
                            (techDelta * wTech) +
                            (scoreDelta * wScore) +
                            (reputationDelta * wRep);

            return utility;
        }
    }
}
