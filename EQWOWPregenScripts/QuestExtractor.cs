using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using static EQWOWPregenScripts.QuestExtractor;

namespace EQWOWPregenScripts
{
    internal class QuestExtractor
    {
        private string WorkingQuestRootFolder = "E:\\ConverterData\\Quests";
        private string OutputQuestFile = "E:\\ConverterData\\Quests\\Quests.csv";
        private string OutputExceptionQuestFile = "E:\\ConverterData\\Quests\\Exceptions.csv";

        public class ExceptionLine
        {
            public ExceptionLine(string questgiverName, string zoneShortName, string exceptionReason, string text)
            {
                QuestgiverName = questgiverName;
                ZoneShortName = zoneShortName;
                ExceptionReason = exceptionReason;
                Text = text;
                DataToString(true);
            }
            private string ZoneShortName = string.Empty;
            private string QuestgiverName = string.Empty;
            private string ExceptionReason = string.Empty;
            private string Text = string.Empty;
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
                sb.Append(Text);
                if (forConsole == true)
                    Console.WriteLine(sb.ToString());
                return sb.ToString();
            }
            static public string HeaderToString()
            {
                return "Questgiver_Name|Zone_Shortname|Exception_Reason|Text";
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
            public List<int> RequiredItems = new List<int>(); // Item IDs for turn-in
            public QuestReward Reward = new QuestReward();
            public List<string> Dialogue = new List<string>(); // NPC Say statements
            public int MinimumFaction= -1; // Minimum faction value required
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


        private Quest? ParseQuest(List<string> lines, int startIndex, string zoneShortName, string questGiverName, ref List<ExceptionLine> exceptionLines)
        {
            var quest = new Quest();

            // Operate on all lines or until the end of the quest area
            for (int i = startIndex; i < lines.Count; i++)
            {
                string line = lines[i].Split("--")[0].Trim(); // Dump the comment

                // Required items
                if (line.Contains("check_turn_in"))
                {
                    // Multi-part conditionals should be skipped and done manually
                    if (line.Contains(" or "))
                    {
                        exceptionLines.Add(new ExceptionLine(questGiverName, zoneShortName, "Has 'or' conditional", line));
                        continue;
                    }

                    // For now, just catch any instances where there is a second
                    if (quest.RequiredItems.Count > 0)
                    {
                        exceptionLines.Add(new ExceptionLine(questGiverName, zoneShortName, "Already have required items", line));
                        continue;
                    }
                    else
                        quest.RequiredItems.AddRange(GetRequiredItemIDsFromLine(line));
                }

                if (line.Contains("end"))
                {
                    if (quest.RequiredItems.Count == 0)
                        return null;
                    else
                        return quest;
                }
                //if (line.Contains(""))
            }
            return quest;       

            // Parse faction condition if present (e.g., GetFactionValue(e.self) >= 100 or > 100)
            //var factionConditionMatch = Regex.Match(line, @"GetFactionValue\(e\.self\)\s*(>=|>)\s*(\d+)");
            //if (factionConditionMatch.Success)
            //{
            //    int factionValue = int.Parse(factionConditionMatch.Groups[2].Value);
            //    string operatorStr = factionConditionMatch.Groups[1].Value;
            //    if (operatorStr == ">=" || operatorStr == ">")
            //    {
            //        quest.MinimumFaction = factionValue;
            //    }
            //}

            // Parse required items from check_turn_in
            //var itemMatch = Regex.Match(line, @"check_turn_in\([^,]+,[^{]+{([^}]+)}\)");
            //if (!itemMatch.Success) return null;

            //string requiredItemsStr = itemMatch.Groups[1].Value;
            //var itemPairs = requiredItemsStr.Split(',').Select(s => s.Trim());
            //foreach (var pair in itemPairs)
            //{
            //    var match = Regex.Match(pair, @"item\d+\s*=\s*(\d+)");
            //    if (match.Success)
            //    {
            //        quest.RequiredItems.Add(int.Parse(match.Groups[1].Value));
            //    }
            //}

            // Generate quest name based on items
            //quest.Name = $"Turn in {string.Join(", ", quest.RequiredItems)}";

            // Find associated Say statement (before or after)
            //for (int i = Math.Max(0, startIndex - 5); i < Math.Min(lines.Count, startIndex + 5); i++)
            //{
            //    string dialogueLine = lines[i].Trim();
            //    if (dialogueLine.Contains("e.self:Say"))
            //    {
            //        var sayMatch = Regex.Match(dialogueLine, @"e\.self:Say\(""([^""]+)""");
            //        if (sayMatch.Success)
            //        {
            //            quest.Dialogue.Add(sayMatch.Groups[1].Value);
            //        }
            //    }
            //}

            // Parse rewards and faction changes
            //for (int i = startIndex + 1; i < lines.Count; i++)
            //{
            //    line = lines[i].Trim();

            //    // Stop if we hit another check_turn_in (new quest block)
            //    if (line.Contains("check_turn_in") && line.Contains("item1"))
            //    {
            //        break;
            //    }

            //    // Parse faction changes
            //    if (line.Contains("e.other:Faction"))
            //    {
            //        var factionMatch = Regex.Match(line, @"e\.other:Faction\([^,]+,\s*(\d+),\s*([-]?\d+)[^;]*\);\s*--\s*([^)]+)");
            //        if (factionMatch.Success)
            //        {
            //            quest.Reward.FactionChanges.Add(new FactionChange
            //            {
            //                FactionId = int.Parse(factionMatch.Groups[1].Value),
            //                ChangeAmount = int.Parse(factionMatch.Groups[2].Value),
            //                FactionName = factionMatch.Groups[3].Value.Trim()
            //            });
            //        }
            //    }
            //    // Check for attack trigger
            //    else if (line.Contains("eq.attack(e.other:GetName())"))
            //    {
            //        quest.Reward.AttackPlayerOnTurnin = true;
            //    }
            //    // Parse QuestReward
            //    else if (line.Contains("e.other:QuestReward"))
            //    {
            //        // Match QuestReward with 6 arguments (items, exp) or 5 arguments (item only)
            //        var rewardMatch = Regex.Match(line, @"e\.other:QuestReward\([^,]+,(\d+),(\d+),(\d+),(\d+),([^,]+)(?:,(\d+))?\)");
            //        if (rewardMatch.Success)
            //        {
            //            quest.Reward.Money = int.Parse(rewardMatch.Groups[2].Value); // Silver (second argument)

            //            string rewardItemsStr = rewardMatch.Groups[5].Value.Trim();
            //            // Check if the last group (exp) exists
            //            if (rewardMatch.Groups.Count > 6 && !string.IsNullOrEmpty(rewardMatch.Groups[6].Value))
            //            {
            //                quest.Reward.Experience = int.Parse(rewardMatch.Groups[6].Value);
            //                // Parse items (can be a single ID or a list like {id1, id2})
            //                if (rewardItemsStr.StartsWith("{") && rewardItemsStr.EndsWith("}"))
            //                {
            //                    rewardItemsStr = rewardItemsStr.Trim('{', '}');
            //                    quest.Reward.Items.AddRange(rewardItemsStr.Split(',').Select(s => int.Parse(s.Trim())));
            //                }
            //                else if (int.TryParse(rewardItemsStr, out int itemId))
            //                {
            //                    quest.Reward.Items.Add(itemId);
            //                }
            //            }
            //            else
            //            {
            //                // No exp, last argument is the item
            //                if (int.TryParse(rewardItemsStr, out int itemId))
            //                {
            //                    quest.Reward.Items.Add(itemId);
            //                }
            //                quest.Reward.Experience = 0; // Default to 0 if no exp specified
            //            }
            //        }
            //        // Handle table-based QuestReward (e.g., {silver = ..., items = {...}, exp = ...})
            //        else
            //        {
            //            var tableRewardMatch = Regex.Match(line, @"e\.other:QuestReward\([^,]+,\s*{\s*silver\s*=\s*(?:math\.random\()?(\d+)(?:\))?\s*,items\s*=\s*{([^}]+)},exp\s*=\s*(\d+)\s*}\)");
            //            if (tableRewardMatch.Success)
            //            {
            //                quest.Reward.Money = int.Parse(tableRewardMatch.Groups[1].Value); // Silver
            //                quest.Reward.Experience = int.Parse(tableRewardMatch.Groups[3].Value); // Exp

            //                // Parse items from the items table
            //                string rewardItemsStr = tableRewardMatch.Groups[2].Value.Trim();
            //                quest.Reward.Items.AddRange(rewardItemsStr.Split(',').Select(s => int.Parse(s.Trim())));
            //            }
            //        }
            //        break; // Stop after QuestReward to avoid collecting unrelated faction changes
            //    }
            //}

            return quest;
        }

        public void ExtractQuests()
        {
            string outputHeaderLine = "zone_shortname|questgiver_name|quest_name|req_repmin|req_item1|req_item2|req_item3|req_item4|req_item5|req_item6|reward_money|reward_exp|reward_item1|reward_item2|reward_item3|reward_item4|reward_item5|reward_item6|reward_faction1ID|reward_faction1Amt|reward_faction2ID|reward_faction2Amt|reward_faction3ID|reward_faction3Amt|reward_faction4ID|reward_faction4Amt|reward_faction5ID|reward_faction5Amt|reward_faction6ID|reward_faction6Amt|reward_dialog|attack_player_after_turnin";
            List<string> outputQuestLines = new List<string>();
            outputQuestLines.Add(outputHeaderLine);
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

                    // Skip certain quest givers
                    //if (questgiverName == "Jusean_Evanesque")
                    //{
                    //    outputExceptionLines.Add("qeynos|Jusean_Evanesque");
                    //    continue;
                    //}

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
                    var quests = new List<Quest>();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        string line = lines[i];
                        if (line.Contains("check_turn_in") && line.Contains("item1"))
                        {
                            var quest = ParseQuest(lines, i, zoneShortName, questgiverName, ref outputExceptionLines);
                            if (quest != null)
                            {
                                quests.Add(quest);
                            }
                        }
                    }

                    //if (questgiverName == "Sheriff_Roglio")
                    //{
                    //    int x = 5;
                    //    int y = 5;
                    //}

                    for (int qi = 0; qi < quests.Count; qi++)
                    {
                        Quest quest = quests[qi];

                        // Output the found quest
                        StringBuilder outputLineSB = new StringBuilder();
                        outputLineSB.Append(zoneShortName);
                        outputLineSB.Append("|");
                        outputLineSB.Append(questgiverName);
                        outputLineSB.Append("|");
                        outputLineSB.Append(questgiverName.Replace('_', ' ').Replace("#", "") + " Quest " + qi.ToString());
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
                }
            }

            // Output the quest files
            if (File.Exists(OutputQuestFile))
                File.Delete(OutputQuestFile);
            using (var outputFile = new StreamWriter(OutputQuestFile))
                foreach (string outputLine in outputQuestLines)
                    outputFile.WriteLine(outputLine);
            if (File.Exists(OutputExceptionQuestFile))
                File.Delete(OutputExceptionQuestFile);
            using (var outputFile = new StreamWriter(OutputExceptionQuestFile))
            {
                outputFile.WriteLine(ExceptionLine.HeaderToString());
                foreach (ExceptionLine exceptionLine in outputExceptionLines)
                    outputFile.WriteLine(exceptionLine.DataToString());
            }
        }
    }
}
