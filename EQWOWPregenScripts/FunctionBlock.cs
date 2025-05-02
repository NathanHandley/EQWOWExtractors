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

using EQWOWPregenScripts.Quests;

namespace EQWOWPregenScripts
{
    internal class FunctionBlock
    {
        private string NpcName = string.Empty;
        private string ZoneShortName = string.Empty;
        public string FunctionName = string.Empty;
        public List<string> BlockLines = new List<string>();

        public void LoadStartingAtLine(List<string> inputLines, int startingLineIndex, string npcName, string zoneShortName, ref List<ExceptionLine> exceptionLines,
            out int readLineCount)
        {
            NpcName = npcName;
            ZoneShortName = zoneShortName;

            readLineCount = 0;
            string startLine = inputLines[startingLineIndex].Split("--")[0].Trim();
            if (startingLineIndex >= inputLines.Count)
            {
                exceptionLines.Add(new ExceptionLine(npcName, zoneShortName, "Invalid startingLineIndex in FunctionBlock Load", startingLineIndex, string.Empty));
                return;
            }
            if (startLine.StartsWith("function") == false)
            {
                exceptionLines.Add(new ExceptionLine(npcName, zoneShortName, "Line is not a function line", startingLineIndex, inputLines[startingLineIndex]));
                return;
            }

            int scopeSteps = 0;
            for (int i = startingLineIndex; i < inputLines.Count; i++)
            {
                // Grab the line, removing comments and cap whitespaces
                string curLine = inputLines[i].Split("--")[0].Trim();
                BlockLines.Add(curLine);
                readLineCount++;

                // Function line
                if (curLine.StartsWith("function "))
                {
                    FunctionName = curLine.Split("function ")[1];
                    scopeSteps++;
                    continue;
                }
                else if (curLine.StartsWith("if"))
                    scopeSteps++;
                else if (curLine.StartsWith("while"))
                    scopeSteps++;
                else if (curLine.StartsWith("for"))
                    scopeSteps++;

                if (curLine.StartsWith("end "))
                    scopeSteps--;
                else if (curLine.EndsWith("end"))
                    scopeSteps--;

                // When all scope is resolved, exit
                if (scopeSteps == 0)
                    return;
            }

            exceptionLines.Add(new ExceptionLine(npcName, zoneShortName, "Could not find an end line for function", startingLineIndex, inputLines[startingLineIndex]));
            return;
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

        public List<Quest> ExtractQuests(ref List<ExceptionLine> exceptionLines)
        {
            if (NpcName == string.Empty)
                throw new Exception("Not loaded");

            List<Quest> extractedQuests = new List<Quest>();
            Quest? currentQuest = null;
            Dictionary<string, string> foundTextLinesByTextVariableName = new Dictionary<string, string>();
            for (int i = 0; i < BlockLines.Count; i++)
            {
                string line = BlockLines[i]; // Dump the comments

                // If requirements are found, then this starts a new quest
                if (line.Contains("check_turn_in"))
                {
                    // If rewards haven't been found but an open quest was started, then reset the block.  Probably just a text response.
                    if (currentQuest != null)
                    {
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "check_turn_in found before finishing rewards on prior possible quest", i, line));
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
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "check_turn_in has 'or' conditional", i, line));
                        continue;
                    }
                    if (line.Contains("not"))
                    {
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "check_turn_in has 'not'", i, line));
                        continue;
                    }
                    if (StringHelper.StringHasTwoFragments(line, " and "))
                    {
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "check_turn_in has two 'and'", i, line));
                        continue;
                    }

                    // Start a new quest then
                    currentQuest = new Quest(ZoneShortName, NpcName);
                    currentQuest.MinimumExpansion = minimumExpansion;

                    // Required Items
                    List<int> requiredItems = new List<int>();
                    foreach (int requiredItemID in GetRequiredItemIDsFromLine(line))
                    {
                        bool foundExistingItemID = false;
                        for (int ii = 0; ii < currentQuest.RequiredItemIDs.Count; ii++)
                        {
                            if (currentQuest.RequiredItemIDs[ii] == requiredItemID)
                            {
                                currentQuest.RequiredItemCounts[ii]++;
                                foundExistingItemID = true;
                                break;
                            }
                        }
                        if (foundExistingItemID == false)
                        {
                            currentQuest.RequiredItemIDs.Add(requiredItemID);
                            currentQuest.RequiredItemCounts.Add(1);
                        }
                    }

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
                        string textVariableName = StringHelper.GetTextVariableNameFromLine(line);
                        if (foundTextLinesByTextVariableName.ContainsKey(textVariableName) == false)
                            exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "found a text line named " + textVariableName + " but it wasn't defined", i, line));
                        else
                            currentQuest.RequestText = StringHelper.ConvertText(foundTextLinesByTextVariableName[textVariableName]);
                    }
                }

                // Say line
                else if (line.Contains("e.self:Say"))
                {
                    if (currentQuest == null)
                    {
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "Say line found but quest was null", i, line));
                        continue;
                    }

                    string formattedLine = StringHelper.ConvertText(line.Replace("e.self:Say(\"", ""));
                    if (currentQuest.RewardText.Length == 0)
                        currentQuest.RewardText = formattedLine;
                    else
                        currentQuest.RewardText = string.Concat(currentQuest.RequestText, "$B$B", formattedLine);
                }

                // Text Line
                else if (line.Contains("local text") == true)
                {
                    // Grab variable name
                    string[] parts = line.Split(" ");
                    string variableName = parts[1];
                    string value = line.Substring(line.IndexOf('"'));
                    if (foundTextLinesByTextVariableName.ContainsKey(variableName))
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "Found a second text variable named " + variableName + " while parsing a quest", i, line));
                    else
                        foundTextLinesByTextVariableName.Add(variableName, value);
                }

                else if (line.Contains("SummonCursorItem"))
                {
                    exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "SummonCursorItem found but unhandled", i, line));
                }

                // Add Faction
                else if (line.Contains("e.other:Faction(e.self"))
                {
                    if (currentQuest == null)
                    {
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "Faction add found but there was no check_turn_in proceeding it", i, line));
                        continue;
                    }

                    QuestRewardFactionChange? factionChange = QuestRewardFactionChange.GetFactionChangeFromLine(line, ZoneShortName, NpcName, ref exceptionLines);
                    if (factionChange == null)
                    {
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "Faction add line found, but could not parse", i, line));
                        continue;
                    }
                    currentQuest.Reward.FactionChanges.Add(factionChange);
                }

                // Reward
                else if (line.Contains("QuestReward") == true)
                {
                    if (currentQuest == null)
                    {
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "QuestReward found but there was no check_turn_in proceeding it", i, line));
                        continue;
                    }

                    QuestReward? questReward = QuestReward.GetQuestRewardFromLine(line, ZoneShortName, NpcName, ref exceptionLines);
                    if (questReward == null)
                    {
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "Rewards line found, but could not parse", i, line));
                        currentQuest = null;
                        continue;
                    }
                    currentQuest.Reward.ItemRewards = questReward.ItemRewards;
                    currentQuest.Reward.Money = questReward.Money;
                    currentQuest.Reward.Experience = questReward.Experience;
                    extractedQuests.Add(currentQuest);
                    currentQuest = null;
                }
            }

            if (currentQuest != null)
            {
                extractedQuests.Add(currentQuest);
            }

            return extractedQuests;
        }
    }
}
