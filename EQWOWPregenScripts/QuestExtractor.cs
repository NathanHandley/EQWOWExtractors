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
//      - function event_timer(e)
// Spawn Events
//  - airplane\#Master_of_elements
//      - function event_spawn(e)
//      - eq.depop()
//  - airplane\Magnus_Frinon
//      - eq.spawn2
//  - airplane\a_thunder_spirit_princess
//      - eq.unique_spawn(71073,0,0,287.9,662.5,-54.1,109.3); -- NPC: Gkzzallk
// Subconditionals for rewards
//  - akanon\Manik_Compolten
//      - if(math.random(100) < 20) then
// Random and Choose Random
//  - akanon\Manik_Compolten
//      - math.random(0,10)
//      - eq.ChooseRandom
// elseif on check_turn_in
//  - airplane\Inte_Akera
// Say events that summon items
//  - airplane\Cilin_Spellsinger
//      - e.other:SummonCursorItem(18542); -- The Flute
// Combat events (event_combat(e)
//  - airplane\Sirran_the_lunatic
// Multiple of the same quest item
//  - akanon\Larkon_Theardor
//      - if(e.other:GetFactionValue(e.self) >= -100 and item_lib.check_turn_in(e.self, e.trade, {item1 = 13077, item2 = 13077},1,text)) then -- Minotaur Horn x 2
// Hail
//  - cabeast\Hierophant_Oxyn
//      - if(e.message:findi("hail")) then
// Name Extraction
//  - airplane\a_thunder_spirit_princess
//      - e.self:Say("Thank you, " .. e.other:GetCleanName() .. ". I will tell him to expect visitors.");
// Money as a requirement
//  - airplane\a_thunder_spirit_princess
//      - if(item_lib.check_turn_in(e.self, e.trade, {gold = 10})) then
// Death Events
//  - airplane\a_thunder_spirit_princess
//      - function event_death_complete(e)
// Rewards to cursor
//  - global\Priest_of_Discord
//      - e.other:SummonCursorItem(18700); -- Item: Tome of Order and Discord

