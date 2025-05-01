//  Author: Nathan Handley (nathanhandley@protonmail.com)
//  Copyright (c) 2025 Nathan Handley
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace EQWOWPregenScripts
{
    internal class QuestExtractorNew
    {
        private string WorkingQuestRootFolder = "E:\\ConverterData\\Quests";

        public void ExtractQuests()
        {
            //List<Quest> quests = new List<Quest>();
            //List<ExceptionLine> outputExceptionLines = new List<ExceptionLine>();

            //string zoneQuestFolderRoot = Path.Combine(WorkingQuestRootFolder, "zonequests");
            //string[] zoneFolders = Directory.GetDirectories(zoneQuestFolderRoot);
            //foreach (string zoneFolder in zoneFolders)
            //{
            //    // Shortname
            //    string zoneShortName = Path.GetFileName(zoneFolder);

            //    string[] questNPCFiles = Directory.GetFiles(zoneFolder, "*.lua");
            //    foreach (string questNPCFile in questNPCFiles)
            //    {
            //        // Questgiver name
            //        string questgiverName = Path.GetFileNameWithoutExtension(questNPCFile);

            //        // Grab the lines of text
            //        List<string> lines = new List<string>();
            //        using (var sr = new StreamReader(questNPCFile))
            //        {
            //            string? curLine;
            //            while ((curLine = sr.ReadLine()) != null)
            //            {
            //                if (curLine != null)
            //                    lines.Add(curLine);
            //            }
            //        }

            //        // Look far any quests if there is a turnin block
            //        List<string> eventTradeLines = GetEventTradeBlock(questgiverName, zoneShortName, questNPCFile, ref outputExceptionLines);
            //        if (eventTradeLines.Count > 0)
            //        {
            //            List<Quest> curGiverQuests = ParseQuests(eventTradeLines, zoneShortName, questgiverName, ref outputExceptionLines);
            //            for (int i = 0; i < curGiverQuests.Count; i++)
            //            {
            //                Quest quest = curGiverQuests[i];
            //                quest.Name = questgiverName.Replace('_', ' ').Replace("#", "") + " Quest " + i;
            //                quests.Add(quest);
            //            }
            //        }
            //    }
            //}

            //OutputQuests(quests);

            //if (File.Exists(OutputExceptionQuestFile))
            //    File.Delete(OutputExceptionQuestFile);
            //using (var outputFile = new StreamWriter(OutputExceptionQuestFile))
            //{
            //    outputFile.WriteLine(ExceptionLine.HeaderToString());
            //    foreach (ExceptionLine exceptionLine in outputExceptionLines)
            //        outputFile.WriteLine(exceptionLine.DataToString());
            //}
        }
    }
}
