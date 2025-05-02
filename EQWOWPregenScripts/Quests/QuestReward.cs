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

namespace EQWOWPregenScripts.Quests
{
    public class QuestReward
    {
        public int Money; // Is this in silver?  Looks like it
        public List<QuestRewardItem> ItemRewards = new List<QuestRewardItem>();
        public int Experience;
        public List<QuestRewardFactionChange> FactionChanges = new List<QuestRewardFactionChange>();
        public bool AttackPlayerOnTurnin = false; // True if NPC attacks player after turn-in

        public void AddItemReward(int itemID, int count = 1, float chance = 100f)
        {
            foreach (QuestRewardItem itemReward in ItemRewards)
                if (itemReward.ID == itemID)
                {
                    itemReward.Count += count;
                    return;
                }
            ItemRewards.Add(new QuestRewardItem(itemID, count, chance));
        }

        static public QuestReward? GetQuestRewardFromLine(string line, string zoneShortName, string questgiverName, ref List<ExceptionLine> exceptionLines)
        {
            QuestReward? returnReward = new QuestReward();
            // Hard-coded
            if (zoneShortName == "innothule" && questgiverName == "Lynuga")
            {
                returnReward.FactionChanges.Add(new QuestRewardFactionChange(222, 5));
                returnReward.FactionChanges.Add(new QuestRewardFactionChange(308, -5));
                returnReward.FactionChanges.Add(new QuestRewardFactionChange(235, -5));
                returnReward.Experience = 100;
                returnReward.AddItemReward(10082, 1, 5);
                returnReward.AddItemReward(10080, 1, 47.5f);
                returnReward.AddItemReward(10081, 1, 47.5f);
                return returnReward;
            }

            // Strip comments
            string workingLine = line.Split("--")[0];

            // TODO: Handle these conditions
            if (workingLine.Contains(" or"))
                return null;
            //if (workingLine.Contains("random"))
            //    return null;
            //if (workingLine.Contains("ChooseRandom"))
            //    return null;
            //if (workingLine.Contains("GetFaction"))
            //    return null;

            // There are two reward line patterns:
            // - One that is a normal parameter list separated by comma
            // - One that uses bracket notation { }
            if (workingLine.Contains("{"))
            {
                // Get the sets
                List<string> parameterGroups = StringHelper.ExtractCurlyBraceParameters(workingLine);
                int copper = 0;
                int silver = 0;
                int gold = 0;
                int platinum = 0;
                foreach (string parameter in parameterGroups)
                {
                    string[] blocks = parameter.Split(",");
                    switch (blocks[0])
                    {
                        case "itemid":
                            {
                                int itemID;
                                bool parseSuccess = int.TryParse(blocks[1], out itemID);
                                if (parseSuccess == false)
                                {
                                    exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Could not parse itemid out of the line", 0, line));
                                    return null;
                                }
                                else
                                    returnReward.AddItemReward(itemID);
                            }
                            break;
                        case "items":
                            {
                                int numOfRowsRead;
                                string itemsBlock = StringHelper.ExtractCurlyBraceContentRaw(new List<string>() { parameter }, 0, out numOfRowsRead);
                                string[] itemBlocks = itemsBlock.Split(",");
                                foreach (string parameterValue in itemBlocks)
                                {
                                    int itemID;
                                    bool parseSuccess = int.TryParse(parameterValue, out itemID);
                                    if (parseSuccess == false)
                                    {
                                        exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Could not parse itemid out of the line", 0, line));
                                        return null;
                                    }
                                    else
                                        returnReward.AddItemReward(itemID);
                                }
                            }
                            break;
                        case "exp":
                            {
                                returnReward.Experience = StringHelper.GetSingleRangedIntFromString(blocks[1], zoneShortName, questgiverName, ref exceptionLines);
                            }
                            break;
                        case "copper":
                            {
                                copper = StringHelper.GetSingleRangedIntFromString(blocks[1], zoneShortName, questgiverName, ref exceptionLines);
                            }
                            break;
                        case "silver":
                            {
                                silver = StringHelper.GetSingleRangedIntFromString(blocks[1], zoneShortName, questgiverName, ref exceptionLines);
                            }
                            break;
                        case "gold":
                            {
                                gold = StringHelper.GetSingleRangedIntFromString(blocks[1], zoneShortName, questgiverName, ref exceptionLines);
                            }
                            break;
                        case "platinum":
                            {
                                platinum = StringHelper.GetSingleRangedIntFromString(blocks[1], zoneShortName, questgiverName, ref exceptionLines);
                            }
                            break;
                        default:
                            {
                                exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Unhandled parameter block inside curly brace line with key " + blocks[0], 0, line));
                            }
                            break;
                    }
                }
                returnReward.Money = copper + (silver * 100) + (gold * 10000) + (platinum * 1000000);
            }
            else
            {
                // Get the parameters, and process them
                List<string> parameters = StringHelper.ExtractMethodParameters(workingLine, "QuestReward");

                // Target
                if (parameters[0] != "e.self")
                    exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Reward line's first parameter is not e.self, so it's unhandled", 0, line));

                // Copper
                int copper = StringHelper.GetSingleRangedIntFromString(parameters[1], zoneShortName, questgiverName, ref exceptionLines);

                // Silver
                int silver = 0;
                if (parameters.Count > 2)
                    silver = StringHelper.GetSingleRangedIntFromString(parameters[2], zoneShortName, questgiverName, ref exceptionLines);

                // Gold
                int gold = 0;
                if (parameters.Count > 3)
                    gold = StringHelper.GetSingleRangedIntFromString(parameters[3], zoneShortName, questgiverName, ref exceptionLines);

                // Platinum
                int platinum = 0;
                if (parameters.Count > 4)
                    gold = StringHelper.GetSingleRangedIntFromString(parameters[4], zoneShortName, questgiverName, ref exceptionLines);

                // Add/assign the money
                returnReward.Money = copper + (silver * 100) + (gold * 10000) + (platinum * 1000000);

                // ItemID
                if (parameters.Count > 5)
                {
                    int itemID;
                    bool parseSuccess = int.TryParse(parameters[5], out itemID);
                    if (parseSuccess == false)
                    {
                        exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Unable to determine the ItemID from the string " + parameters[5], 0, line));
                        return null;
                    }
                    if (itemID != 0)
                        returnReward.AddItemReward(itemID);
                }

                // Experience
                if (parameters.Count > 6)
                    returnReward.Experience = StringHelper.GetSingleRangedIntFromString(parameters[6], zoneShortName, questgiverName, ref exceptionLines);

                // Faction and beyond (faction is a bool)
                if (parameters.Count > 7)
                    exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Reward line had 7 or more parameters which is unhandled", 0, line));
            }

            return returnReward;
        }
    }
}
