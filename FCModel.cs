using System;
using System.Collections.Generic;
using System.Text;

namespace InferenceEngine
{

    public class ForwardChainingModel : HornModel
    {
        /* Method:
         * - Assume (or check) all rules are in Horn Form [A /\ B /\ C etc => Z], where any number of cunjunctional literals implies a single literal.
         * - Use given literals to fill in sentences (i.e. replace [A /\ B => C; A] with [B => C])
         * - If all literals are known in a sentence use it's inference to fill in more sentences (e.g. use [A => B; B => C; A] to infer [B => C; B] to infer [C])
         * - Stop when either the query is directly infered (True => a) or no more inferences can be made (returning KB |= a or KB ~|= a respectively)
         * 
         * Algorithm:
         * - Seperate KB into literals and 'rules'
         * - Foreach literal [i.e. A or ~A]
         * -    Foreach rule [i.e. conjunction => literal]
         * -        If one of the conjunctions is a solved literal
         * -            Remove that literal from the rule
         * -        If the rule is solved [i.e. there are no literals remaining on the left side]
         * -            Remove the rule and add the implied literal to the solved literals list
         * -            If the implied literal is Q return true [KB |= Q]
         * - Return false [KB ~|= Q]
         */

        public ForwardChainingModel(string knowledge)
        {
            Tell(knowledge);
        }

        public ForwardChainingModel() { }

        //UNUSED! Simplifies the KB by substituting proven literals into rules. Created to understand how to solve the KB.
        public void SolveKB()
        {
            bool ruleChanged = true;
            while (ruleChanged)
            {
                ruleChanged = false;
                foreach (HornRule R in KB)
                {
                    if (R.Resolve(literalsKey, literalsSolutions))
                    {
                        ruleChanged = true;
                        for (int i = 0; i < literalsKey.Length; ++i)
                        {
                            SymbolIs solution = R.DefinesLiteral(literalsKey[i]);
                            if (solution != SymbolIs.Unknown && literalsSolutions[i] == SymbolIs.Unknown)
                                literalsSolutions[i] = solution;
                        }
                    }
                }
            }
        }

        public override bool SolveAndQuery(string symbol)
        {
            bool stateChanged = true;
            while (stateChanged)
            {
                stateChanged = false;
                //Try to solve each of the rules given the current knowledge.
                foreach (HornRule R in KB)
                {
                    //Resolve the rule. If it was fully resolved:
                    R.Resolve(literalsKey, literalsSolutions);
                    if (R.IsFullyResolved)
                    {
                        //If this rule proves a literal (i.e. doesn't return Unknown), add the literal to the knowledge and remove the rule.
                        HornRule.Literal newLiteral = R.ProveLiteral(literalsKey, literalsSolutions);
                        if (newLiteral.state != SymbolIs.Unknown)
                        {
                            stateChanged = true;

                            //Loop through the knowledge table and add the new information at the correct index.
                            for (int i = 0; i < literalsKey.Length; ++i)
                            {
                                if (literalsKey[i] == newLiteral.symbol)
                                {
                                    if (literalsSolutions[i] == SymbolIs.Unknown)
                                        literalsSolutions[i] = newLiteral.state;
                                    else if (literalsSolutions[i] != newLiteral.state)
                                        throw new Exception("KnowledgeBase is contradictory");
                                }
                            }

                            //If the symbol being proven is the query symbol, return true.
                            if (newLiteral.symbol == symbol)
                                return true;
                        }
                    }
                }
            } //End of While loop

            return false;
        }

        protected override int QueryUsingRule(Rule query)
        {
            List<HornRule> checkedRules = new List<HornRule>();

            bool stateChanged = true;
            while (stateChanged)
            {
                stateChanged = false;
                //Try to solve each of the rules given the current knowledge.
                foreach (HornRule HR in KB)
                {
                    if (checkedRules.Contains(HR))
                        continue;
                    //Resolve the rule. If it was fully resolved:
                    HR.Resolve(literalsKey, literalsSolutions);
                    if (HR.IsFullyResolved)
                    {
                        //If this rule proves a literal (i.e. doesn't return Unknown), add the literal to the knowledge and remove the rule.
                        HornRule.Literal newLiteral = HR.ProveLiteral(literalsKey, literalsSolutions);
                        if (newLiteral.state != SymbolIs.Unknown)
                        {
                            stateChanged = true;
                            checkedRules.Add(HR);

                            //Loop through the knowledge table and add the new information at the correct index.
                            for (int i = 0; i < literalsKey.Length; ++i)
                            {
                                if (literalsKey[i] == newLiteral.symbol)
                                {
                                    if (literalsSolutions[i] == SymbolIs.Unknown)
                                        literalsSolutions[i] = newLiteral.state;
                                    else if (literalsSolutions[i] != newLiteral.state)
                                        throw new Exception("KnowledgeBase is contradictory");
                                }
                            }

                            //If the query has been proven, return either true or false based on the outcome.
                            SymbolIs solution = query.CheckSolved(literalsKey, literalsSolutions);
                            if (solution == SymbolIs.True)
                                return 1;
                            else if (solution == SymbolIs.False)
                                return 0;
                        }
                    }
                }
            } //End of While loop

            return 0;
        }


    }

    public abstract class HornModel : Model
    {
        protected List<HornRule> KB = new List<HornRule>();

        protected string[] literalsKey;
        protected SymbolIs [] literalsSolutions;
        
        public override void Tell(string knowledge)
        {
            //Split the text into prepositions and use them to generate rules
            string[] strRules = knowledge.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in strRules)
                KB.Add(new HornRule(s));

            //Find each unique symbol in the given rules
            foreach (HornRule r in KB)
            {
                foreach (string symbol in r.symbols)
                {
                    if (!uniqueSymbols.Contains(symbol))
                        uniqueSymbols.Add(symbol);
                }
            }

