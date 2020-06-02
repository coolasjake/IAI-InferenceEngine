using System;
using System.IO;
using System.Collections.Generic;

namespace InferenceEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 2)
                RunInference(args[0], args[1]);
            else
                Console.WriteLine("Invalid Command");
        }

        private static void RunInference(string method, string path)
        {
            bool LOG = false;
            Model model = GetModel(method);

            if (!File.Exists(path))
                throw new Exception("Chosen path not found.");
            
            List<string> lines = new List<string>();
            StreamReader SR = new StreamReader(path);
            while (!SR.EndOfStream)
                lines.Add(SR.ReadLine());

            SR.Close();

            if (lines.Count < 2)
                throw new Exception("File is invalid.");

            for (int i = 0; i < lines.Count; ++i)
            {
                if (lines[i] == "TELL" && lines.Count > i + 1)
                {
                    model.Tell(lines[i + 1]);
                    if (LOG)
                        model.PrintRules();
                }
                else if (lines[i] == "ASK" && lines.Count > i + 1)
                {
                    if (LOG)
                        Console.WriteLine("Asking: " + lines[i + 1]);
                    model.PrintQueryUsingRule(lines[i + 1]);
                }
                else if (lines[i] == "RESET")
                {
                    if (LOG)
                        Console.WriteLine("Resetting KB");
                    model = GetModel(method);
                }
                else if (lines[i] == "LOG")
                    LOG = true;
                else if (lines[i] == "NOLOG")
                    LOG = true;
            }
        }

        public static Model GetModel(string method)
        {
            if (method == "FC")
                return new ForwardChainingModel();
            else if (method == "BC")
                return new BackwardChainingModel();
            else
                return new TTModel();
        }
    }
}