using System.Text;
using System.Text.RegularExpressions;

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
            public FactionChange() { }
            public FactionChange(int factionId, int changeAmount)   
            {
                FactionId = factionId;
                ChangeAmount = changeAmount;
            }
        }

        public class ItemReward
        {
            public int ID = 0;
            public int Count = 1;
            public float Chance = 100f;
            public ItemReward(int id, int count, float chance)
            {
                ID = id;
                Count = count;
                Chance = chance;
            }
        }

        public class QuestReward
        {
            public int Money; // Is this in silver?  Looks like it
            public Dictionary<int, int> ItemCountByIDs = new Dictionary<int, int>();
            public List<ItemReward> ItemRewards { get; } = new List<ItemReward>();
            public int Experience; 
            public List<FactionChange> FactionChanges = new List<FactionChange>();
            public bool AttackPlayerOnTurnin = false; // True if NPC attacks player after turn-in

            public void AddItemReward(int itemID, int count = 1, float chance = 100f)
            {
                foreach (ItemReward itemReward in ItemRewards)
                    if (itemReward.ID == itemID)
                    {
                        itemReward.Count += count;
                        return;
                    }
                ItemRewards.Add(new ItemReward(itemID, count, chance));
            }
        }

        public class Quest
        {
            public string Name = string.Empty;
            public string ZoneShortName = string.Empty;
            public string QuestgiverName = string.Empty;
            public List<int> RequiredItems = new List<int>(); // Item IDs for turn-in
            public QuestReward Reward = new QuestReward();
            public int MinimumFaction= -1; // Minimum faction value required
            public int MinimumExpansion = -1;
            public string RequestText = string.Empty;
            public string RewardText = string.Empty;
            public string RewardEmote = string.Empty;            

            public Quest(string zoneShortName, string questGiverName)
            {
                ZoneShortName = zoneShortName;
                QuestgiverName = questGiverName;
            }
        }

        static private bool StringHasTwoFragments(string inputString, string fragment)
        {
            if (string.IsNullOrEmpty(inputString) || string.IsNullOrEmpty(fragment))
                return false;

            int firstIndex = inputString.IndexOf(fragment, StringComparison.OrdinalIgnoreCase);
            if (firstIndex == -1)
                return false;

            // Search for the second occurrence after the first
            int secondIndex = inputString.IndexOf(fragment, firstIndex + fragment.Length, StringComparison.OrdinalIgnoreCase);
            return secondIndex != -1;
        }

        private List<string> GetParameterValuesAfterParameterToken(string line, string parameterToken)
        {
            // Match content within { } preceded by the paremeter token = 
            string matchString = parameterToken + @"\s*=\s*\{([^}]+)\}";
            var match = Regex.Match(line, matchString);
            if (match.Success == false)
                return new List<string>();

            // Get the content inside the braces
            string valuesStr = match.Groups[1].Value;
            string[] values = valuesStr.Split(",");
            List<string> returnArray = new List<string>();
            foreach (string value in values)
                returnArray.Add(value.Trim());
            return returnArray;
        }

        private string GetParameterValueAfterParameterToken(string line, string parameterToken)
        {
            // Match the key followed by = and a number
            var match = Regex.Match(line, $@"{parameterToken}\s*=\s*(\d+)");
            if (match.Success == false)
                return string.Empty;

            return match.Groups[1].Value;
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

        private string GetTextVariableNameFromLine(string line)
        {
            string[] lineBlocks = line.Replace("then", "").Trim().Split(",");
            for (int i = 0; i < lineBlocks.Length; i++)
            {
                if (lineBlocks[i].Contains("text"))
                {
                    return lineBlocks[i].Replace(")", "").Trim();
                }
            }
            return string.Empty;
        }

        private QuestReward? GetQuestRewardFromLine(string line, string zoneShortName, string questgiverName, ref List<ExceptionLine> exceptionLines)
        {
            QuestReward? returnReward = new QuestReward();
            // Hard-coded
            if (zoneShortName == "innothule" && questgiverName == "Lynuga")
            {
                returnReward.FactionChanges.Add(new FactionChange(222,5));
                returnReward.FactionChanges.Add(new FactionChange(308, -5));
                returnReward.FactionChanges.Add(new FactionChange(235, -5));
                returnReward.Experience = 100;
                returnReward.AddItemReward(10082, 1, 5);
                returnReward.AddItemReward(10080, 1, 47.5f);
                returnReward.AddItemReward(10081, 1, 47.5f);
                return returnReward;
            }

            // Strip comments
            string workingLine = line.Split("--")[0];

            // TODO: Handle these conditions
            if (workingLine.Contains(" or" ))
                return null;
            if (workingLine.Contains("random"))
                return null;
            if (workingLine.Contains("ChooseRandom"))
                return null;
            if (workingLine.Contains("GetFaction"))
                return null;
            if (workingLine.Contains("silver"))
                return null;

            // There are two reward line patterns:
            // - One that is a normal parameter list separated by comma
            // - One that uses bracket notation { }
            if (workingLine.Contains("{"))
            {
                // Look for all possible keywords
                // Single item
                if (workingLine.Contains("itemid") == true)
                {
                    string paramaterValue = GetParameterValueAfterParameterToken(workingLine, "itemid");
                    int itemID = int.Parse(paramaterValue);
                    returnReward.AddItemReward(itemID);
                }

                // Group of items
                if (workingLine.Contains("items"))
                {
                    List<string> paramaterValues = GetParameterValuesAfterParameterToken(workingLine, "items");
                    foreach(string paramaterValue in paramaterValues)
                    {
                        int itemID = int.Parse(paramaterValue);
                        returnReward.AddItemReward(itemID);
                    }
                }

                // Experience
                if (workingLine.Contains("exp"))
                {
                    string paramaterValue = GetParameterValueAfterParameterToken(workingLine, "exp");
                    returnReward.Experience = int.Parse(paramaterValue);
                }

                // Money
                int copper = 0;
                if (workingLine.Contains("copper"))
                {
                    string paramaterValue = GetParameterValueAfterParameterToken(workingLine, "copper");
                    copper = int.Parse(paramaterValue);
                }
                int gold = 0;
                if (workingLine.Contains("gold"))
                {
                    string paramaterValue = GetParameterValueAfterParameterToken(workingLine, "gold");
                    gold = int.Parse(paramaterValue);
                }
                int platinum = 0;
                if (workingLine.Contains("platinum"))
                {
                    string paramaterValue = GetParameterValueAfterParameterToken(workingLine, "platinum");
                    platinum = int.Parse(paramaterValue);
                }
                returnReward.Money = copper + (gold * 10000) + (platinum * 1000000);
            }
            else
            {
                // Process the line based on length of blocks
                string rewardDataOnly = workingLine.Replace("e.other:QuestReward(e.self,", "").Trim().Split(")")[0];
                string[] blocks = rewardDataOnly.Split(",");
                if (blocks.Length < 4)
                {
                    exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Reward line split into " + blocks.Length + " blocks, which is unhandled", 0, line));
                    return null;
                }
                int copper = int.Parse(blocks[0]);
                int silver = int.Parse(blocks[1]);
                int gold = int.Parse(blocks[2]);
                int platinum = int.Parse(blocks[3]);
                returnReward.Money = copper + (silver * 100) + (gold * 10000) + (platinum * 1000000);
                if (blocks.Length > 4)
                {
                    int itemID = int.Parse(blocks[4]);
                    if (itemID != 0)
                        returnReward.AddItemReward(itemID);
                }
                if (blocks.Length > 5)
                    returnReward.Experience = int.Parse(blocks[5]);
                if (blocks.Length > 6)
                {
                    exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Reward line split into " + blocks.Length + " blocks, which is unhandled", 0, line));
                    return null;
                }
            }

            return returnReward;
        }


        private List<Quest> ParseQuests(List<string> lines, string zoneShortName, string questgiverName, ref List<ExceptionLine> exceptionLines)
        {
            List<Quest> returnQuests = new List<Quest>();
            Quest? currentQuest = null;
            Dictionary<string, string> foundTextLinesByTextVariableName = new Dictionary<string, string>();
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Split("--")[0].Trim(); // Dump the comment

                // If requirements are found, then this starts a new quest
                if (line.Contains("check_turn_in"))
                {
                    // If rewards haven't been found but an open quest was started, then reset the block.  Probably just a text response.
                    if (currentQuest != null)
                    {
                        exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "check_turn_in found before finishing rewards on prior possible quest", i, line));
                        currentQuest = null;
                        continue;
                    }

                    // Look for expansion limits
                    int minimumExpansion = -1;
                    if (line.Contains("eq.is_the_scars_of_velious_enabled() and "))
                    {
                        minimumExpansion = 2;
                        line = line.Replace("eq.is_the_scars_of_velious_enabled() and ", "");
                    }

                    // Multi-part conditionals should be skipped and done manually
                    if (line.Contains(" or "))
                    {
                        exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "check_turn_in has 'or' conditional", i, line));
                        continue;
                    }
                    //if (line.Contains("text"))
                    //{
                    //    exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "check_turn_in has 'text'", i, line));
                    //    continue;
                    //}
                    if (line.Contains("not"))
                    {
                        exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "check_turn_in has 'not'", i, line));
                        continue;
                    }
                    if (StringHasTwoFragments(line, " and "))
                    {
                        exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "check_turn_in has two 'and'", i, line));
                        continue;
                    }

                    // Start a new quest then
                    currentQuest = new Quest(zoneShortName, questgiverName);
                    currentQuest.MinimumExpansion = minimumExpansion;

                    // Required Items
                    currentQuest.RequiredItems.AddRange(GetRequiredItemIDsFromLine(line));

                    // Required Faction
                    if (line.Contains("Faction"))
                    {
                        // Faction is always first, it seems
                        string workingLine = line.Trim().Replace("if", "").TrimStart();
                        workingLine = workingLine.Trim().Replace("else", "").TrimStart();
                        string[] blocks = workingLine.Split(" ");

                        // Faction level
                        if (workingLine.Contains("GetFaction("))
                        {
                            int minFactionLevel = int.Parse(blocks[2].Replace(")", ""));
                            if (blocks[1] == "<")
                                minFactionLevel--;
                            currentQuest.MinimumFaction = GetFactionValueFromFactionLevel(minFactionLevel);
                        }

                        // Faction value
                        else
                        {
                            int minFactionValue = int.Parse(blocks[2].Replace(")", ""));
                        }
                    }

                    // Text line
                    if (line.Contains("text"))
                    {
                        string textVariableName = GetTextVariableNameFromLine(line);
                        if (foundTextLinesByTextVariableName.ContainsKey(textVariableName) ==  false)
                            exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "found a text line named " + textVariableName + " but it wasn't defined", i, line));
                        else
                            currentQuest.RequestText = foundTextLinesByTextVariableName[textVariableName];
                    }
                }
                
                // Text Line
                else if(line.Contains("local text") == true)
                {
                    // Grab variable name
                    string[] parts = line.Split(" ");
                    string variableName = parts[1];
                    string value = line.Substring(line.IndexOf('"'));
                    if (foundTextLinesByTextVariableName.ContainsKey(variableName))
                        exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Found a second text variable named " + variableName + " while parsing a quest", i, line));
                    else
                        foundTextLinesByTextVariableName.Add(variableName, value);
                }

                // Reward
                else if (line.Contains("QuestReward") == true)
                {
                    if (currentQuest == null)
                    {
                        exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "QuestReward found but there was no check_turn_in proceeding it", i, line));
                        continue;
                    }

                    QuestReward? questReward = GetQuestRewardFromLine(line, zoneShortName, questgiverName, ref exceptionLines);
                    if (questReward == null)
                    {
                        exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Rewards line found, but could not parse", i, line));
                        currentQuest = null;
                        continue;
                    }
                    currentQuest.Reward = questReward;
                    returnQuests.Add(currentQuest);
                    currentQuest = null;
                }
            }

            if (currentQuest != null)
                returnQuests.Add(currentQuest);
            return returnQuests;
        }

        static int GetFactionValueFromFactionLevel(int factionLevel)
        {
            switch (factionLevel)
            {
                case 1: return 1100; // Ally
                case 2: return 750; // Warmly
                case 3: return 500; // Kindly
                case 4: return 100; // Amiably
                case 5: return 0; // Indifferently
                case 6: return -100; // Apprhensively
                default: throw new Exception("Unhandled faction level of " + factionLevel);
            }
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
                        List<Quest> curGiverQuests = ParseQuests(eventTradeLines, zoneShortName, questgiverName, ref outputExceptionLines);
                        for (int i = 0; i < curGiverQuests.Count; i++)
                        {
                            Quest quest = curGiverQuests[i];
                            quest.Name = questgiverName.Replace('_', ' ').Replace("#", "") + " Quest " + i;
                            quests.Add(quest);
                        }
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
            StringBuilder outputHeaderSB = new StringBuilder();
            outputHeaderSB.Append("zone_shortname|questgiver_name|quest_name|req_repmin|req_item1|req_item2|req_item3|req_item4|req_item5|req_item6|reward_money|reward_exp|");
            for (int i = 1; i <= 20; i++)
            {
                outputHeaderSB.Append("reward_item_ID");
                outputHeaderSB.Append(i);
                outputHeaderSB.Append("|");
                outputHeaderSB.Append("reward_item_count");
                outputHeaderSB.Append(i);
                outputHeaderSB.Append("|");
                outputHeaderSB.Append("reward_item_chance");
                outputHeaderSB.Append(i);
                outputHeaderSB.Append("|");
            }
            outputHeaderSB.Append("reward_faction1ID|reward_faction1Amt|reward_faction2ID|reward_faction2Amt|reward_faction3ID|reward_faction3Amt|reward_faction4ID|reward_faction4Amt|reward_faction5ID|reward_faction5Amt|reward_faction6ID|reward_faction6Amt|attack_player_after_turnin|request_text|min_expansion");
            string outputHeaderLine = outputHeaderSB.ToString();
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
                outputLineSB.Append(quest.Name);
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
                for (int i = 0; i < 20; i++)
                {
                    if (quest.Reward.ItemRewards.Count > i)
                    {
                        outputLineSB.Append(quest.Reward.ItemRewards[i].ID);
                        outputLineSB.Append("|");
                        outputLineSB.Append(quest.Reward.ItemRewards[i].Count);
                        outputLineSB.Append("|");
                        outputLineSB.Append(quest.Reward.ItemRewards[i].Chance);
                        outputLineSB.Append("|");
                    }
                    else
                    {
                        outputLineSB.Append("-1");
                        outputLineSB.Append("|");
                        outputLineSB.Append("-1");
                        outputLineSB.Append("|");
                        outputLineSB.Append("-1");
                        outputLineSB.Append("|");
                    }
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
                outputLineSB.Append(quest.Reward.AttackPlayerOnTurnin == true ? "1" : "0");
                outputLineSB.Append("|");
                outputLineSB.Append(quest.RequestText);
                outputLineSB.Append("|");
                outputLineSB.Append(quest.MinimumExpansion);
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