            //Initialize KB
            literalsKey = uniqueSymbols.ToArray();
            literalsSolutions = new SymbolIs[literalsKey.Length];
        }

        protected void FindDefinitions()
        {
            for (int i = 0; i < literalsKey.Length; ++i)
            {
                literalsSolutions[i] = SymbolIs.Unknown;
                foreach (HornRule HR in KB)
                {
                    SymbolIs solution = HR.DefinesLiteral(literalsKey[i]);
                    if (solution != SymbolIs.Unknown)
                    {
                        literalsSolutions[i] = solution;
                        break;
                    }
                }
            }
        }

        public override void PrintRules()
        {
            foreach (HornRule rule in KB)
                Console.Write(rule + ",  ");
            Console.WriteLine();
        }

        public  void PrintKB()
        {
            for (int i = 0; i < literalsKey.Length; ++i)
                Console.WriteLine(literalsKey[i] + ": " + literalsSolutions[i]);
        }

        public override int Query(string symbol)
        {
            for (int i = 0; i < literalsKey.Length; ++i)
            {
                if (literalsKey[i] == symbol)
                {
                    if (literalsSolutions[i] == SymbolIs.True)
                        return 1;
                    else
                        return 0;
                }
            }
            int total = 0;
            return total;
        }

        public override void PrintQuery(string symbol)
        {
            if (SolveAndQuery(symbol))
            {
                Console.Write("YES: ");
                for (int i = 0; i < literalsKey.Length; ++i)
                {
                    if (literalsSolutions[i] == SymbolIs.True)
                        Console.Write(literalsKey[i] + ", ");
                }
                Console.WriteLine();
            }
            else
                Console.WriteLine("NO");
        }

        public override void PrintQueryUsingRule(string query)
        {
            if (QueryUsingRuleFromText(query) > 0)
            {
                Console.Write("YES: ");
                for (int i = 0; i < literalsKey.Length; ++i)
                {
                    if (literalsSolutions[i] == SymbolIs.True)
                        Console.Write(literalsKey[i] + ", ");
                }
                Console.WriteLine();
            }
            else
                Console.WriteLine("NO");
        }

        public abstract bool SolveAndQuery(string symbol);
    }

    /// <summary> Has 2 properties, the literal and the clause which implies the literal (stored in _rule).
    /// If the rule is just a literal, the clause will be a True() class.  </summary>
    public class HornRule : Rule
    {
        protected Proposition _literalPrep;

        protected Literal _literalVal;
        protected bool _definesLiteral = false;
        
        public HornRule(string ruleAsText)
        {
            debugTextVersion = ruleAsText.Trim();
            _rule = FindImplication(GenerateElements(debugTextVersion));
        }

        protected override Proposition FindImplication(List<string> elements)
        {
            Proposition totalRule = base.FindImplication(elements);
            if (totalRule is Implies) {
                GenerateLiteral(((Implies)totalRule).Implication);
                _definesLiteral = false;
                return ((Implies)totalRule).Requirement;
            }
            else
            {
                GenerateLiteral(totalRule);
                return new True();
            }
        }

        private void GenerateLiteral(Proposition literalPrep)
        {
            if (literalPrep is Not)
            {
                _literalPrep = literalPrep;
                _literalVal = new Literal(((Not)literalPrep).Symbol, true);
                _definesLiteral = true;
            }
            else if (literalPrep is Symbol)
            {
                _literalPrep = literalPrep;
                _literalVal = new Literal(((Symbol)literalPrep).ToString(), false);
                _definesLiteral = true;
            }
        }

        public Literal ProveLiteral(string[] key, SymbolIs[] knowledge)
        {
            SymbolIs solution = _rule.CheckSolvable(key, knowledge);
            if (solution == SymbolIs.True)
                return _literalVal;
            else if (solution == SymbolIs.False)
                return new Literal(_literalVal.symbol, _literalVal.Inverse);
            else
                return new Literal(_literalVal.symbol, SymbolIs.Unknown);
        }

        public SymbolIs DefinesLiteral(string literal)
        {
            if (_definesLiteral && _literalVal.symbol == literal)
                return _literalVal.state;
            return SymbolIs.Unknown;
        }

        public bool LiteralIsImplication(string literal)
        {
            if (_literalVal.symbol == literal)
                return true;
            return false;
        }

        /// <summary> Simplify the rule using the specified knowledge. Returns true if the rule was changed. </summary>
        public bool Resolve(string[] key, SymbolIs[] knowledge)
        {
            Proposition newRule = _rule.Resolve(key, knowledge);
            if (newRule == _rule)
                return false;
            _rule = newRule;
            if (_rule is True)
                _definesLiteral = true;
            return true;
        }

        public bool IsFullyResolved { get => _rule.IsResolved; }

        public override bool StateIsPossible(string[] key, bool[] state)
        {
            return _rule.Check(key, state);
        }
        
        public override string ToString()
        {
            if (_rule != null)
                return _rule.ToString() + " => " + _literalPrep.ToString();
            return debugTextVersion;
        }

        public struct Literal
        {
            public string symbol;
            public SymbolIs state;

            public Literal(string Symbol, bool Negated)
            {
                symbol = Symbol;
                if (Negated)
                    state = SymbolIs.False;
                else
                    state = SymbolIs.True;
            }

            public Literal(string Symbol, SymbolIs State)
            {
                symbol = Symbol;
                state = State;
            }

            public SymbolIs Inverse
            {
                get
                {
                    if (state == SymbolIs.False)
                        return SymbolIs.True;
                    else if (state == SymbolIs.True)
                        return SymbolIs.False;
                    else
                        return SymbolIs.Unknown;
                }
            }
        }
    }
}