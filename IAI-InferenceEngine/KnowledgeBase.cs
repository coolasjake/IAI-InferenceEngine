using System;
using System.Collections.Generic;
using System.Text;

namespace InferenceEngine
{
    class TTKnowledgeBase
    {
        private List<TTRule> rules = new List<TTRule>();
        private List<bool[]> truthTable = new List<bool[]>();
        private string[] tableKey;
        private int tableHeight;
        private int tableWidth;

        public TTKnowledgeBase(string knowledge)
        {
            //Split the text into prepositions and use them to generate rules
            string[] strRules = knowledge.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in strRules)
                rules.Add(new TTRule(s));

            //Find each unique symbol in the given rules
            List<string> uniqueSymbols = new List<string>();
            foreach (TTRule r in rules)
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
                foreach (TTRule r in rules)
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

        public TTKnowledgeBase(string knowledge, bool debugLines)
        {
            //Split the text into prepositions and use them to generate rules
            string[] strRules = knowledge.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in strRules)
                rules.Add(new TTRule(s));

            //Find each unique symbol in the given rules
            List<string> uniqueSymbols = new List<string>();
            foreach (TTRule r in rules)
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
                //Console.Write("Checking TT State: ");
                foreach (bool b in truthTable[j])
                    Console.Write(TF(b) + ", ");
                Console.WriteLine();
                foreach (TTRule r in rules)
                {
                    Console.WriteLine("Checking Rule: " + r.debugTextVersion);
                    if (!r.StateIsPossible(tableKey, truthTable[j]))
                    {
                        Console.WriteLine("Removing row");
                        truthTable.RemoveAt(j);
                        --j;
                        break;
                    }
                }
            }

            Console.WriteLine("Key:");
            foreach (string k in tableKey)
                Console.Write(k + ", ");
            Console.WriteLine();

