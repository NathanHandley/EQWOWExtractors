﻿//  Author: Nathan Handley (nathanhandley@protonmail.com)
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
        public Dictionary<string, string> LocalVariableValuesByName = new Dictionary<string, string>();
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
                if (curLine.Length == 0)
                {
                    readLineCount++;
                    continue;
                }

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

                if (curLine.StartsWith("local "))
                {
                    string variableLine = curLine;
                    if (curLine.Contains("{"))
                        variableLine = StringHelper.ExtractCurlyBraceContentRaw(inputLines, i, out readLineCount);
                    (string key, string value) variable = StringHelper.GetLocalbleDataFromLine(variableLine);
                    if (variable.key.Length > 0)
                    {
                        if (LocalVariableValuesByName.ContainsKey(variable.key) == true)
                        {
                            exceptionLines.Add(new ExceptionLine(npcName, zoneShortName, "Local variable with name " + variable.key + "already found", i, curLine));
                        }
                        else
                            LocalVariableValuesByName.Add(variable.key, variable.value);
                    }
                }

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

        public bool HasPossibleQuestData()
        {
            foreach (string line in BlockLines)
            {
                if (line.Contains("check_turn_in"))
                    return true;
                if (line.Contains("SummonCursorItem"))
                    return true;
            }

            return false;
        }

        public List<Quest> ExtractQuests(ref List<ExceptionLine> exceptionLines, List<string> variableLines)
        {
            // Pull out the quest blocks to work with and process them
            List<FunctionBlock> questBlocks = GetFunctionBlocksForQuests(ref exceptionLines);
            List<Quest> extractedQuests = new List<Quest>();
            for (int qi = 0; qi < questBlocks.Count; qi++)
            {
                FunctionBlock curQuestBlock = questBlocks[qi];
                Quest? currentQuest = new Quest(ZoneShortName, NpcName);
                for (int i = 0; i < curQuestBlock.BlockLines.Count; i++)
                {
                    string line = curQuestBlock.BlockLines[i];

                    if (line.Contains("e.message:find"))
                    {
                        // Multi-part conditionals should be skipped and done manually
                        if (line.Contains(" not "))
                        {
                            exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "e.message:find has 'not'", i, line));
                            currentQuest = null;
                            break;
                        }
                        if (StringHelper.StringHasTwoFragments(line, " and "))
                        {
                            exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "e.message:find has two 'and'", i, line));
                            currentQuest = null;
                            break;
                        }
                    }

                    // If there is a say and it was a talking block, then that's the request text
                    if (curQuestBlock.FunctionName.Contains("event_say") && line.Contains("self:Say"))
                    {
                        if (currentQuest != null)
                        {
                            if (currentQuest.RequestText.Length > 0)
                                currentQuest.RequestText += "$B" + StringHelper.ConvertText(StringHelper.ExtractMethodParameters(line, "e.self:Say")[0]);
                            else
                                currentQuest.RequestText = StringHelper.ConvertText(StringHelper.ExtractMethodParameters(line, "e.self:Say")[0]);
                        }
                    }

                    // Quest requirements
                    if (line.Contains("check_turn_in"))
                    {
                        // Multi-part conditionals should be skipped and done manually
                        if (line.Contains(" or "))
                        {
                            exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "check_turn_in has 'or' conditional", i, line));
                            currentQuest = null;
                            break;
                        }
                        if (line.Contains("not"))
                        {
                            exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "check_turn_in has 'not'", i, line));
                            currentQuest = null;
                            break;
                        }
                        if (StringHelper.StringHasTwoFragments(line, " and "))
                        {
                            exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "check_turn_in has two 'and'", i, line));
                            currentQuest = null;
                            break;
                        }

                        // Get the parameters out of the turn-in method, and the first two should be ignored
                        List<string> turnInParameters = StringHelper.ExtractMethodParameters(line, "check_turn_in");
                        if (turnInParameters.Count < 3)
                        {
                            exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "check_turn_in has two or less parameters", i, line));
                            currentQuest = null;
                            break;
                        }
                        if (currentQuest == null)
                            continue;

                        // Look for expansion limits
                        int minimumExpansion = -1;
                        if (line.Contains("eq.is_the_scars_of_velious_enabled() and "))
                        {
                            minimumExpansion = 2;
                            line = line.Replace("eq.is_the_scars_of_velious_enabled() and ", "");
                        }
                        currentQuest.MinimumExpansion = minimumExpansion;

                        // Things required are in the 3rd parameter block
                        List<string> handInPairs = StringHelper.ExtractCurlyBraceParameters(turnInParameters[2]);
                        List<int> requiredItems = new List<int>();
                        int requiredCopper = 0;
                        foreach (string handInPair in handInPairs)
                        {
                            string key = handInPair.Split(",")[0].Trim();
                            string value = handInPair.Split(",")[1].Trim();
                            switch (key)
                            {
                                case "item1":   requiredItems.Add(int.Parse(value)); break;
                                case "item2":   requiredItems.Add(int.Parse(value)); break;
                                case "item3":   requiredItems.Add(int.Parse(value)); break;
                                case "item4":   requiredItems.Add(int.Parse(value)); break;
                                case "copper":  requiredCopper += int.Parse(value); break;
                                case "silver":  requiredCopper += int.Parse(value)*10; break;
                                case "gold":    requiredCopper += int.Parse(value)*100; break;
                                case "platinum":requiredCopper += int.Parse(value)*1000; break;
                                default:
                                    {
                                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "check_turn_in has an unhandled parameter key of " + key, i, line));
                                    } break;
                            }
                        }

                        // Items
                        foreach (int requiredItemID in requiredItems)
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

                        // Money
                        if (requiredCopper > 0)
                            currentQuest.RequiredMoneyInCopper = requiredCopper;

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
                            if (line.Contains("text4") && LocalVariableValuesByName.ContainsKey("text4"))
                                currentQuest.NotEnoughItemsText = StringHelper.ConvertText(LocalVariableValuesByName["text4"]);
                            else if (line.Contains("text3") && LocalVariableValuesByName.ContainsKey("text3"))
                                currentQuest.NotEnoughItemsText = StringHelper.ConvertText(LocalVariableValuesByName["text3"]);
                            else if (line.Contains("text2") && LocalVariableValuesByName.ContainsKey("text2"))
                                currentQuest.NotEnoughItemsText = StringHelper.ConvertText(LocalVariableValuesByName["text2"]);
                            else if (line.Contains("text1") && LocalVariableValuesByName.ContainsKey("text1"))
                                currentQuest.NotEnoughItemsText = StringHelper.ConvertText(LocalVariableValuesByName["text1"]);
                            else if (line.Contains("text") && LocalVariableValuesByName.ContainsKey("text"))
                                currentQuest.NotEnoughItemsText = StringHelper.ConvertText(LocalVariableValuesByName["text"]);
                            else
                            {
                                exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "Found a text line but it wasn't handled", i, line));
                                currentQuest = null;
                                break;
                            }   
                        }
                    }

                    // Summoned quest item
                    else if (line.Contains("SummonCursorItem"))
                    {
                        List<string> parameters = StringHelper.ExtractMethodParameters(line, "SummonCursorItem");
                        if (parameters.Count > 1)
                            throw new Exception("beep");
                        currentQuest.Reward.AddItemReward(StringHelper.GetSingleRangedIntFromString(parameters[0], ZoneShortName, NpcName, ref exceptionLines));
                    }

                    // Faction adjustment
                    else if (line.Contains("e.other:Faction(e.self"))
                    {
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
                        QuestReward? questReward = QuestReward.GetQuestRewardFromLine(line, ZoneShortName, NpcName, ref exceptionLines);
                        if (questReward == null)
                        {
                            exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "Rewards line found, but could not parse", i, line));
                            continue;
                        }
                        currentQuest.Reward.ItemRewards = questReward.ItemRewards;
                        currentQuest.Reward.Money = questReward.Money;
                        currentQuest.Reward.Experience = questReward.Experience;
                    }

                    // Everything else is an unhandled reaction
                    else if (currentQuest != null)
                    {
                        currentQuest.ResponseReactionsRaw.Add(line);
                    }
                }
                if (currentQuest != null)
                {
                    currentQuest.CalculateQuestID();
                    extractedQuests.Add(currentQuest);
                }
            }
            return extractedQuests;
        }

        public List<FunctionBlock> GetFunctionBlocksForQuests(ref List<ExceptionLine> exceptionLines)
        {
            List<FunctionBlock> questFunctionBlocks = new List<FunctionBlock>();
            FunctionBlock? curFunctionBlock = null;
            if (FunctionName.Contains("event_trade"))
            {
                for (int i = 0; i < BlockLines.Count - 1; i++)
                {
                    string curLine = BlockLines[i];

                    // This ends a block
                    if (curLine.StartsWith("elseif") || curLine.StartsWith("end"))
                    {
                        if (curFunctionBlock == null)
                            exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "Found what should be the end of a quest, but no quest was started", i, curLine));
                        else
                            questFunctionBlocks.Add(curFunctionBlock);
                        curFunctionBlock = null;
                    }

                    // New quest block
                    if (curLine.Contains("check_turn_in"))
                    {
                        if (curFunctionBlock != null)
                            exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "check_turn_in found before finishing prior quest", i, curLine));
                        curFunctionBlock = new FunctionBlock();
                        curFunctionBlock.FunctionName = FunctionName;
                    }

                    // Skip conditionals
                    else if (curFunctionBlock != null && curLine.Contains("if("))
                    {
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "Conditional found in quest block, so nullifying it to make manually", i, curLine));
                        curFunctionBlock = null;
                    }

                    if (curFunctionBlock != null)
                        curFunctionBlock.BlockLines.Add(curLine);
                }
            }
            else if (FunctionName.Contains("event_say"))
            {
                bool rewardFound = false;
                for (int i = 0; i < BlockLines.Count - 1; i++)
                {
                    string curLine = BlockLines[i];

                    // New quest block
                    if (curLine.Contains("e.message:findi"))
                    {
                        // This is just a dialog item
                        curFunctionBlock = new FunctionBlock();
                        curFunctionBlock.FunctionName = FunctionName;
                    }

                    // This ends a block
                    else if (curLine.StartsWith("elseif") || curLine.StartsWith("end"))
                    {
                        // If null, it's just a dialog item
                        if (curFunctionBlock != null)
                        {
                            // Only save it if it had a reward
                            if (rewardFound == true)
                            {
                                questFunctionBlocks.Add(curFunctionBlock);
                                rewardFound = false;
                            }
                        }
                        curFunctionBlock = null;

                        if (curLine.Contains("e.message:findi"))
                        {
                            curFunctionBlock = new FunctionBlock();
                            curFunctionBlock.FunctionName = FunctionName;
                        }
                    }

                    // Skip conditionals
                    else if (curFunctionBlock != null && curLine.Contains("if("))
                    {
                        exceptionLines.Add(new ExceptionLine(NpcName, ZoneShortName, "Conditional found in quest block, so nullifying it to make manually", i, curLine));
                        curFunctionBlock = null;
                    }

                    // See if a reward was located
                    if (curLine.Contains("SummonCursorItem"))
                        rewardFound = true;

                    if (curFunctionBlock != null)
                        curFunctionBlock.BlockLines.Add(curLine);
                }
            }
            return questFunctionBlocks;
        }
    }
}
