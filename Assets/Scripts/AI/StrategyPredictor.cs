using System;
using System.Collections.Generic;
using NeonTerritories.Strategies;

namespace NeonTerritories.AI
{
    public class StrategyPredictor
    {
        // Tracks total action counts for frequency calculation: PlayerId -> (ActionType -> Count)
        private Dictionary<string, Dictionary<ActionType, int>> _actionCounts;
        private Dictionary<string, int> _totalActionCounts;

        // Tracks Markov state transitions: PlayerId -> (FromAction -> (ToAction -> Count))
        private Dictionary<string, Dictionary<ActionType, Dictionary<ActionType, int>>> _transitions;
        private Dictionary<string, Dictionary<ActionType, int>> _transitionTotals;

        // Tracks last action played by player
        private Dictionary<string, ActionType> _lastActions;

        public StrategyPredictor()
        {
            _actionCounts = new Dictionary<string, Dictionary<ActionType, int>>();
            _totalActionCounts = new Dictionary<string, int>();
            _transitions = new Dictionary<string, Dictionary<ActionType, Dictionary<ActionType, int>>>();
            _transitionTotals = new Dictionary<string, Dictionary<ActionType, int>>();
            _lastActions = new Dictionary<string, ActionType>();
        }

        public void RegisterPlayer(string playerId)
        {
            if (!_actionCounts.ContainsKey(playerId))
            {
                _actionCounts[playerId] = new Dictionary<ActionType, int>();
                _totalActionCounts[playerId] = 0;
                
                // Initialize counts
                foreach (ActionType action in Enum.GetValues(typeof(ActionType)))
                {
                    _actionCounts[playerId][action] = 0;
                }

                _transitions[playerId] = new Dictionary<ActionType, Dictionary<ActionType, int>>();
                _transitionTotals[playerId] = new Dictionary<ActionType, int>();

                foreach (ActionType fromAct in Enum.GetValues(typeof(ActionType)))
                {
                    _transitions[playerId][fromAct] = new Dictionary<ActionType, int>();
                    _transitionTotals[playerId][fromAct] = 0;
                    foreach (ActionType toAct in Enum.GetValues(typeof(ActionType)))
                    {
                        _transitions[playerId][fromAct][toAct] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Records an action played by a player to update their profile.
        /// </summary>
        public void RecordPlayerAction(string playerId, ActionType action)
        {
            RegisterPlayer(playerId);

            // Update simple frequency
            _actionCounts[playerId][action]++;
            _totalActionCounts[playerId]++;

            // Update Markov transitions if there is a previous action recorded
            if (_lastActions.TryGetValue(playerId, out ActionType lastAction))
            {
                _transitions[playerId][lastAction][action]++;
                _transitionTotals[playerId][lastAction]++;
            }

            // Save last action
            _lastActions[playerId] = action;
        }

        /// <summary>
        /// Returns the historical frequency distribution of actions for a player.
        /// </summary>
        public Dictionary<ActionType, float> GetActionProbabilities(string playerId)
        {
            var probs = new Dictionary<ActionType, float>();
            
            RegisterPlayer(playerId);
            int total = _totalActionCounts[playerId];

            foreach (ActionType action in Enum.GetValues(typeof(ActionType)))
            {
                if (total == 0)
                {
                    // Default uniform distribution
                    probs[action] = 1.0f / Enum.GetNames(typeof(ActionType)).Length;
                }
                else
                {
                    probs[action] = (float)_actionCounts[playerId][action] / total;
                }
            }

            return probs;
        }

        /// <summary>
        /// Predicts the probability of the next action given the player's history.
        /// Uses Markov model if transitions exist, otherwise falls back to simple frequency.
        /// </summary>
        public Dictionary<ActionType, float> PredictNextActionProbabilities(string playerId)
        {
            RegisterPlayer(playerId);

            if (_lastActions.TryGetValue(playerId, out ActionType lastAction))
            {
                int transitionTotal = _transitionTotals[playerId][lastAction];
                if (transitionTotal > 3) // Need a small sample size before using transition probabilities
                {
                    var probs = new Dictionary<ActionType, float>();
                    foreach (ActionType action in Enum.GetValues(typeof(ActionType)))
                    {
                        probs[action] = (float)_transitions[playerId][lastAction][action] / transitionTotal;
                    }
                    return probs;
                }
            }

            // Fallback to frequency predictor
            return GetActionProbabilities(playerId);
        }

        /// <summary>
        /// Predicts the single most likely next action for a player.
        /// </summary>
        public ActionType PredictNextAction(string playerId)
        {
            var probs = PredictNextActionProbabilities(playerId);
            ActionType bestAction = ActionType.Invest;
            float maxProb = -1.0f;

            foreach (var kvp in probs)
            {
                if (kvp.Value > maxProb)
                {
                    maxProb = kvp.Value;
                    bestAction = kvp.Key;
                }
            }

            return bestAction;
        }
    }
}
