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

// TODO
// Timer Events
//  - airplane\#Master_of_Elements
//  - function event_timer(e)
// Spawn Events
//  - airplane\#Master_of_elements
//  - function event_spawn(e)

using System.Text;

namespace EQWOWPregenScripts
{
    internal class QuestExtractor
    {
        private string WorkingQuestRootFolder = "E:\\ConverterData\\Quests";
        private string OutputQuestFile = "E:\\ConverterData\\Quests\\Quests.csv";
        private string OutputExceptionQuestFile = "E:\\ConverterData\\Quests\\Exceptions.csv";

        public class ExceptionLine
        {
            public ExceptionLine(string questgiverName, string zoneShortName, string exceptionReason, int lineRow, string lineText)
            {
                QuestgiverName = questgiverName;
                ZoneShortName = zoneShortName;
                ExceptionReason = exceptionReason;
                LineRow = lineRow;
                LineText = lineText;
                DataToString(true);
            }
            private string ZoneShortName = string.Empty;
            private string QuestgiverName = string.Empty;
            private string ExceptionReason = string.Empty;
            private int LineRow;
            private string LineText = string.Empty;
            public string DataToString(bool forConsole = false)
            {
                StringBuilder sb = new StringBuilder();
                string delimeter = "|";
                if (forConsole == true)
                    delimeter = ", ";
                sb.Append(QuestgiverName);
                sb.Append(delimeter);
                sb.Append(ZoneShortName);
                sb.Append(delimeter);
                sb.Append(ExceptionReason);
                sb.Append(delimeter);
                sb.Append(LineRow);
                sb.Append(delimeter);
                sb.Append(LineText);
                if (forConsole == true)
                    Console.WriteLine(sb.ToString());
                return sb.ToString();
            }
            static public string HeaderToString()
            {
                return "Questgiver_Name|Zone_Shortname|Exception_Reason|LineRow|LineText";
            }
        }

        public class FactionChange
        {
            public int FactionId;
            public int ChangeAmount;
        }

        public class QuestReward
        {
            public int Money; // Is this in silver?  Looks like it
            public Dictionary<int, int> ItemCountByIDs = new Dictionary<int, int>();
            public List<int> Items = new List<int>();
            public int Experience; 
            public List<FactionChange> FactionChanges = new List<FactionChange>();
            public bool AttackPlayerOnTurnin = false; // True if NPC attacks player after turn-in
        }

        public class Quest
        {
            public string Name = string.Empty;
            public string ZoneShortName = string.Empty;
            public string QuestgiverName = string.Empty;
            public List<int> RequiredItems = new List<int>(); // Item IDs for turn-in
            public QuestReward Reward = new QuestReward();
            public List<string> Dialogue = new List<string>(); // NPC Say statements
            public int MinimumFaction= -1; // Minimum faction value required

            public Quest(string zoneShortName, string questGiverName)
            {
                ZoneShortName = zoneShortName;
                QuestgiverName = questGiverName;
            }
        }

        private List<int> GetRequiredItemIDsFromLine(string line)
        {
            List<int> requiredItemIDs = new List<int>();
            string[] lineBlocks = line.Split(",");
            for (int i = 0; i < lineBlocks.Length; i++)
            {
                if (lineBlocks[i].Contains("check") == false && lineBlocks[i].Contains("item") == true)
                {
                    lineBlocks[i] = lineBlocks[i].Replace("then", "");
                    lineBlocks[i] = lineBlocks[i].Trim();
                    string[] blockParts = lineBlocks[i].Split(" ");
                    string idString = blockParts[blockParts.Length - 1].Replace("}", "").Replace(")", "").Replace("=", "");
                    if (idString.Trim().Length != 0)
                    {
                        int itemID = int.Parse(idString);
                        requiredItemIDs.Add(itemID);
                    }
                }
            }
            return requiredItemIDs;
        }


        private Quest? ParseQuest(List<string> lines, string zoneShortName, string questGiverName, ref List<ExceptionLine> exceptionLines)
        {
            var quest = new Quest(zoneShortName, questGiverName);

            // Grab the relevant lines
            int rewardsLineIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Split("--")[0].Trim(); // Dump the comment

                // Rewards
                if (line.Contains("QuestReward") == true)
                {
                    if (rewardsLineIndex == -1)
                        rewardsLineIndex = i;
                    else
                    {
                        exceptionLines.Add(new ExceptionLine(questGiverName, zoneShortName, "Additional QuestReward line found", i, line));
                        continue;
                    }
                }
            }

            // Operate on all lines or until the end of the quest area
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Split("--")[0].Trim(); // Dump the comment

