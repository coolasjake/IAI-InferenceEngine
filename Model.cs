using System;
using System.Collections.Generic;
using System.Text;

namespace InferenceEngine
{
    public abstract class Model
    {
        protected List<string> uniqueSymbols = new List<string>();

        public abstract void PrintRules();

        public abstract void Tell(string knowledge);

        public static string TF(bool val)
        {
            if (val)
                return "T";
            else
                return "F";
        }

        public abstract int Query(string symbol);

        public int QueryUsingRuleFromText(string query)
        {
            Rule R = new Rule(query);
            return QueryUsingRule(R);
        }

        protected abstract int QueryUsingRule(Rule query);

        public abstract void PrintQuery(string symbol);

        public abstract void PrintQueryUsingRule(string symbol);
    }

    /// <summary> A class which contains a single parent-preposition, and functionality involved in generating, checking and resolving prepositions.
    /// For the input [A /\ B => ~C] the parent preposition would be an 'Implication' containing child prepositions P1: [A /\ B] and P2: [~C].
    /// P1 would be expressed as an 'And' preposition containing two 'Symbol' prepositions, while P2 would be expressed as a 'Not' preposition containing
    /// a single 'Symbol' preposition.</summary>
    public class Rule
    {
        public string debugTextVersion;
        public List<string> symbols = new List<string>();
        protected Proposition _rule;

        public Rule(string ruleAsText)
        {
            debugTextVersion = ruleAsText.Trim();
            _rule = FindImplication(GenerateElements(debugTextVersion));
        }
        public Rule() {}

        public virtual bool StateIsPossible(string[] key, bool[] state)
        {
            return _rule.Check(key, state);
        }

        public SymbolIs CheckSolved(string[] key, SymbolIs[] knowledge)
        {
            return _rule.CheckSolvable(key, knowledge);
        }

        public override string ToString()
        {
            if (_rule != null)
                return _rule.ToString();
            return debugTextVersion;
        }

        protected virtual List<string> GenerateElements(string ruleAsText)
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
                else if (chars[i] == '|' && i < chars.Length - 1 && chars[i + 1] == '|')
                {
                    if (symbol.Count > 0)
                        elements.Add(new string(symbol.ToArray()));
                    elements.Add("||");
                    symbol.Clear();
                    ++i;
                }
                else if (chars[i] == '~')
                {
                    if (symbol.Count > 0)
                        elements.Add(new string(symbol.ToArray()));
                    elements.Add("~");
                    symbol.Clear();
                }
                else if (chars[i] == '<' && i < chars.Length - 2 && chars[i + 1] == '=' && chars[i + 2] == '>')
                {
                    if (symbol.Count > 0)
                        elements.Add(new string(symbol.ToArray()));
                    elements.Add("<=>");
                    symbol.Clear();
                    i += 2;
                }
                else if (chars[i] == '=' && i < chars.Length - 1 && chars[i + 1] == '>')
                {
                    if (symbol.Count > 0)
                        elements.Add(new string(symbol.ToArray()));
                    elements.Add("=>");
                    symbol.Clear();
                    ++i;
                }
                else if (chars[i] == '(')
                {
                    if (symbol.Count > 0)
                        elements.Add(new string(symbol.ToArray()));
                    elements.Add("(");
                    symbol.Clear();
                }
                else if (chars[i] == ')')
                {
                    if (symbol.Count > 0)
                        elements.Add(new string(symbol.ToArray()));
                    elements.Add(")");
                    symbol.Clear();
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

            return elements;
        }

