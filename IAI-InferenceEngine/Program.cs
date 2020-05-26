using System;

namespace InferenceEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            TTKnowledgeBase KB = new TTKnowledgeBase("p2=> p3; p3 => p1; c => e; b&e => f; f&g => h; p1=>d; p1&p3 => c; a; b; p2;");
            KB.PrintRules();
            Console.WriteLine("a: " + KB.Query("a"));
            Console.WriteLine("b: " + KB.Query("b"));
            Console.WriteLine("c: " + KB.Query("c"));
            Console.WriteLine("d: " + KB.Query("d"));
            Console.WriteLine("e: " + KB.Query("e"));
            Console.WriteLine("f: " + KB.Query("f"));
            Console.WriteLine("g: " + KB.Query("g"));
            Console.WriteLine("h: " + KB.Query("h"));
            Console.WriteLine("p1: " + KB.Query("p1"));
            Console.WriteLine("p2: " + KB.Query("p2"));
            Console.WriteLine("p3: " + KB.Query("p3"));

            Console.ReadLine();

            //If there is at least one argument, begin execution, otherwise print an error.
            if (args.Length > 0)
            {
                //If the first argument (the 'command') is custom etc, build a custom environment instead of searching.
                if (args[0].ToLower() == "custom" || args[0].ToLower() == "new" || args[0].ToLower() == "environment")
                {

                }
                else if (args[0].ToLower() == "print")
                {
                    if (args.Length > 1)
                    {
                    }
                    else
                        Console.WriteLine("Please specify an environment to print.");
                }
                else if (args[0].ToLower() == "search" || args[0].ToLower() == "nicesearch")
                {
                }
                else
                    Console.WriteLine("Unknown Command.");
            }
            else
            {
                Console.WriteLine("Unknown Command");
                Console.WriteLine("Make sure you run this with arguments from a console.");
                Console.WriteLine("Use 'search <environment> <method>' to search");
                Console.WriteLine("Use 'custom' to create a custom environment");
                Console.WriteLine("Use 'search custom <method>' to create a custom environment and search");
                Console.ReadLine();
            }
        }
    }
}
