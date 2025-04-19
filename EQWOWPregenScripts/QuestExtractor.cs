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

        // Represents a faction change
        public class FactionChange
        {
            public int FactionId { get; set; }
            public int ChangeAmount { get; set; }
            public string FactionName { get; set; } // Optional, from comments
        }

        // Represents quest rewards
        public class QuestReward
        {
            public int Money { get; set; } // Silver amount, TODO: Confirm if this is actually silver!
            public List<int> Items { get; set; } = new List<int>(); // Item IDs
            public int Experience { get; set; }
            public List<FactionChange> FactionChanges { get; set; } = new List<FactionChange>();
        }

        // Represents a quest
        public class Quest
        {
            public string Name { get; set; } // Derived from context or item
            public List<int> RequiredItems { get; set; } = new List<int>(); // Item IDs for turn-in
            public QuestReward Reward { get; set; } = new QuestReward();
            public List<string> Dialogue { get; set; } = new List<string>(); // NPC Say statements
        }

        // Parser to extract quests from Lua code
        public class QuestParser
        {
            private readonly List<string> _lines;

            public QuestParser(List<string> lines)
            {
                _lines = lines;
            }

            public List<Quest> ParseQuests()
            {
                var quests = new List<Quest>();
                for (int i = 0; i < _lines.Count; i++)
                {
                    string line = _lines[i].Trim();
                    // Look for check_turn_in
                    if (line.Contains("check_turn_in") && line.Contains("item1"))
                    {
                        var quest = ParseQuest(i);
                        if (quest != null)
                        {
                            quests.Add(quest);
                        }
                    }
                }
                return quests;
            }

            private Quest ParseQuest(int startIndex)
            {
                var quest = new Quest();
                string line = _lines[startIndex].Trim();

                // Parse required items from check_turn_in
                var itemMatch = Regex.Match(line, @"check_turn_in\([^,]+,[^{]+{([^}]+)}\)");
                if (!itemMatch.Success) return null;

                string itemMatchString = itemMatch.Groups[1].Value;
                var itemPairs = itemMatchString.Split(',').Select(s => s.Trim());
                foreach (var pair in itemPairs)
                {
                    var match = Regex.Match(pair, @"item\d+\s*=\s*(\d+)");
                    if (match.Success)
                    {
                        quest.RequiredItems.Add(int.Parse(match.Groups[1].Value));
                    }
                }

                // Generate quest name based on items
                quest.Name = $"Turn in {string.Join(", ", quest.RequiredItems)}";

                // Find associated Say statement (before or after)
                for (int i = Math.Max(0, startIndex - 5); i < Math.Min(_lines.Count, startIndex + 5); i++)
                {
                    string dialogueLine = _lines[i].Trim();
                    if (dialogueLine.Contains("e.self:Say"))
                    {
                        var sayMatch = Regex.Match(dialogueLine, @"e\.self:Say\(""([^""]+)""");
                        if (sayMatch.Success)
                        {
                            quest.Dialogue.Add(sayMatch.Groups[1].Value);
                        }
                    }
                }

                // Parse rewards and faction changes
                for (int i = startIndex + 1; i < _lines.Count; i++)
                {
                    line = _lines[i].Trim();
                    // Parse faction changes
                    if (line.Contains("e.other:Faction"))
                    {
                        var factionMatch = Regex.Match(line, @"e\.other:Faction\([^,]+,\s*(\d+),\s*([-]?\d+)[^;]*\);\s*--\s*([^)]+)");
                        if (factionMatch.Success)
                        {
                            quest.Reward.FactionChanges.Add(new FactionChange
                            {
                                FactionId = int.Parse(factionMatch.Groups[1].Value),
                                ChangeAmount = int.Parse(factionMatch.Groups[2].Value),
                                FactionName = factionMatch.Groups[3].Value.Trim()
                            });
                        }
                    }
                    // Parse QuestReward
                    if (line.Contains("e.other:QuestReward"))
                    {
                        var rewardMatch = Regex.Match(line, @"e\.other:QuestReward\([^,]+,(\d+),(\d+),(\d+),(\d+),([^,]+),(\d+)\)");
                        if (rewardMatch.Success)
                        {
                            quest.Reward.Money = int.Parse(rewardMatch.Groups[2].Value); // Silver
                            quest.Reward.Experience = int.Parse(rewardMatch.Groups[6].Value);

                            // Parse items (can be a single ID or a list like {id1, id2})
                            string itemsStr = rewardMatch.Groups[5].Value.Trim();
                            if (itemsStr.StartsWith("{") && itemsStr.EndsWith("}"))
                            {
                                itemsStr = itemsStr.Trim('{', '}');
                                quest.Reward.Items.AddRange(itemsStr.Split(',').Select(s => int.Parse(s.Trim())));
                            }
                            else if (int.TryParse(itemsStr, out int itemId))
                            {
                                quest.Reward.Items.Add(itemId);
                            }
                        }
                        break; // Assume rewards are the last part of the quest block
                    }
                }

                return quest;
            }
        }

        private Quest ParseQuest(List<string> lines, int startIndex)
        {
            var quest = new Quest();
            string line = lines[startIndex].Trim();

            // Parse required items from check_turn_in
            var itemMatch = Regex.Match(line, @"check_turn_in\([^,]+,[^{]+{([^}]+)}\)");
            if (!itemMatch.Success) return null;

            string requiredItemsStr = itemMatch.Groups[1].Value;
            var itemPairs = requiredItemsStr.Split(',').Select(s => s.Trim());
            foreach (var pair in itemPairs)
            {
                var match = Regex.Match(pair, @"item\d+\s*=\s*(\d+)");
                if (match.Success)
                {
                    quest.RequiredItems.Add(int.Parse(match.Groups[1].Value));
                }
            }

            // Generate quest name based on items
            quest.Name = $"Turn in {string.Join(", ", quest.RequiredItems)}";

            // Find associated Say statement (before or after)
            for (int i = Math.Max(0, startIndex - 5); i < Math.Min(lines.Count, startIndex + 5); i++)
            {
                string dialogueLine = lines[i].Trim();
                if (dialogueLine.Contains("e.self:Say"))
                {
                    var sayMatch = Regex.Match(dialogueLine, @"e\.self:Say\(""([^""]+)""");
                    if (sayMatch.Success)
                    {
                        quest.Dialogue.Add(sayMatch.Groups[1].Value);
                    }
                }
            }

            // Parse rewards and faction changes
            for (int i = startIndex + 1; i < lines.Count; i++)
            {
                line = lines[i].Trim();
                // Parse faction changes
                if (line.Contains("e.other:Faction"))
                {
                    var factionMatch = Regex.Match(line, @"e\.other:Faction\([^,]+,\s*(\d+),\s*([-]?\d+)[^;]*\);\s*--\s*([^)]+)");
                    if (factionMatch.Success)
                    {
                        quest.Reward.FactionChanges.Add(new FactionChange
                        {
                            FactionId = int.Parse(factionMatch.Groups[1].Value),
                            ChangeAmount = int.Parse(factionMatch.Groups[2].Value),
                            FactionName = factionMatch.Groups[3].Value.Trim()
                        });
                    }
                }
                // Parse QuestReward
                if (line.Contains("e.other:QuestReward"))
                {
                    // Match QuestReward with 6 arguments (items, exp) or 5 arguments (item only)
                    var rewardMatch = Regex.Match(line, @"e\.other:QuestReward\([^,]+,(\d+),(\d+),(\d+),(\d+),([^,]+)(?:,(\d+))?\)");
                    if (rewardMatch.Success)
                    {
                        quest.Reward.Money = int.Parse(rewardMatch.Groups[2].Value); // Silver

                        string rewardItemsStr = rewardMatch.Groups[5].Value.Trim();
                        // Check if the last group (exp) exists
                        if (rewardMatch.Groups.Count > 6 && !string.IsNullOrEmpty(rewardMatch.Groups[6].Value))
                        {
                            quest.Reward.Experience = int.Parse(rewardMatch.Groups[6].Value);
                            // Parse items (can be a single ID or a list like {id1, id2})
                            if (rewardItemsStr.StartsWith("{") && rewardItemsStr.EndsWith("}"))
                            {
                                rewardItemsStr = rewardItemsStr.Trim('{', '}');
                                quest.Reward.Items.AddRange(rewardItemsStr.Split(',').Select(s => int.Parse(s.Trim())));
                            }
                            else if (int.TryParse(rewardItemsStr, out int itemId))
                            {
                                quest.Reward.Items.Add(itemId);
                            }
                        }
                        else
                        {
                            // No exp, last argument is the item
                            if (int.TryParse(rewardItemsStr, out int itemId))
                            {
                                quest.Reward.Items.Add(itemId);
                            }
                            quest.Reward.Experience = 0; // Default to 0 if no exp specified
                        }
                    }
                    break; // Assume rewards are the last part of the quest block
                }
            }

            return quest;
        }

        public void ExtractQuests()
        {
            string outputHeaderLine = "zone_shortname|questgiver_name|req_item1|req_item2|req_item3|req_item4|req_item4|req_item5|req_item6|reward_money|reward_exp|reward_item1|reward_item2|reward_item3|reward_item4|reward_item5|reward_item6|reward_faction1ID|reward_faction1Amt|reward_faction2ID|reward_faction2Amt|reward_faction3ID|reward_faction3Amt|reward_faction4ID|reward_faction4Amt|reward_faction5ID|reward_faction5Amt|reward_faction6ID|reward_faction6Amt|reward_faction7ID|reward_faction7Amt|reward_faction8ID|reward_faction8Amt|reward_faction9ID|reward_faction9Amt|reward_faction10ID|reward_faction10Amt|reward_dialog";
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
                    var quests = new List<Quest>();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        string line = lines[i];
                        if (line.Contains("check_turn_in") && line.Contains("item1"))
                        {
                            var quest = ParseQuest(lines, i);
                            if (quest != null)
                            {
                                quests.Add(quest);
                            }
                        }
                    }
                    foreach (var quest in quests)
                    {
                        // Output the found quest
                        StringBuilder outputLineSB = new StringBuilder();
                        outputLineSB.Append(zoneShortName);
                        outputLineSB.Append("|");
                        outputLineSB.Append(questgiverName);
                        outputLineSB.Append("|");
                        outputLineSB.Append(quest.Name);
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
                        for (int i = 0; i < 6; i++)
                        {
                            if (quest.Reward.Items.Count > i)
                                outputLineSB.Append(quest.Reward.Items[i]);
                            else
                                outputLineSB.Append("-1");
                            outputLineSB.Append("|");
                        }
                        for (int i = 0; i < 10; i++)
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
