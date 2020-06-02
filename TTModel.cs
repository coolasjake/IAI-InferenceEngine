using System;
using System.Collections.Generic;
using System.Text;

namespace InferenceEngine
{
    class TTModel : Model
    {
        private List<Rule> KB = new List<Rule>();
        private List<bool[]> truthTable = new List<bool[]>();
        private string[] tableKey;
        private int tableHeight;
        private int tableWidth;

        public TTModel(string knowledge)
        {
            Tell(knowledge);
        }

        public TTModel() {}

        public override void Tell(string knowledge)
        {
            //Split the text into prepositions and use them to generate rules
            string[] strRules = knowledge.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in strRules)
                KB.Add(new Rule(s));

            //Find each unique symbol in the given rules
            foreach (Rule r in KB)
            {
                foreach (string symbol in r.symbols)
                {
                    if (!uniqueSymbols.Contains(symbol))
                        uniqueSymbols.Add(symbol);
                }
            }

            //Use the list of unique symbols to generate a truth table
            tableKey = uniqueSymbols.ToArray();
            tableWidth = tableKey.Length;
            tableHeight = (int)Math.Pow(2, tableWidth);

            for (int j = 0; j < tableHeight; ++j)
            {
                truthTable.Add(new bool[tableWidth]);
            }

            int pattern = 1;
            for (int i = 0; i < tableWidth; ++i)
            {
                bool contents = true;
                for (int j = 0; j < tableHeight; ++j)
                {
                    if (j % pattern == 0)
                        contents = !contents;
                    truthTable[j][i] = contents;
                }
                pattern = pattern * 2;
            }

            //Remove impossible states from truth table
            for (int j = 0; j < truthTable.Count; ++j)
            {
                foreach (Rule r in KB)
                {
                    if (!r.StateIsPossible(tableKey, truthTable[j]))
                    {
                        truthTable.RemoveAt(j);
                        --j;
                        break;
                    }
                }
            }
        }

        public override void PrintRules()
        {
            foreach (Rule rule in KB)
                Console.Write(rule + ",  ");
            Console.WriteLine();
        }

        public override int Query(string symbol)
        {
            //If the truth table is empty return false.
            if (truthTable.Count == 0)
                return 0;

            //Find the symbol in the knowledge base
            int column = -1;
            for (int i = 0; i < tableWidth; ++i)
            {
                if (symbol == tableKey[i])
                {
                    column = i;
                    break;
                }
            }
            //If it doesn't already exist return false
            if (column == -1)
                return 0;

            int total = 0;
            //If the symbol is false in any possible state, return false
            for (int j = 0; j < truthTable.Count; ++j)
            {
                if (truthTable[j][column] == true)
                    ++total;
            }

            //Otherwise return the total.
            return total;
        }

        protected override int QueryUsingRule(Rule R)
        {
            //If the truth table is empty return false.
            if (truthTable.Count == 0)
                return 0;

            //For each remaining state, check if the query is possible (and provable) with it.
            //Increment the total if it is, and return 0 (query is not entailed) if false.
            int total = 0;
            foreach (bool[] state in truthTable)
            {
                if (R.StateIsPossible(tableKey, state))
                    ++total;
                else
                    return 0;
            }

            //Return the total.
            return total;
        }

        public override void PrintQuery(string symbol)
        {
            int total = Query(symbol);
            if (total > 0)
                Console.WriteLine("YES: " + total);
            else
                Console.WriteLine("NO");
        }

        public override void PrintQueryUsingRule(string query)
        {
            int total = QueryUsingRuleFromText(query);
            if (total > 0)
                Console.WriteLine("YES: " + total);
            else
                Console.WriteLine("NO");
        }
    }
}