        protected virtual Proposition FindImplication(List<string> elements)
        {
            //Throw an exception if the are zero elements. This is the ONLY error checked for here.
            if (elements.Count == 0)
                throw new Exception("Too few elements to generate rule");

            //Simplify the process for individual symbols
            if (elements.Count == 1)
            {
                symbols.Add(elements[0]);
                return new Symbol(elements[0]); ;
            }

            //Create list of generation helper-classes, and immediately generate logic for symbols
            List<PrepGen> logicalEs = new List<PrepGen>();
            foreach (string element in elements)
            {
                PrepGen PG = new PrepGen(element);
                if (element != "=>" && element != "&" && element != "|" && element != "~" && element != "<=>" && element != "(" && element != ")")
                {
                    PG._p = new Symbol(element);
                    symbols.Add(element);
                }
                logicalEs.Add(PG);
            }

            //Find and solve Brackets
            for (int i = 0; i < logicalEs.Count; ++i)
            {
                if (logicalEs[i].Used)
                    continue;
                if (logicalEs[i]._strRep == "(")
                {
                    int a;
                    int opened = 1;
                    for (a = i + 1; a < logicalEs.Count; ++a)
                    {
                        if (logicalEs[a]._strRep == "(")
                            ++opened;
                        if (logicalEs[a]._strRep == ")")
                        {
                            --opened;
                            if (opened == 0)
                                break;
                        }
                    }
                    logicalEs[i]._p = FindImplication(elements.GetRange(i + 1, a - i - 1));
                    for (int j = i + 1; j <= a; ++j)
                        logicalEs[j].Use();
                }
            }

            //Find Not-connections
            for (int i = 0; i < logicalEs.Count; ++i)
            {
                if (logicalEs[i].Used)
                    continue;
                if (logicalEs[i]._strRep == "~")
                {
                    int a;
                    for (a = i + 1; a < logicalEs.Count; ++a)
                    {
                        if (!logicalEs[a].Used)
                            break;
                    }
                    logicalEs[i]._p = new Not(logicalEs[a]._p);
                    logicalEs[a].Use();
                }
            }

            //Find And / Or
            for (int i = 0; i < logicalEs.Count; ++i)
            {
                if (logicalEs[i].Used)
                    continue;
                //Find And-connections
                if (elements[i] == "&")
                {
                    int a, b;
                    for (a = i - 1; a > 0; --a)
                    {
                        if (!logicalEs[a].Used)
                            break;
                    }
                    for (b = i + 1; b < logicalEs.Count; ++b)
                    {
                        if (!logicalEs[b].Used)
                            break;
                    }
                    logicalEs[i]._p = new And(logicalEs[a]._p, logicalEs[b]._p);
                    logicalEs[a].Use();
                    logicalEs[b].Use();
                }
                //Find And-connections
                if (elements[i] == "||")
                {
                    int a, b;
                    for (a = i - 1; a > 0; --a)
                    {
                        if (!logicalEs[a].Used)
                            break;
                    }
                    for (b = i + 1; b < logicalEs.Count; ++b)
                    {
                        if (!logicalEs[b].Used)
                            break;
                    }
                    logicalEs[i]._p = new Or(logicalEs[a]._p, logicalEs[b]._p);
                    logicalEs[a].Use();
                    logicalEs[b].Use();
                }
            }

            //Find Implications and Biconditionals
            for (int i = 0; i < logicalEs.Count; ++i)
            {
                if (logicalEs[i].Used)
                    continue;
                if (elements[i] == "=>")
                {
                    int a, b;
                    for (a = i - 1; a > 0; --a)
                    {
                        if (!logicalEs[a].Used)
                            break;
                    }
                    for (b = i + 1; b < logicalEs.Count; ++b)
                    {
                        if (!logicalEs[b].Used)
                            break;
                    }
                    logicalEs[i]._p = new Implies(logicalEs[a]._p, logicalEs[b]._p);
                    logicalEs[a].Use();
                    logicalEs[b].Use();
                }
                if (elements[i] == "<=>")
                {
                    int a, b;
                    for (a = i - 1; a > 0; --a)
                    {
                        if (!logicalEs[a].Used == false)
                            break;
                    }
                    for (b = i + 1; b < logicalEs.Count; ++b)
                    {
                        if (!logicalEs[b].Used)
                            break;
                    }
                    logicalEs[i]._p = new BiConditional(logicalEs[a]._p, logicalEs[b]._p);
                    logicalEs[a].Use();
                    logicalEs[b].Use();
                }
            }

            //Find the parent logical element and return it
            for (int i = 0; i < logicalEs.Count; ++i)
            {
                if (!logicalEs[i].Used)
                    return logicalEs[i]._p;
            }

            //Return an 'always true' rule if somehow no logic was created.
            return new True();
        }

