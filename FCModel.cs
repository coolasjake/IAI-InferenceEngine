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
}