            Console.WriteLine("Remaining Rows:");
            foreach (bool[] row in truthTable)
            {
                foreach (bool b in row)
                    Console.Write(TF(b) + ", ");
                Console.WriteLine();
            }
        }

        public void PrintRules()
        {
            foreach (TTRule rule in rules)
                Console.WriteLine(rule);
        }

        public string TF(bool val)
        {
            if (val)
                return "T";
            else
                return "F";
        }

        public int Query(string symbol)
        {
            //If the truth table is empty return false. Also the KB is broken
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

            //Otherwise return true
            return total;
        }
    }

    //b&e => f      Implies(And(IsTrue(b), IsTrue(e)), IsTrue(f))
    public class TTRule
    {
        public string debugTextVersion;
        public List<string> symbols = new List<string>();

        private Preposition _rule;

        //private List<string> elements = new List<string>();

        public TTRule(string ruleAsText)
        {
            debugTextVersion = ruleAsText.Trim();
            _rule = FindImplication(GenerateElements(debugTextVersion));
        }

        private string[] GenerateElements(string ruleAsText)
        {
            List<string> elements = new List<string>();
            char[] chars = ruleAsText.ToCharArray();
            List<char> symbol = new List<char>();
            for (int i = 0; i < chars.Length; ++i)
            {
                if (chars[i] == '&')
                {
                    if (symbol.Count > 0)
                        elements.Add(new string(symbol.ToArray()));
                    elements.Add("&");
                    symbol.Clear();
                }
                else if (chars[i] == '=' && i < chars.Length - 1 && chars[i + 1] == '>')
                {
                    if (symbol.Count > 0)
                        elements.Add(new string(symbol.ToArray()));
                    elements.Add("=>");
                    symbol.Clear();
                    ++i;
                }
                else if (chars[i] == ' ')
                {
                    if (symbol.Count > 0)
                        elements.Add(new string(symbol.ToArray()));
                    symbol.Clear();
                }
                else
                    symbol.Add(chars[i]);
            }
            if (symbol.Count > 0)
                elements.Add(new string(symbol.ToArray()));

            return elements.ToArray();
        }

        private Preposition FindImplication(List<string> elements)
        {
            if (elements.Count == 0)
                throw new Exception("Too few elements to generate rule");

            if (elements.Count == 1)
            {
                symbols.Add(elements[0]);
                return new Symbol(elements[0]); ;
            }

            List<PrepGen> logicalEs = new List<PrepGen>();
            foreach (string element in elements)
                logicalEs.Add(new PrepGen(element));
            //Preposition[] logic = new Preposition[elements.Length];
            //bool[] isChild = new bool[elements.Length];

            //Find symbols
            /*
            for (int i = 0; i < elements.Length; ++i)
            {
                if (elements[i] != "=>" && elements[i] != "&")
                {
                    symbols.Add(elements[i]);
                    logic[i] = new Symbol(elements[i]);
                }
            }
            */

            //Brackets
            for (int i = 0; i < logicalEs.Count; ++i)
            {
                if (logicalEs[i]._strRep == "(")
                {
                    int a;
                    for (a = logicalEs.Count - 1; a > i; --a)
                    {
                        if (logicalEs[i]._strRep == ")")
                            break;
                    }
                    logicalEs[i]._p = FindImplication(elements.GetRange(i + 1, i - a - 1));
                    logicalEs[a]._used = true;
                }
            }


            //Find Not-connections
            for (int i = 0; i < elements.Length; ++i)
            {
                if (elements[i] == "~")
                {
                    int a;
                    for (a = i - 1; a > 0; --a)
                    {
                        if (isChild[a] == false)
                            break;
                    }
                    logic[i] = new Not(logic[a]);
                    isChild[a] = true;
                }
            }
            //Find And-connections
            for (int i = 0; i < elements.Length; ++i)
            {
                if (elements[i] == "&")
                {
                    int a, b;
                    for (a = i - 1; a > 0; --a)
                    {
                        if (isChild[a] == false)
                            break;
                    }
                    for (b = i + 1; b < elements.Length; ++b)
                    {
                        if (isChild[b] == false)
                            break;
                    }
                    logic[i] = new And(logic[a], logic[b]);
                    isChild[a] = true;
                    isChild[b] = true;
                }
            }
            //Find Implications
            for (int i = 0; i < elements.Length; ++i)
            {
                if (elements[i] == "=>")
                {
                    int a, b;
                    for (a = i - 1; a > 0; --a)
                    {
                        if (isChild[a] == false)
                            break;
                    }
                    for (b = i + 1; b < elements.Length; ++b)
                    {
                        if (isChild[b] == false)
                            break;
                    }
                    logic[i] = new Implies(logic[a], logic[b]);
                    isChild[a] = true;
                    isChild[b] = true;
                }
            }

            for (int i = 0; i < elements.Length; ++i)
            {
                if (isChild[i] == false)
                    return logic[i];
            }
            return new True();
        }

        public bool StateIsPossible(string[] key, bool[] state)
        {
            return _rule.Check(key, state);
        }

        public override string ToString()
        {
            return _rule.ToString();
        }

        private class PrepGen
        {
            public string _strRep;
            public Preposition _p;
            public bool _used = false;

            public PrepGen(string strRep)
            {
                _strRep = strRep;

                if (_strRep != "=>" && _strRep != "&" && _strRep != "|" && _strRep != "~" && _strRep != "<=>")
                    _p = new Symbol(strRep);
            }


        }

        /// <summary> Prepositions represent the different logical elements of a rule, which form a tree with 'IsTrue' at the end of each branch. </summary>
        private abstract class Preposition
        {
            /// <summary> Using this worldstate, return FALSE if the preposition does NOT allow it, otherwise return true </summary>
            public abstract bool Check(string[] key, bool[] state);

            //public abstract string ToString();
        }

        private class True : Preposition
        {

            public True() {}

            /// <summary> Return false if the requirement is met AND the implication is not true. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return true;
            }

            public override string ToString()
            {
                return "T";
            }
        }

        private class Implies : Preposition
        {
            private Preposition _requirement;
            private Preposition _implication;

            public Implies(Preposition requirement, Preposition implication)
            {
                _requirement = requirement;
                _implication = implication;
            }

            /// <summary> Return false if the requirement is met AND the implication is not true. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return (!_requirement.Check(key, state) || _implication.Check(key, state));
            }

            public override string ToString()
            {
                return _requirement.ToString() + " => " + _implication.ToString();
            }
        }

        private class And : Preposition
        {
            private Preposition _fact1;
            private Preposition _fact2;
            
            public And(Preposition fact1, Preposition fact2)
            {
                _fact1 = fact1;
                _fact2 = fact2;
            }

            /// <summary> Return false if either contained preposition is false. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return (_fact1.Check(key, state) && _fact2.Check(key, state));
            }

            public override string ToString()
            {
                return _fact1.ToString() + "&" + _fact2.ToString();
            }
        }

        private class Not : Preposition
        {
            private Preposition _fact;
            
            public Not(Preposition fact)
            {
                _fact = fact;
            }

            /// <summary> Return false if the contained preposition is true. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return !_fact.Check(key, state);
            }

            public override string ToString()
            {
                return _fact.ToString() + "~" + _fact.ToString();
            }
        }

        private class Symbol : Preposition
        {
            private string _symbol;

            public Symbol(string symbol)
            {
                _symbol = symbol;
            }

            /// <summary> Return true if the symbol is true in the world-state (false if it doesn't exist). </summary>
            public override bool Check(string[] key, bool[] state)
            {
                for (int i = 0; i < key.Length; ++i)
                {
                    if (_symbol == key[i])
                        return state[i];
                }
                return false;
            }

            public override string ToString()
            {
                return _symbol;
            }
        }

        private class BiConditional : Preposition
        {
            private Preposition _fact1;
            private Preposition _fact2;

            public BiConditional(Preposition fact1, Preposition fact2)
            {
                _fact1 = fact1;
                _fact2 = fact2;
            }

            /// <summary> Return false if the facts are not of the same value. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return (_fact1.Check(key, state) == _fact2.Check(key, state));
            }

            public override string ToString()
            {
                return _fact1.ToString() + " <=> " + _fact2.ToString();
            }
        }
    }
}