        protected class PrepGen
        {
            public string _strRep;
            public Proposition _p;
            private bool _used = false;

            public bool Used
            {
                get { return _used; }
            }

            public void Use()
            {
                _used = true;
            }

            public PrepGen(string strRep)
            {
                _strRep = strRep;
            }

            public override string ToString()
            {
                return _strRep + " Used: " + Model.TF(_used);
            }
        }

        /// <summary> Propositions represent the different logical elements of a rule, which form a tree with 'IsTrue' at the end of each branch. </summary>
        public abstract class Proposition
        {
            /// <summary> Using the given worldstate, return the output of this preposition given the current knowledge. Unknown symbols are converted to 'false'. </summary>
            public abstract bool Check(string[] key, bool[] state);

            /// <summary> Using the given knowledge, return the result of this preposition, or unknown if there is not enough information to fully resolve it. </summary>
            public abstract SymbolIs CheckSolvable(string[] key, SymbolIs[] state);

            /// <summary> Using the given knowledge, return the most simple solvable version of this preposition chain. Fully solvable chain will result in True(). </summary>
            public virtual Proposition Resolve(string[] key, SymbolIs[] state)
            {
                SymbolIs solution = CheckSolvable(key, state);
                if (solution == SymbolIs.True)
                    return new True();
                else if (solution == SymbolIs.False)
                    return new Not(new True());
                else
                    return this;
            }

            public virtual bool IsResolved { get => false; }
        }

        /// <summary> Propositional class which represents a solved value. Cannot contain children and always returns True. </summary>
        public class True : Proposition
        {

            public True() {}

            /// <summary> Return false if the requirement is met AND the implication is not true. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return true;
            }

            public override SymbolIs CheckSolvable(string[] key, SymbolIs[] state)
            {
                return SymbolIs.True;
            }

            public override Proposition Resolve(string[] key, SymbolIs[] state)
            {
                return this;
            }

            public override bool IsResolved
            {
                get { return true; }
            }

            public override string ToString()
            {
                return "T";
            }
        }

        /// <summary> Propositional class which logically represents an Implication (=>). Contains two child prepositions: [A=>B] A: requirement, B: implication. </summary>
        public class Implies : Proposition
        {
            private Proposition _requirement;
            private Proposition _implication;

            public Proposition Requirement
            {
                get { return _requirement; }
            }

            public Proposition Implication
            {
                get { return _implication; }
            }

            public Implies(Proposition requirement, Proposition implication)
            {
                _requirement = requirement;
                _implication = implication;
            }

            /// <summary> Return false if the requirement is met AND the implication is not true. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return (!_requirement.Check(key, state) || _implication.Check(key, state));
            }

            public override SymbolIs CheckSolvable(string[] key, SymbolIs[] state)
            {
                SymbolIs req = _requirement.CheckSolvable(key, state);
                SymbolIs imp = _implication.CheckSolvable(key, state);
                if (req == SymbolIs.False)
                    return SymbolIs.True;
                else if (imp == SymbolIs.True)
                    return SymbolIs.True;
                else if (req == SymbolIs.True && imp == SymbolIs.False)
                    return SymbolIs.False;
                else
                    return SymbolIs.Unknown;
            }

            public override Proposition Resolve(string[] key, SymbolIs[] state)
            {
                //Resolve child prepositions.
                _requirement = _requirement.Resolve(key, state);
                _implication = _implication.Resolve(key, state);
                
                SymbolIs solution = CheckSolvable(key, state);
                
                if (solution == SymbolIs.True)
                    return new True();
                else if (solution == SymbolIs.False)
                    return new Not(new True());
                else
                {
                    //If the requirement [A in A=>B] is true, return the implication [B].
                    SymbolIs req = _requirement.CheckSolvable(key, state);

                    if (req == SymbolIs.True)
                        return _implication;
                    else
                        return this;
                }
            }

