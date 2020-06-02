using System;
using System.Collections.Generic;
using System.Text;

namespace InferenceEngine
{
    class BackwardChainingModel : HornModel
    {
        /* Method:
         * - Assume (or check) all rules are in Horn Form [A /\ B /\ C etc => Z], where any number of cunjunctional literals implies a single literal.
         * - Starting from any rules which imply Q,
         * - Check for literals that prove that rule,
         * - Then check for rules that imply those literals
         * - Repeat until either Q is proven or no NEW rules can be checked
         * - Make sure rules are not checked multiple times to prevent loops
         * - Use recursion:
         * - FindRules(Q) =>
         * - FindLiterals(rule) =>
         * - FindRules(each literal in rule) =>
         * - Terminate if rule = checked or literal = false
         */

        public BackwardChainingModel(string knowledge)
        {
            Tell(knowledge);
        }

        public BackwardChainingModel() { }

        public override bool SolveAndQuery(string symbol)
        {
            //Initialize
            List<string> literalsToSolve = new List<string>();
            literalsToSolve.Add(symbol);
            List<HornRule> rulesToSolve = new List<HornRule>();
            List<HornRule> checkedRules = new List<HornRule>();
            bool stateChanged = true;

            while (stateChanged == true)
            {
                stateChanged = false;

                //Add the the list  of rules to solve any rules which are relevant to our query (I.e. are relevant to symbols which are relevant).
                //Then add each symbol used by those rules to the list of literals to solve.
                foreach (HornRule HR in KB)
                {
                    if (checkedRules.Contains(HR))
                        continue;
                    for (int i = 0; i < literalsToSolve.Count; ++i)
                    {
                        if (HR.LiteralIsImplication(literalsToSolve[i]))
                        {
                            stateChanged = true;
                            rulesToSolve.Add(HR);
                            checkedRules.Add(HR);
                            foreach (string s in HR.symbols)
                            {
                                if (!literalsToSolve.Contains(s))
                                    literalsToSolve.Add(s);
                            }
                            break;
                        }
                    }
                }

                //Try to solve each of the rules given the current knowledge.
                //If a rule is solved, remove it from the list, and add any symbols it entails to the KB.
                for (int r = 0; r < rulesToSolve.Count; ++r)
                {
                    //Resolve the rule. If it was fully resolved:
                    rulesToSolve[r].Resolve(literalsKey, literalsSolutions);
                    if (rulesToSolve[r].IsFullyResolved)
                    {
                        //If this rule proves a literal (i.e. doesn't return Unknown), add the literal to the knowledge and remove the rule.
                        HornRule.Literal newLiteral = rulesToSolve[r].ProveLiteral(literalsKey, literalsSolutions);
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

                            //Stop trying to find rules relevant to the proven literal.
                            literalsToSolve.Remove(newLiteral.symbol);

                            //Remove the solved rule from the list, and go back one index.
                            rulesToSolve.RemoveAt(r);
                            --r;
                        }
                    }
                }
            } //End of While loop.

            return false;
        }

        protected override int QueryUsingRule(Rule query)
        {
            //Initialize
            List<string> literalsToSolve = new List<string>();
            foreach (string symbol in query.symbols)
                literalsToSolve.Add(symbol);
            List<HornRule> rulesToSolve = new List<HornRule>();
            List<HornRule> checkedRules = new List<HornRule>();
            bool stateChanged = true;

            while (stateChanged == true)
            {
                stateChanged = false;

                //Add the the list  of rules to solve any rules which are relevant to our query (I.e. are relevant to symbols which are relevant).
                //Then add each symbol used by those rules to the list of literals to solve.
                foreach (HornRule HR in KB)
                {
                    if (checkedRules.Contains(HR))
                        continue;
                    for (int i = 0; i < literalsToSolve.Count; ++i)
                    {
                        if (HR.LiteralIsImplication(literalsToSolve[i]))
                        {
                            stateChanged = true;
                            rulesToSolve.Add(HR);
                            checkedRules.Add(HR);
                            foreach (string s in HR.symbols)
                            {
                                if (!literalsToSolve.Contains(s))
                                    literalsToSolve.Add(s);
                            }
                            break;
                        }
                    }
                }

                //Try to solve each of the rules given the current knowledge.
                //If a rule is solved, remove it from the list, and add any symbols it entails to the KB.
                for (int r = 0; r < rulesToSolve.Count; ++r)
                {
                    //Resolve the rule. If it was fully resolved:
                    rulesToSolve[r].Resolve(literalsKey, literalsSolutions);
                    if (rulesToSolve[r].IsFullyResolved)
                    {
                        //If this rule proves a literal (i.e. doesn't return Unknown), add the literal to the knowledge and remove the rule.
                        HornRule.Literal newLiteral = rulesToSolve[r].ProveLiteral(literalsKey, literalsSolutions);
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

                            //Stop trying to find rules relevant to the proven literal.
                            literalsToSolve.Remove(newLiteral.symbol);

                            //Remove the solved rule from the list, and go back one index.
                            rulesToSolve.RemoveAt(r);
                            --r;
                        }
                    }
                }

                //If the query has been proven, return either true or false based on the outcome.
                SymbolIs solution = query.CheckSolved(literalsKey, literalsSolutions);
                if (solution == SymbolIs.True)
                    return 1;
                else if (solution == SymbolIs.False)
                    return 0;
            } //End of While loop.

            return 0;
        }
    }
}