using System;
using System.Collections.Generic;

namespace NeonTerritories.GameTheory
{
    public class PayoffMatrix
    {
        public string PlayerAId { get; private set; }
        public string PlayerBId { get; private set; }
        public string[] ActionsA { get; private set; }
        public string[] ActionsB { get; private set; }
        public Tuple<float, float>[,] Payoffs { get; private set; } // [A_action_index, B_action_index] -> (PayoffA, PayoffB)

        public PayoffMatrix(string playerAId, string playerBId, string[] actionsA, string[] actionsB)
        {
            PlayerAId = playerAId;
            PlayerBId = playerBId;
            ActionsA = actionsA ?? new string[0];
            ActionsB = actionsB ?? new string[0];
            Payoffs = new Tuple<float, float>[ActionsA.Length, ActionsB.Length];
        }

        public void SetPayoff(int indexA, int indexB, float payoffA, float payoffB)
        {
            if (indexA >= 0 && indexA < ActionsA.Length && indexB >= 0 && indexB < ActionsB.Length)
            {
                Payoffs[indexA, indexB] = new Tuple<float, float>(payoffA, payoffB);
            }
        }

        public Tuple<float, float> GetPayoff(int indexA, int indexB)
        {
            if (indexA >= 0 && indexA < ActionsA.Length && indexB >= 0 && indexB < ActionsB.Length)
            {
                return Payoffs[indexA, indexB];
            }
            return new Tuple<float, float>(0, 0);
        }

        /// <summary>
        /// Finds all Pure Strategy Nash Equilibria (PSNE) in the payoff matrix.
        /// A cell (i, j) is a Nash Equilibrium if:
        /// - Player A cannot benefit by unilaterally changing their action (A's payoff is max in column j)
        /// - Player B cannot benefit by unilaterally changing their action (B's payoff is max in row i)
        /// </summary>
        public List<Tuple<int, int>> FindPureNashEquilibria()
        {
            List<Tuple<int, int>> equilibria = new List<Tuple<int, int>>();
            int rows = ActionsA.Length;
            int cols = ActionsB.Length;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (Payoffs[i, j] == null) continue;

                    float currentPayoffA = Payoffs[i, j].Item1;
                    float currentPayoffB = Payoffs[i, j].Item2;

                    // 1. Check if i is Player A's best response to j (max in column j)
                    bool isABestResponse = true;
                    for (int k = 0; k < rows; k++)
                    {
                        if (Payoffs[k, j] != null && Payoffs[k, j].Item1 > currentPayoffA)
                        {
                            isABestResponse = false;
                            break;
                        }
                    }

                    // 2. Check if j is Player B's best response to i (max in row i)
                    bool isBBestResponse = true;
                    for (int k = 0; k < cols; k++)
                    {
                        if (Payoffs[i, k] != null && Payoffs[i, k].Item2 > currentPayoffB)
                        {
                            isBBestResponse = false;
                            break;
                        }
                    }

                    // If both are best responses, it is a Nash Equilibrium
                    if (isABestResponse && isBBestResponse)
                    {
                        equilibria.Add(new Tuple<int, int>(i, j));
                    }
                }
            }

            return equilibria;
        }

        public string PrintMatrix()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Payoff Matrix between {PlayerAId} (Rows) and {PlayerBId} (Cols):");
            
            // Header
            sb.Append("\t\t");
            foreach (var actB in ActionsB)
            {
                sb.Append($"{actB}\t\t");
            }
            sb.AppendLine();

            for (int i = 0; i < ActionsA.Length; i++)
            {
                sb.Append($"{ActionsA[i]}\t");
                for (int j = 0; j < ActionsB.Length; j++)
                {
                    var p = Payoffs[i, j];
                    string payoffStr = p != null ? $"({p.Item1:F1}, {p.Item2:F1})" : "( -, - )";
                    sb.Append($"{payoffStr}\t\t");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