            public override string ToString()
            {
                return "(" +_requirement.ToString() + " => " + _implication.ToString() + ")";
            }
        }

        /// <summary> Propositional class which logically represents a Biconditional (k=>)*. Contains two child prepositions: fact1 and fact2. </summary>
        // *cannot use < symbol in summary comment
        public class BiConditional : Proposition
        {
            private Proposition _fact1;
            private Proposition _fact2;

            public BiConditional(Proposition fact1, Proposition fact2)
            {
                _fact1 = fact1;
                _fact2 = fact2;
            }

            /// <summary> Return false if the facts are not of the same value. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return (_fact1.Check(key, state) == _fact2.Check(key, state));
            }

            public override SymbolIs CheckSolvable(string[] key, SymbolIs[] state)
            {
                SymbolIs sol1 = _fact1.CheckSolvable(key, state);
                SymbolIs sol2 = _fact2.CheckSolvable(key, state);
                if (sol1 == sol2 && sol1 != SymbolIs.Unknown)
                    return SymbolIs.True;
                else if (sol1 != SymbolIs.Unknown && sol2 != SymbolIs.Unknown)
                    return SymbolIs.False;
                else
                    return SymbolIs.Unknown;
            }

            public override Proposition Resolve(string[] key, SymbolIs[] state)
            {
                //Resolve child prepositions.
                _fact1 = _fact1.Resolve(key, state);
                _fact2 = _fact2.Resolve(key, state);

                SymbolIs sol1 = _fact1.CheckSolvable(key, state);
                SymbolIs sol2 = _fact2.CheckSolvable(key, state);

                //If either is false resolve to Not-True, if both are true resolve to True.
                if (sol1 == SymbolIs.False || sol2 == SymbolIs.False)
                    return new Not(new True());
                else if (sol1 == SymbolIs.True && sol2 == SymbolIs.True)
                    return new True();
                else
                {
                    //If one of the facts can be proved true, it can be removed from the logic.
                    if (sol1 == SymbolIs.True)
                        return _fact2;
                    else if (sol2 == SymbolIs.True)
                        return _fact1;
                    else
                        return this;
                }
            }

            public override string ToString()
            {
                return "(" + _fact1.ToString() + " <=> " + _fact2.ToString() + ")";
            }
        }

        /// <summary> Propositional class which logically represents a conjunction (/\) or (&). Contains two child prepositions: fact1 and fact2. </summary>
        public class And : Proposition
        {
            private Proposition _fact1;
            private Proposition _fact2;
            
            public And(Proposition fact1, Proposition fact2)
            {
                _fact1 = fact1;
                _fact2 = fact2;
            }

            /// <summary> Return false if either contained preposition is false. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return (_fact1.Check(key, state) && _fact2.Check(key, state));
            }

            public override SymbolIs CheckSolvable(string[] key, SymbolIs[] state)
            {
                SymbolIs sol1 = _fact1.CheckSolvable(key, state);
                SymbolIs sol2 = _fact2.CheckSolvable(key, state);
                if (sol1 == SymbolIs.False || sol2 == SymbolIs.False)
                    return SymbolIs.False;
                else if (sol1 == SymbolIs.True && sol2 == SymbolIs.True)
                    return SymbolIs.True;
                else
                    return SymbolIs.Unknown;
            }

            public override Proposition Resolve(string[] key, SymbolIs[] state)
            {
                //Resolve child prepositions.
                _fact1 = _fact1.Resolve(key, state);
                _fact2 = _fact2.Resolve(key, state);

                SymbolIs sol1 = _fact1.CheckSolvable(key, state);
                SymbolIs sol2 = _fact2.CheckSolvable(key, state);

                //If either is false resolve to Not-True, if both are true resolve to True.
                if (sol1 == SymbolIs.False || sol2 == SymbolIs.False)
                    return new Not(new True());
                else if (sol1 == SymbolIs.True && sol2 == SymbolIs.True)
                    return new True();
                else
                {
                    //If one of the facts can be proved true, it can be removed from the logic.
                    if (sol1 == SymbolIs.True)
                        return _fact2;
                    else if (sol2 == SymbolIs.True)
                        return _fact1;
                    else
                        return this;
                }
            }

            public override string ToString()
            {
                return "(" + _fact1.ToString() + " & " + _fact2.ToString() + ")";
            }
        }

        /// <summary> Propositional class which logically represents a disjunction (\/) or (||). Contains two child prepositions: fact1 and fact2. </summary>
        public class Or : Proposition
        {
            private Proposition _fact1;
            private Proposition _fact2;

            public Or(Proposition fact1, Proposition fact2)
            {
                _fact1 = fact1;
                _fact2 = fact2;
            }

            /// <summary> Return false if both contained prepositions are false. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return (_fact1.Check(key, state) || _fact2.Check(key, state));
            }

            public override SymbolIs CheckSolvable(string[] key, SymbolIs[] state)
            {
                SymbolIs sol1 = _fact1.CheckSolvable(key, state);
                SymbolIs sol2 = _fact2.CheckSolvable(key, state);
                if (sol1 == SymbolIs.False && sol2 == SymbolIs.False)
                    return SymbolIs.False;
                else if (sol1 == SymbolIs.True || sol2 == SymbolIs.True)
                    return SymbolIs.True;
                else
                    return SymbolIs.Unknown;
            }

            public override Proposition Resolve(string[] key, SymbolIs[] state)
            {
                //Resolve child prepositions.
                _fact1 = _fact1.Resolve(key, state);
                _fact2 = _fact2.Resolve(key, state);

                SymbolIs sol1 = _fact1.CheckSolvable(key, state);
                SymbolIs sol2 = _fact2.CheckSolvable(key, state);

                //If both are false resolve to Not-True, if either is true resolve to True.
                if (sol1 == SymbolIs.False && sol2 == SymbolIs.False)
                    return new Not(new True());
                else if (sol1 == SymbolIs.True || sol2 == SymbolIs.True)
                    return new True();
                else
                {
                    //If one of the facts can be proved false, it can be removed from the logic.
                    if (sol1 == SymbolIs.False)
                        return _fact2;
                    else if (sol2 == SymbolIs.False)
                        return _fact1;
                    else
                        return this;
                }
            }

            public override string ToString()
            {
                return "(" + _fact1.ToString() + " || " + _fact2.ToString() + ")";
            }
        }

        /// <summary> Propositional class which logically represents a negation (~). Contains a single child preposition: fact. </summary>
        public class Not : Proposition
        {
            private Proposition _fact;
            
            public Not(Proposition fact)
            {
                _fact = fact;
            }

            /// <summary> Return false if the contained preposition is true. </summary>
            public override bool Check(string[] key, bool[] state)
            {
                return !_fact.Check(key, state);
            }

            public override SymbolIs CheckSolvable(string[] key, SymbolIs[] state)
            {
                SymbolIs solution = _fact.CheckSolvable(key, state);
                if (solution == SymbolIs.False)
                    return SymbolIs.True;
                else if (solution == SymbolIs.True)
                    return SymbolIs.False;
                else
                    return SymbolIs.Unknown;
            }

            public override Proposition Resolve(string[] key, SymbolIs[] state)
            {
                if (_fact is True)
                    return this;
                return base.Resolve(key, state);
            }

            public override bool IsResolved
            {
                get { return _fact.IsResolved; }
            }

            public string Symbol
            {
                get { return _fact.ToString(); }
            }

            public override string ToString()
            {
                return "~" + _fact.ToString();
            }
        }

        /// <summary> Propositional class which logically represents a symbol. Contains the symbol as a string. </summary>
        public class Symbol : Proposition
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

            public override SymbolIs CheckSolvable(string[] key, SymbolIs[] state)
            {
                for (int i = 0; i < key.Length; ++i)
                {
                    if (_symbol == key[i])
                        return state[i];
                }
                return SymbolIs.Unknown;
            }

            public override string ToString()
            {
                return _symbol;
            }
        }
    }

    /// <summary> True/False/Unknown.
    /// Used for resolving rules by checking if a (sub) preposition will always give the same result with the given knowledge. </summary>
    public enum SymbolIs
    {
        Unknown,
        True,
        False
    }
}