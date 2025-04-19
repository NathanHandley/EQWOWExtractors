using System.Runtime.CompilerServices;
using System.Text;

namespace EQWOWPregenScripts
{
    internal class QuestExtractor
    {
        private string WorkingQuestRootFolder = "E:\\ConverterData\\Quests";
        private string OutputQuestFile = "E:\\ConverterData\\Quests\\Quests.csv";

        public void ExtractQuests()
        {
            string outputHeaderLine = "zone_shortname|questgiver_name";
            List<string> outputLines = new List<string>();
            outputLines.Add(outputHeaderLine);

            string zoneQuestFolderRoot = Path.Combine(WorkingQuestRootFolder, "zonequests");
            string[] zoneFolders = Directory.GetDirectories(zoneQuestFolderRoot);
            foreach(string zoneFolder in zoneFolders)
            {
                // Shortname
                string zoneShortName = Path.GetFileName(zoneFolder);

                string[] questNPCFiles = Directory.GetFiles(zoneFolder, "*.lua");
                foreach(string questNPCFile in questNPCFiles)
                {
                    // Questgiver name
                    string questgiverName = Path.GetFileNameWithoutExtension(questNPCFile);

                    // Grab the lines of text
                    List<string> lines = new List<string>();
                    using (var sr = new StreamReader(questNPCFile))
                    {
                        string? curLine;
                        Dictionary<int, int> indexCounts = new Dictionary<int, int>();
                        while ((curLine = sr.ReadLine()) != null)
                        {
                            if (curLine != null)
                                lines.Add(curLine);
                        }
                    }

                    // Look far any quests
                    foreach(string line in lines)
                    {



                        // Output the found quest
                        StringBuilder outputLineSB = new StringBuilder();
                        outputLineSB.Append(zoneShortName);
                        outputLineSB.Append("|");
                        outputLineSB.Append(questgiverName);
                        outputLines.Add(outputLineSB.ToString());
                    }                   
                }
            }

            // Output the quest file
            if (File.Exists(OutputQuestFile))
                File.Delete(OutputQuestFile);
            using (var outputFile = new StreamWriter(OutputQuestFile))
                foreach (string outputLine in outputLines)
                    outputFile.WriteLine(outputLine);
        }
    }
}
