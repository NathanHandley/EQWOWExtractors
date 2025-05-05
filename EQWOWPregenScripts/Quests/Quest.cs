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

using System.Text;

namespace EQWOWPregenScripts.Quests
{
    public class Quest
    {
        static private string OutputQuestFile = "E:\\ConverterData\\Quests\\QuestTemplates.csv";
        static private string OutputQuestReactionsFile = "E:\\ConverterData\\Quests\\QuestReactions.csv";

        public int WOWQuestID;
        public string Name = string.Empty;
        public string ZoneShortName = string.Empty;
        public string QuestgiverName = string.Empty;
        public List<int> RequiredItemIDs = new List<int>(); // Item IDs for turn-in
        public List<int> RequiredItemCounts = new List<int>();
        public int RequiredMoneyInCopper = 0;
        public QuestReward Reward = new QuestReward();
        public int MinimumFaction = -1; // Minimum faction value required
        public int MinimumExpansion = -1;
        public string RequestText = string.Empty;
        public string RewardText = string.Empty;
        public string NotEnoughItemsText = string.Empty;
        public string RewardEmote = string.Empty;
        public List<string> ResponseReactionsRaw = new List<string>();

        private static int curQuestIter = 0;

        public Quest(string zoneShortName, string questGiverName)
        {
            ZoneShortName = zoneShortName;
            QuestgiverName = questGiverName;
        }

        public void CalculateQuestID()
        {
            WOWQuestID = 30000 + curQuestIter;
            curQuestIter++;
        }

        static public void OutputQuests(List<Quest> quests)
        {
            OutputQuestTemplates(quests);
            OutputQuestReactions(quests);
        }

        static private void OutputQuestReactions(List<Quest> quests)
        {
            List<string> outputQuestReactionLines = new List<string>();
            outputQuestReactionLines.Add("wow_questid|zone_shortname|questgiver_name|reaction");
            foreach (Quest quest in quests)
            {
                foreach (string responseReaction in quest.ResponseReactionsRaw)
                {
                    StringBuilder outputSB = new StringBuilder();
                    outputSB.Append(quest.WOWQuestID);
                    outputSB.Append("|");
                    outputSB.Append(quest.ZoneShortName);
                    outputSB.Append("|");
                    outputSB.Append(quest.QuestgiverName);
                    outputSB.Append("|"); 
                    outputSB.Append(responseReaction);
                    outputQuestReactionLines.Add(outputSB.ToString());
                }
            }
            if (File.Exists(OutputQuestReactionsFile))
                File.Delete(OutputQuestReactionsFile);
            using (var outputFile = new StreamWriter(OutputQuestReactionsFile))
                foreach (string outputLine in outputQuestReactionLines)
                    outputFile.WriteLine(outputLine);
        }

        static private void OutputQuestTemplates(List<Quest> quests)
        {
            StringBuilder outputHeaderSB = new StringBuilder();
            outputHeaderSB.Append("wow_questid|zone_shortname|questgiver_name|quest_name|level|req_repmin|req_copper|");
            for (int i = 1; i <= 6; i++)
            {
                outputHeaderSB.Append("req_item_id");
                outputHeaderSB.Append(i);
                outputHeaderSB.Append("|");
                outputHeaderSB.Append("req_item_count");
                outputHeaderSB.Append(i);
                outputHeaderSB.Append("|");
            }
            outputHeaderSB.Append("reward_money|reward_exp|");
            for (int i = 1; i <= 38; i++)
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
            outputHeaderSB.Append("reward_faction1ID|reward_faction1Amt|reward_faction2ID|reward_faction2Amt|reward_faction3ID|reward_faction3Amt|reward_faction4ID|reward_faction4Amt|reward_faction5ID|reward_faction5Amt|reward_faction6ID|reward_faction6Amt|attack_player_after_turnin|request_text|reward_text|min_expansion");
            string outputHeaderLine = outputHeaderSB.ToString();
            List<string> outputQuestLines = new List<string>();
            outputQuestLines.Add(outputHeaderLine);

            for (int qi = 0; qi < quests.Count; qi++)
            {
                Quest quest = quests[qi];

                // Output the found quest
                StringBuilder outputLineSB = new StringBuilder();
                outputLineSB.Append(quest.WOWQuestID);
                outputLineSB.Append("|");
                outputLineSB.Append(quest.ZoneShortName);
                outputLineSB.Append("|");
                outputLineSB.Append(quest.QuestgiverName);
                outputLineSB.Append("|");
                outputLineSB.Append(quest.Name);
                outputLineSB.Append("|");
                outputLineSB.Append("-1"); // Level
                outputLineSB.Append("|");
                outputLineSB.Append(quest.MinimumFaction);
                outputLineSB.Append("|");
                outputLineSB.Append(quest.RequiredMoneyInCopper);
                outputLineSB.Append("|");
                for (int i = 0; i < 6; i++)
                {
                    if (quest.RequiredItemIDs.Count > i)
                    {
                        outputLineSB.Append(quest.RequiredItemIDs[i]);
                        outputLineSB.Append("|");
                        outputLineSB.Append(quest.RequiredItemCounts[i]);
                    }
                    else
                    {
                        outputLineSB.Append("-1");
                        outputLineSB.Append("|");
                        outputLineSB.Append("0");
                    }
                    outputLineSB.Append("|");
                }
                outputLineSB.Append(quest.Reward.Money);
                outputLineSB.Append("|");
                outputLineSB.Append(quest.Reward.Experience);
                outputLineSB.Append("|");
                for (int i = 0; i < 38; i++)
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
                        outputLineSB.Append("0");
                        outputLineSB.Append("|");
                        outputLineSB.Append("0");
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
                outputLineSB.Append(quest.RewardText);
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
