using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace InferenceEngine
{
    /// <summary> Class that handles IO for loading environments. Converts text into lists of information in a 'raw environment',
    /// which the environment then converts into more useable data. </summary>
    public static class Save
    {

        /// <summary> List of directories for the units in the players collection. </summary>
        public static List<string> Collection = new List<string>();
        public static string CurrentNodeName;
        public static int PlayerLevel = 3;

        /// <summary> Try to find the "FileToLoad" file and return the directory it specifies, or the default one. </summary>
        public static string GetDirectory()
        {
            if (File.Exists("Environments/FileToLoad.txt"))
            {
                StreamReader SR = new StreamReader("Environments/FileToLoad.txt");
                return SR.ReadLine();
            }
            else
                Console.WriteLine("Environments/Directory instructions missing!");
            return "Environments/RobotNav-test.txt";
        }

        /*
        /// <summary> Load the given text file and convert the text into Vector2Ints and Rects. Store these in a 'RawEnvironment' and return. </summary>
        public static RawEnvironment LoadEnvironment(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("Chosen directory not found. Swapping to default Environment");
                if (File.Exists("Environments/RobotNav-test.txt"))
                    path = "Environments/RobotNav-test.txt";
                else
                {
                    Console.WriteLine("ERROR loading default directory.");
                    path = "RobotNav-test.txt";
                }
            }

            if (File.Exists(path))
            {
                RawEnvironment RE = new RawEnvironment();
                RE.name = path;
                List<string> lines = new List<string>();
                StreamReader SR = new StreamReader(path);
                while (!SR.EndOfStream)
                    lines.Add(SR.ReadLine());

                if (lines.Count < 3)
                {
                    Console.WriteLine("Environment file incomplete");
                    return new RawEnvironment();
                }

                RE.gridSize = lines[0].StringToV2();
                //Swap the x and y values to make later code simpler.
                int temp = RE.gridSize.x;
                RE.gridSize.x = RE.gridSize.y;
                RE.gridSize.y = temp;
                RE.startPos = lines[1].StringToV2();

                string[] goals = lines[2].Split('|');
                RE.goalsPos = new List<Vector2Int>();

                foreach (string G in goals)
                    RE.goalsPos.Add(G.StringToV2());
                lines.RemoveRange(0, 3);

                if (lines.Count == 0)
                {
                    Console.WriteLine("No walls in Environment");
                    return new RawEnvironment();
                }

                RE.wallRects = new List<Rect>();
                foreach (string wall in lines)
                    RE.wallRects.Add(wall.StringToRect());

                return RE;
            }
            return new RawEnvironment();
        }
        */
    }

}