                // Required items
                if (line.Contains("check_turn_in"))
                {
                    // Multi-part conditionals should be skipped and done manually
                    if (line.Contains(" or "))
                    {
                        exceptionLines.Add(new ExceptionLine(questGiverName, zoneShortName, "Has 'or' conditional", i, line));
                        continue;
                    }

                    // For now, just catch any instances where there is a second
                    if (quest.RequiredItems.Count > 0)
                    {
                        exceptionLines.Add(new ExceptionLine(questGiverName, zoneShortName, "Already have required items", i,line));
                        continue;
                    }
                    else
                        quest.RequiredItems.AddRange(GetRequiredItemIDsFromLine(line));
                }
            }
            return quest;
        }

        static protected List<string> GetEventTradeBlock(string questGiverName, string zoneShortName, string fileFullPath, ref List<ExceptionLine> exceptionLines)
        {
            List<string> eventTradeLines = new List<string>();

            // Grab lines to return by looking for the function container
            bool startLineFound = false;
            int blockStartCount = 0;
            using (var sr = new StreamReader(fileFullPath))
            {
                string? curLine;
                while ((curLine = sr.ReadLine()) != null)
                {
                    // Start the primary container
                    if (curLine.TrimStart().StartsWith("function event_trade(e)") == true)
                    {
                        startLineFound = true;
                        blockStartCount++;
                        continue;
                    }

                    // No primary container found yet, so jump to next line without processing anything
                    if (startLineFound == false)
                        continue;

                    // All scope subcontainers should start with an if
                    if (curLine.TrimStart().StartsWith("if") == true)
                        blockStartCount++;

                    // All scopes should end with end
                    if (curLine.TrimStart().StartsWith("end") == true)
                    {
                        blockStartCount--;
                        if (blockStartCount == 0)
                            return eventTradeLines;
                    }

                    // If we got here, then this is in-context data
                    eventTradeLines.Add(curLine);
                }
            }

            if (startLineFound == true)
            {
                exceptionLines.Add(new ExceptionLine(questGiverName, zoneShortName, "Start block found but not fully terminated", 0, ""));
                return new List<string>();
            }

            return new List<string>();
        }


        public void ExtractQuests()
        {
            List<Quest> quests = new List<Quest>();
            List<ExceptionLine> outputExceptionLines = new List<ExceptionLine>();

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
                        while ((curLine = sr.ReadLine()) != null)
                        {
                            if (curLine != null)
                                lines.Add(curLine);
                        }
                    }

                    // Look far any quests if there is a turnin block
                    List<string> eventTradeLines = GetEventTradeBlock(questgiverName, zoneShortName, questNPCFile, ref outputExceptionLines);
                    if (eventTradeLines.Count > 0)
                    {
                        Quest? quest = ParseQuest(eventTradeLines, zoneShortName, questgiverName, ref outputExceptionLines);
                        if (quest != null)
                            quests.Add(quest);
                    }                    
                }
            }

            OutputQuests(quests);
            
            if (File.Exists(OutputExceptionQuestFile))
                File.Delete(OutputExceptionQuestFile);
            using (var outputFile = new StreamWriter(OutputExceptionQuestFile))
            {
                outputFile.WriteLine(ExceptionLine.HeaderToString());
                foreach (ExceptionLine exceptionLine in outputExceptionLines)
                    outputFile.WriteLine(exceptionLine.DataToString());
            }
        }
    
        private void OutputQuests(List<Quest> quests)
        {
            string outputHeaderLine = "zone_shortname|questgiver_name|quest_name|req_repmin|req_item1|req_item2|req_item3|req_item4|req_item5|req_item6|reward_money|reward_exp|reward_item1|reward_item2|reward_item3|reward_item4|reward_item5|reward_item6|reward_faction1ID|reward_faction1Amt|reward_faction2ID|reward_faction2Amt|reward_faction3ID|reward_faction3Amt|reward_faction4ID|reward_faction4Amt|reward_faction5ID|reward_faction5Amt|reward_faction6ID|reward_faction6Amt|reward_dialog|attack_player_after_turnin";
            List<string> outputQuestLines = new List<string>();
            outputQuestLines.Add(outputHeaderLine);

            for (int qi = 0; qi < quests.Count; qi++)
            {
                Quest quest = quests[qi];

                // Output the found quest
                StringBuilder outputLineSB = new StringBuilder();
                outputLineSB.Append(quest.ZoneShortName);
                outputLineSB.Append("|");
                outputLineSB.Append(quest.QuestgiverName);
                outputLineSB.Append("|");
                outputLineSB.Append(quest.QuestgiverName.Replace('_', ' ').Replace("#", "") + " Quest " + qi.ToString());
                outputLineSB.Append("|");
                outputLineSB.Append(quest.MinimumFaction);
                outputLineSB.Append("|");
                for (int i = 0; i < 6; i++)
                {
                    if (quest.RequiredItems.Count > i)
                        outputLineSB.Append(quest.RequiredItems[i]);
                    else
                        outputLineSB.Append("-1");
                    outputLineSB.Append("|");
                }
                outputLineSB.Append(quest.Reward.Money);
                outputLineSB.Append("|");
                outputLineSB.Append(quest.Reward.Experience);
                outputLineSB.Append("|");
                for (int i = 0; i < 4; i++)
                {
                    if (quest.Reward.Items.Count > i)
                        outputLineSB.Append(quest.Reward.Items[i]);
                    else
                        outputLineSB.Append("-1");
                    outputLineSB.Append("|");
                }
                for (int i = 0; i < 6; i++)
                {
                    if (quest.Reward.FactionChanges.Count > i)
                    {
                        outputLineSB.Append(quest.Reward.FactionChanges[i].FactionId);
                        outputLineSB.Append("|");
                        outputLineSB.Append(quest.Reward.FactionChanges[i].ChangeAmount);
                        outputLineSB.Append("|");
                    }
                    else
                    {
                        outputLineSB.Append("-1");
                        outputLineSB.Append("|");
                        outputLineSB.Append("0");
                        outputLineSB.Append("|");
                    }
                }
                outputLineSB.Append(quest.Dialogue);
                outputLineSB.Append("|");
                outputLineSB.Append(quest.Reward.AttackPlayerOnTurnin == true ? "1" : "0");
                outputQuestLines.Add(outputLineSB.ToString());
            }

            // Output the quest files
            if (File.Exists(OutputQuestFile))
                File.Delete(OutputQuestFile);
            using (var outputFile = new StreamWriter(OutputQuestFile))
                foreach (string outputLine in outputQuestLines)
                    outputFile.WriteLine(outputLine);
        }
    }
}
