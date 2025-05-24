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

///////////////////////////////////////////////////////////////////////////////
// TODO LIST
// - Handle "script_init" files (example: cazicthule\script_init
// Timer Events
//  - airplane\#Master_of_Elements
//      - function event_timer(e)
// Subconditionals for rewards
//  - akanon\Manik_Compolten
//      - if(math.random(100) < 20) then
// elseif on check_turn_in
//  - airplane\Inte_Akera
// Say events that summon items
//  - airplane\Cilin_Spellsinger
//      - e.other:SummonCursorItem(18542); -- The Flute
// Combat events (event_combat(e)
//  - airplane\Sirran_the_lunatic
// Hail
//  - cabeast\Hierophant_Oxyn
//      - if(e.message:findi("hail")) then
// Money as a requirement
//  - airplane\a_thunder_spirit_princess
//      - if(item_lib.check_turn_in(e.self, e.trade, {gold = 10})) then
// Death Events
//  - airplane\a_thunder_spirit_princess
//      - function event_death_complete(e)
// Rewards to cursor
//  - global\Priest_of_Discord
//      - e.other:SummonCursorItem(18700); -- Item: Tome of Order and Discord
// Text Parse Bug (no ' at the end)
// - eastkarana, Tanal_Redblade
//  - Very good, you have wreaked havoc on your foes in the ancient land of the giants. Rallos Zek must have guided your blade. (Tenal's voice is suddenly silenced and you feel as if your body is frozen. From Tenal's lips issues a voice that is not his own.) 'Bring this mortal the scales of the children of Veeshan. The red and green as well as my war totem. I will guide your blade.' Your movement returns as Tenal falls to the ground, gasping for breath.

///////////////////////////////////////////////////////////////////////////////

using EQWOWPregenScripts;
using EQWOWPregenScripts.Quests;
using System.Text;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Uncomment to export quests
//List<ExceptionLine> exceptionLines = new List<ExceptionLine>();
//List<Quest> quests = new List<Quest>();

//NPCDataExtractor npcExtractor = new NPCDataExtractor();
//string zoneQuestFolderRoot = Path.Combine("E:\\ConverterData\\Quests", "zonequests");
//string[] zoneFolders = Directory.GetDirectories(zoneQuestFolderRoot);
//List<string> filesToSkip = new List<string>() { "Shuttle_I", "Shuttle_II", "Shuttle_III", "Shuttle_IV", "SirensBane", "Stormbreaker", "Golden_Maiden", 
//    "Sea_King", "Suddenly", "pirate_runners_skiff", "Maidens_Voyage", "Muckskimmer", "Captains_Skiff", "Barrel_Barge", "Bloated_Belly", "script_init" };
//foreach (string zoneFolder in zoneFolders)
//{
//    // Shortname
//    string zoneShortName = Path.GetFileName(zoneFolder);

//    string[] questNPCFiles = Directory.GetFiles(zoneFolder, "*.lua");
//    foreach (string questNPCFile in questNPCFiles)
//    {
//        string npcName = Path.GetFileNameWithoutExtension(questNPCFile);
//        if (filesToSkip.Contains(npcName))
//            continue;

//        npcExtractor.ProcessFile(questNPCFile, zoneShortName, ref exceptionLines, ref quests);
//    }
//}
//Quest.OutputQuests(quests);
//ExceptionLine.OutputExceptionLines(exceptionLines);
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Create item lookups
//string itemTemplatesFile = "E:\\ConverterData\\itemTemplates.csv";
//Dictionary<int, string> itemNamesByEQIDs = new Dictionary<int, string>();
//foreach (Dictionary<string, string> itemColumns in FileTool.ReadAllRowsFromFileWithHeader(itemTemplatesFile, "|"))
//    itemNamesByEQIDs.Add(Convert.ToInt32(itemColumns["id"]), itemColumns["Name"]);

//// Map the item names to the first required item
//string questTemplatesFileInput = "E:\\ConverterData\\QuestTemplates.csv";
//List<Dictionary<string, string>> questTemplatesRows = FileTool.ReadAllRowsFromFileWithHeader(questTemplatesFileInput, "|");
//for (int i = 0; i < questTemplatesRows.Count; i++)
//{
//    Dictionary<string, string> questColumns = questTemplatesRows[i];
//    int requiredItem1ID = int.Parse(questColumns["req_item_id1"]);
//    if (requiredItem1ID != -1)
//    {
//        if (itemNamesByEQIDs.ContainsKey(requiredItem1ID) == false)
//            questColumns["req_item1_name"] = "INVALID";
//        else
//            questColumns["req_item1_name"] = itemNamesByEQIDs[requiredItem1ID];
//    }
//    int rewardItem1ID = int.Parse(questColumns["reward_item_ID1"]);
//    if (rewardItem1ID != -1)
//    {
//        if (itemNamesByEQIDs.ContainsKey(rewardItem1ID) == false)
//            questColumns["reward_item1_name"] = "INVALID";
//        else
//            questColumns["reward_item1_name"] = itemNamesByEQIDs[rewardItem1ID];
//    }
//}

//// Write a new file for it
//string questTemplatesFileOutput = "E:\\ConverterData\\QuestTemplatesUpdated.csv";
//FileTool.WriteFile(questTemplatesFileOutput, questTemplatesRows);
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Condition reactions - Phase 1
//string questReactionsInputFileName = "E:\\ConverterData\\QuestReactionsInput.csv";
//List<Dictionary<string, string>> discardedRows = new List<Dictionary<string, string>>();
//List<QuestReaction> reactions = new List<QuestReaction>();
//List<Dictionary<string, string>> questReactionColumnRows = FileTool.ReadAllRowsFromFileWithHeader(questReactionsInputFileName, "|");
//foreach (Dictionary<string, string> questReactionColumns in questReactionColumnRows)
//{
//    int curReactionCount = reactions.Count;
//    QuestReaction curQuestReaction = new QuestReaction();
//    curQuestReaction.QuestID = int.Parse(questReactionColumns["wow_questid"]);
//    curQuestReaction.ZoneShortName = questReactionColumns["zone_shortname"];
//    curQuestReaction.QuestGiverName = questReactionColumns["questgiver_name"];
//    string reactionString = questReactionColumns["reaction"].Trim();

//    // Categorize
//    if (reactionString.Contains("e.self:Say(") || reactionString.Contains("e.other:Say("))
//    {
//        string formattedString = StringHelper.ConvertText(reactionString.Replace("e.self:Say(", "").Replace("e.other:Say(", ""));
//        curQuestReaction.ReactionType = "say";
//        curQuestReaction.ReactionValue1 = formattedString;
//        reactions.Add(curQuestReaction);
//    }
//    else if (reactionString.Contains("e.self:Shout("))
//    {
//        string formattedString = StringHelper.ConvertText(reactionString.Replace("e.self:Shout(", ""));
//        curQuestReaction.ReactionType = "yell";
//        curQuestReaction.ReactionValue1 = formattedString;
//        reactions.Add(curQuestReaction);
//    }
//    else if (reactionString.Contains("e.self:Emote"))
//    {
//        curQuestReaction.ReactionType = "emote";
//        string formattedString = StringHelper.ConvertText(reactionString.Replace("e.self:Emote(", ""));
//        curQuestReaction.ReactionValue1 = formattedString;
//        reactions.Add(curQuestReaction);
//    }
//    else if (reactionString.StartsWith("eq.spawn2("))
//    {
//        curQuestReaction.ReactionType = "spawn";
//        List<string> methodParameters = StringHelper.ExtractMethodParameters(reactionString, "eq.spawn2");
//        curQuestReaction.ReactionValue1 = methodParameters[0];
//        if (methodParameters[3].Contains("e.self:GetX("))
//        {
//            curQuestReaction.ReactionValue2 = "playerX";
//            if (methodParameters[3].Contains("-") || methodParameters[3].Contains("+"))
//                curQuestReaction.ReactionValue6 = StringHelper.GetAddedMathPart(methodParameters[3]);
//        }
//        else
//            curQuestReaction.ReactionValue2 = methodParameters[3];
//        if (methodParameters[4].Contains("e.self:GetY("))
//        {
//            curQuestReaction.ReactionValue3 = "playerY";
//            if (methodParameters[4].Contains("-") || methodParameters[4].Contains("+"))
//                curQuestReaction.ReactionValue7 = StringHelper.GetAddedMathPart(methodParameters[4]);
//        }
//        else
//            curQuestReaction.ReactionValue3 = methodParameters[4];
//        if (methodParameters[5].Contains("e.self:GetZ("))
//        {
//            curQuestReaction.ReactionValue4 = "playerZ";
//            if (methodParameters[5].Contains("-") || methodParameters[5].Contains("+"))
//                curQuestReaction.ReactionValue8 = StringHelper.GetAddedMathPart(methodParameters[5]);
//        }
//        else
//            curQuestReaction.ReactionValue4 = methodParameters[5];
//        if (methodParameters[6].Contains("e.self:GetHeading"))
//            curQuestReaction.ReactionValue5 = "playerHeading";
//        else
//            curQuestReaction.ReactionValue5 = methodParameters[6];
//        reactions.Add(curQuestReaction);
//    }
//    else if (reactionString.StartsWith("eq.unique_spawn("))
//    {
//        curQuestReaction.ReactionType = "spawnunique";
//        List<string> methodParameters = StringHelper.ExtractMethodParameters(reactionString, "unique_spawn");
//        curQuestReaction.ReactionValue1 = methodParameters[0];
//        if (methodParameters[3].Contains("e.self:GetX("))
//        {
//            curQuestReaction.ReactionValue2 = "playerX";
//            if (methodParameters[3].Contains("-") || methodParameters[3].Contains("+"))
//                curQuestReaction.ReactionValue6 = StringHelper.GetAddedMathPart(methodParameters[3]);
//        }
//        else
//            curQuestReaction.ReactionValue2 = methodParameters[3];
//        if (methodParameters[4].Contains("e.self:GetY("))
//        {
//            curQuestReaction.ReactionValue3 = "playerY";
//            if (methodParameters[4].Contains("-") || methodParameters[4].Contains("+"))
//                curQuestReaction.ReactionValue7 = StringHelper.GetAddedMathPart(methodParameters[4]);
//        }
//        else
//            curQuestReaction.ReactionValue3 = methodParameters[4];
//        if (methodParameters[5].Contains("e.self:GetZ("))
//        {
//            curQuestReaction.ReactionValue4 = "playerZ";
//            if (methodParameters[5].Contains("-") || methodParameters[5].Contains("+"))
//                curQuestReaction.ReactionValue8 = StringHelper.GetAddedMathPart(methodParameters[5]);
//        }
//        else
//            curQuestReaction.ReactionValue4 = methodParameters[5];
//        if (methodParameters.Count > 6 && methodParameters[6].Contains("e.self:GetHeading"))
//            curQuestReaction.ReactionValue5 = "playerHeading";
//        else if (methodParameters.Count > 6)
//            curQuestReaction.ReactionValue5 = methodParameters[6];
//        reactions.Add(curQuestReaction);
//    }

//    // Handle the inlined attack player
//    if (reactionString.Contains("AddToHateList(e.other,1);") || reactionString.Contains("eq.attack(e.other:GetName())"))
//    {
//        QuestReaction attackReaction = new QuestReaction();
//        attackReaction.QuestID = int.Parse(questReactionColumns["wow_questid"]);
//        attackReaction.ZoneShortName = questReactionColumns["zone_shortname"];
//        attackReaction.QuestGiverName = questReactionColumns["questgiver_name"];
//        attackReaction.ReactionType = "attackplayer";
//        reactions.Add(attackReaction);
//    }

//    if (reactionString.Contains("eq.depop()") || reactionString.Contains("eq.depop_with_timer()")) // TODO: Handle timer?
//    {
//        QuestReaction attackReaction = new QuestReaction();
//        attackReaction.QuestID = int.Parse(questReactionColumns["wow_questid"]);
//        attackReaction.ZoneShortName = questReactionColumns["zone_shortname"];
//        attackReaction.QuestGiverName = questReactionColumns["questgiver_name"];
//        attackReaction.ReactionType = "despawn";
//        attackReaction.ReactionValue1 = "self";
//        reactions.Add(attackReaction);
//    }
//    else if (reactionString.Contains("eq.follow(e.other:GetID());"))
//    {

//    }
//    else if (reactionString.Contains("eq.depop("))
//    {
//        QuestReaction despawnReaction = new QuestReaction();
//        despawnReaction.QuestID = int.Parse(questReactionColumns["wow_questid"]);
//        despawnReaction.ZoneShortName = questReactionColumns["zone_shortname"];
//        despawnReaction.QuestGiverName = questReactionColumns["questgiver_name"];
//        despawnReaction.ReactionType = "despawn";
//        List<string> methodParameters = StringHelper.ExtractMethodParameters(reactionString, "depop");
//        despawnReaction.ReactionValue1 = methodParameters[0];
//        reactions.Add(despawnReaction);
//    }
//    else if (reactionString.Contains("eq.depop_with_timer(")) // TODO: Handle timer?
//    {
//        QuestReaction despawnReaction = new QuestReaction();
//        despawnReaction.QuestID = int.Parse(questReactionColumns["wow_questid"]);
//        despawnReaction.ZoneShortName = questReactionColumns["zone_shortname"];
//        despawnReaction.QuestGiverName = questReactionColumns["questgiver_name"];
//        despawnReaction.ReactionType = "despawn";
//        List<string> methodParameters = StringHelper.ExtractMethodParameters(reactionString, "depop_with_timer");
//        despawnReaction.ReactionValue1 = methodParameters[0];
//        reactions.Add(despawnReaction);
//    }

//    // Everything else is discarded
//    if (curReactionCount == reactions.Count)
//    {
//        Dictionary<string, string> discardedRow = new Dictionary<string, string>();
//        discardedRow.Add("wow_questid", curQuestReaction.QuestID.ToString());
//        discardedRow.Add("zone_shortname", curQuestReaction.ZoneShortName);
//        discardedRow.Add("questgiver_name", curQuestReaction.QuestGiverName);
//        discardedRow.Add("reaction", reactionString);
//        discardedRows.Add(discardedRow);
//    }
//}

//// Write parsed rows
//string questReactionsOutputFileName = "E:\\ConverterData\\QuestReactionsOutput.csv";
//List<Dictionary<string, string>> outputReactionRows = new List<Dictionary<string, string>>();
//foreach (QuestReaction reaction in reactions)
//{
//    Dictionary<string, string> outputReactionRow = new Dictionary<string, string>();
//    outputReactionRow.Add("wow_questid", reaction.QuestID.ToString());
//    outputReactionRow.Add("zone_shortname", reaction.ZoneShortName);
//    outputReactionRow.Add("questgiver_name", reaction.QuestGiverName);
//    outputReactionRow.Add("reaction_type", reaction.ReactionType);
//    outputReactionRow.Add("reaction_value1", reaction.ReactionValue1);
//    outputReactionRow.Add("reaction_value2", reaction.ReactionValue2);
//    outputReactionRow.Add("reaction_value3", reaction.ReactionValue3);
//    outputReactionRow.Add("reaction_value4", reaction.ReactionValue4);
//    outputReactionRow.Add("reaction_value5", reaction.ReactionValue5);
//    outputReactionRow.Add("reaction_value6", reaction.ReactionValue6);
//    outputReactionRow.Add("reaction_value7", reaction.ReactionValue7);
//    outputReactionRow.Add("reaction_value8", reaction.ReactionValue8);
//    outputReactionRows.Add(outputReactionRow);
//}
//FileTool.WriteFile(questReactionsOutputFileName, outputReactionRows);

//// Write discarded rows
//string questReactionsDiscardedFileName = "E:\\ConverterData\\QuestReactionsDiscarded.csv";
//FileTool.WriteFile(questReactionsDiscardedFileName, discardedRows);

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Condition reactions - Generate script lines

//// Load in lookups for creature templates
//string creatureTemplatesFile = "E:\\ConverterData\\CreatureTemplates.csv";
//Dictionary<string, Dictionary<string, string>> wowCreatureTemplateIDByCreatureNameByZones = new Dictionary<string, Dictionary<string, string>>();
//Dictionary<string, string> wowCreatureTemplateIDByEQID = new Dictionary<string, string>();
//Dictionary<string, string> wowCreatureNameByEQID = new Dictionary<string, string>();
//foreach (Dictionary<string, string> columns in FileTool.ReadAllRowsFromFileWithHeader(creatureTemplatesFile, "|"))
//{
//    string spawnZones = columns["spawnzones"];
//    if (wowCreatureTemplateIDByCreatureNameByZones.ContainsKey(spawnZones) == false)
//        wowCreatureTemplateIDByCreatureNameByZones.Add(spawnZones, new Dictionary<string, string>());
//    if (wowCreatureTemplateIDByCreatureNameByZones[spawnZones].ContainsKey(columns["name"]) == false)
//        wowCreatureTemplateIDByCreatureNameByZones[spawnZones].Add(columns["name"], columns["wow_id"]);
//    wowCreatureTemplateIDByEQID.Add(columns["eq_id"], columns["wow_id"]);
//    wowCreatureNameByEQID.Add(columns["eq_id"], columns["name"]);
//}

//static string GetPositionData(string coordinateBaseString, string playerIdentifierString, string playerCoordinatePartString, string coordinateAddString)
//{
//    string positionString = string.Empty;
//    if (coordinateBaseString != playerIdentifierString)
//    {
//        float positionValue = float.Parse(coordinateBaseString) * 0.29f;
//        if (coordinateAddString.Length > 0)
//            positionValue += float.Parse(coordinateAddString) * 0.29f;
//        positionValue = float.Round(positionValue, 2);
//        positionString = positionValue.ToString();
//    }
//    else
//    {
//        positionString = playerCoordinatePartString;
//        if (coordinateAddString.Length > 0)
//        {
//            float addStringValue = float.Round(float.Parse(coordinateAddString) * 0.29f, 2);
//            if (addStringValue > 0)
//                positionString += "+" + addStringValue.ToString();
//            else
//                positionString += addStringValue.ToString();
//        }
//    }
//    return positionString;
//}

//static string GetHeadingData(string reactionValue)
//{
//    if (reactionValue == "playerHeading")
//        return "orientation";
//    else if (reactionValue == "")
//        return "0";

//    float heading = float.Parse(reactionValue);
//    float modHeading = 0;
//    if (heading != 0)
//        modHeading = heading / (256f / 360f);
//    return Convert.ToString(modHeading * Convert.ToSingle(Math.PI / 180));
//}

//// Open the reactions file and make rows
//string questReactionsFile = "E:\\ConverterData\\QuestReactions.csv";
//List<Dictionary<string, string>> reactionsRows = FileTool.ReadAllRowsFromFileWithHeader(questReactionsFile, "|");
//List<string> outputScriptRows = new List<string>();
//List<string> unknownSpawnZoneRows = new List<string>();
//unknownSpawnZoneRows.Add("ZoneName|NPCName|WOWID");
//int curBaseQuestID = 0;
//foreach (Dictionary<string, string> columns in reactionsRows)
//{
//    // Skip anything that isn't a handled type
//    string reactionType = columns["reaction_type"];
//    if (reactionType != "despawn" && reactionType != "spawn" && reactionType != "spawnunique" && reactionType != "attackplayer")
//        continue;

//    // Make sure to make two rows for each quest ID
//    int questID = int.Parse(columns["wow_questid"]);
//    string zoneShortName = columns["zone_shortname"];
//    string npcName = columns["questgiver_name"];
//    string reactionValue1 = columns["reaction_value1"];
//    string reactionValue2 = columns["reaction_value2"];
//    string reactionValue3 = columns["reaction_value3"];
//    string reactionValue4 = columns["reaction_value4"];
//    string reactionValue5 = columns["reaction_value5"];
//    string reactionValue6 = columns["reaction_value6"];
//    string reactionValue7 = columns["reaction_value7"];
//    if (questID != curBaseQuestID)
//    {
//        if (curBaseQuestID != 0)
//            outputScriptRows.Add("}break;");
//        outputScriptRows.Add(string.Concat("case ", questID, ": // ", zoneShortName, " - ", npcName));
//        outputScriptRows.Add(string.Concat("case ", questID+5000, ": // Fallthrough - Repeat Quest"));      
//        outputScriptRows.Add("{");
//        curBaseQuestID = questID;
//    }

//    switch (reactionType)
//    {
//        case "despawn":
//            {
//                string creatureTemplateIDString = string.Empty;
//                if (reactionValue1 == "self")
//                {
//                    if (wowCreatureTemplateIDByCreatureNameByZones[zoneShortName].ContainsKey(npcName) == false)
//                    {
//                        creatureTemplateIDString = wowCreatureTemplateIDByCreatureNameByZones[""][npcName];
//                        unknownSpawnZoneRows.Add(string.Concat(zoneShortName, "|", npcName, "|", creatureTemplateIDString));
//                    }
//                    else
//                        creatureTemplateIDString = wowCreatureTemplateIDByCreatureNameByZones[zoneShortName][npcName];
//                }
//                else
//                    creatureTemplateIDString = wowCreatureTemplateIDByEQID[reactionValue1];
//                outputScriptRows.Add(string.Concat("EverQuest->DespawnCreature(", creatureTemplateIDString, ", map);"));
//            }
//            break;
//        case "spawn":
//            {
//                string creatureTemplateIDString = wowCreatureTemplateIDByEQID[reactionValue1];
//                string xPosition = GetPositionData(reactionValue3, "playerY", "x", reactionValue7); // Invert X and Y due to coordinate differences between games
//                string yPosition = GetPositionData(reactionValue2, "playerX", "y", reactionValue6); // Invert X and Y due to coordinate differences between games
//                string zPosition = GetPositionData(reactionValue4, "playerZ", "z", string.Empty);
//                string heading = GetHeadingData(reactionValue5);
//                StringBuilder sb = new StringBuilder();
//                sb.Append("EverQuest->SpawnCreature(");
//                sb.Append(creatureTemplateIDString);
//                sb.Append(", map, ");
//                sb.Append(xPosition);
//                sb.Append(", ");
//                sb.Append(yPosition);
//                sb.Append(", ");
//                sb.Append(zPosition);
//                sb.Append(", ");
//                sb.Append(heading);
//                sb.Append(", false);");
//                outputScriptRows.Add(sb.ToString());
//            }
//            break;
//        case "spawnunique":
//            {
//                string creatureTemplateIDString = wowCreatureTemplateIDByEQID[reactionValue1];
//                string xPosition = GetPositionData(reactionValue3, "playerY", "x", reactionValue7); // Invert X and Y due to coordinate differences between games
//                string yPosition = GetPositionData(reactionValue2, "playerX", "y", reactionValue6); // Invert X and Y due to coordinate differences between games
//                string zPosition = GetPositionData(reactionValue4, "playerZ", "z", string.Empty);
//                string heading = GetHeadingData(reactionValue5);
//                StringBuilder sb = new StringBuilder();
//                sb.Append("EverQuest->SpawnCreature(");
//                sb.Append(creatureTemplateIDString);
//                sb.Append(", map, ");
//                sb.Append(xPosition);
//                sb.Append(", ");
//                sb.Append(yPosition);
//                sb.Append(", ");
//                sb.Append(zPosition);
//                sb.Append(", ");
//                sb.Append(heading);
//                sb.Append(", true);");
//                outputScriptRows.Add(sb.ToString());
//            }
//            break;
//        case "attackplayer":
//            {
//                string creatureTemplateIDString = "";
//                if (wowCreatureTemplateIDByCreatureNameByZones[zoneShortName].ContainsKey(npcName) == false)
//                {
//                    creatureTemplateIDString = wowCreatureTemplateIDByCreatureNameByZones[""][npcName];
//                    unknownSpawnZoneRows.Add(string.Concat(zoneShortName, "|", npcName, "|", creatureTemplateIDString));
//                }
//                else
//                    creatureTemplateIDString = wowCreatureTemplateIDByCreatureNameByZones[zoneShortName][npcName];
//                outputScriptRows.Add(string.Concat("EverQuest->MakeCreatureAttackPlayer(", creatureTemplateIDString, ", map, player);"));
//            }
//            break;
//        default:
//            {
//                Console.WriteLine("Error");
//            }
//            break;
//    }
//}
//outputScriptRows.Add("}break;");

//string outputRowsFile = "E:\\ConverterData\\ScriptOutput.txt";
//using (var outputFile = new StreamWriter(outputRowsFile))
//    foreach (string outputLine in outputScriptRows)
//        outputFile.WriteLine(outputLine);

//string unknownSpawnZoneFile = "E:\\ConverterData\\UnknownSpawnZone.csv";
//using (var outputFile = new StreamWriter(unknownSpawnZoneFile))
//    foreach (string outputLine in unknownSpawnZoneRows)
//        outputFile.WriteLine(outputLine);

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Populate creature templates with spawn zones for reaction spawns

// Load reactions to grab spawn zones
//string questReactionsFile = "E:\\ConverterData\\QuestReactions.csv";
//List<Dictionary<string, string>> reactionsRows = FileTool.ReadAllRowsFromFileWithHeader(questReactionsFile, "|");
//Dictionary<string, List<string>> reactionSpawnZonesByCreatureEQID = new Dictionary<string, List<string>>();
//foreach (Dictionary<string, string> reactionColumns in reactionsRows)
//{
//    string reactionType = reactionColumns["reaction_type"];
//    if (reactionType != "spawn" && reactionType != "spawnunique")
//        continue;

//    string eqID = reactionColumns["reaction_value1"];
//    string zoneShortName = reactionColumns["zone_shortname"];
//    if (reactionSpawnZonesByCreatureEQID.ContainsKey(eqID) == false)
//        reactionSpawnZonesByCreatureEQID.Add(eqID, new List<string>());
//    if (reactionSpawnZonesByCreatureEQID[eqID].Contains(zoneShortName) == false)
//        reactionSpawnZonesByCreatureEQID[eqID].Add(zoneShortName);
//}

//// Load in lookups for creature templates
//string creatureTemplatesFile = "E:\\ConverterData\\CreatureTemplates.csv";
//List<Dictionary<string, string>> columns = FileTool.ReadAllRowsFromFileWithHeader(creatureTemplatesFile, "|");
//for (int i = 0; i < columns.Count; i++)
//{
//    string eqId = columns[i]["eq_id"].Trim();
//    if (reactionSpawnZonesByCreatureEQID.ContainsKey(eqId))
//    {
//        string creatureSpawnZones = columns[i]["spawnzones"];
//        foreach (string reactionSpawnZone in reactionSpawnZonesByCreatureEQID[eqId])
//        {
//            if (creatureSpawnZones.Contains(reactionSpawnZone) == false)
//            {
//                if (creatureSpawnZones.Length > 0)
//                    creatureSpawnZones += ",";
//                creatureSpawnZones += reactionSpawnZone;
//            }
//        }
//        columns[i]["spawnzones"] = creatureSpawnZones;
//    }
//}

//string creatureTemplatesWithUpdatesFile = "E:\\ConverterData\\CreatureTemplatesWithUpdates.csv";
//FileTool.WriteFile(creatureTemplatesWithUpdatesFile, columns);

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Identify and mark creature templates that are invalid for the project

// Read in NPC-group relationships and if the expansion is valid
//Dictionary<int, List<int>> eqNPCIDsByGroupID = new Dictionary<int, List<int>>();
//Dictionary<int, List<int>> groupIDsByEQNPCID = new Dictionary<int, List<int>>();
//Dictionary<int, bool> isExpansionValidByGroupID = new Dictionary<int, bool>();
//string spawnEntriesFile = "E:\\ConverterData\\SpawnEntries.csv";
//List<Dictionary<string, string>> spawnEntriesRows = FileTool.ReadAllRowsFromFileWithHeader(spawnEntriesFile, "|");
//foreach (Dictionary<string, string> spawnEntriesColumn in spawnEntriesRows)
//{
//    int spawnGroupID = int.Parse(spawnEntriesColumn["spawngroupID"]);
//    int npcID = int.Parse(spawnEntriesColumn["npcID"]);
//    //if (npcID == 72103)
//    //{
//    //    int x = 5;
//    //    int y = 5;
//    //}
//    if (spawnGroupID == 222432)
//    {
//        int x = 5;
//        int y = 5;
//    }

//    // Group ID container
//    if (eqNPCIDsByGroupID.ContainsKey(spawnGroupID) == false)
//        eqNPCIDsByGroupID.Add(spawnGroupID, new List<int>());
//    eqNPCIDsByGroupID[spawnGroupID].Add(npcID);

//    if (groupIDsByEQNPCID.ContainsKey(npcID) == false)
//        groupIDsByEQNPCID.Add(npcID, new List<int>());
//    groupIDsByEQNPCID[npcID].Add(spawnGroupID);

//    //// Expansion
//    //int minExpansionID = int.Parse(spawnEntriesColumn["min_expansion"]);
//    //bool isExpansionValid = (minExpansionID <= 2);
//    //if (isExpansionValidByGroupID.ContainsKey(spawnGroupID) == false)
//    //    isExpansionValidByGroupID[spawnGroupID] = isExpansionValid;
//    //else if (isExpansionValid == false)
//    //    isExpansionValidByGroupID[spawnGroupID] = true;
//}

//string spawnInstancesFile = "E:\\ConverterData\\SpawnInstances.csv";
//List<Dictionary<string, string>> spawnInstancesRows = FileTool.ReadAllRowsFromFileWithHeader(spawnInstancesFile, "|");
//foreach (Dictionary<string, string> spawnInstancesRow in spawnInstancesRows)
//{
//    int spawnGroupID = int.Parse(spawnInstancesRow["spawngroupid"]);
//    int minExpansionID = int.Parse(spawnInstancesRow["min_expansion"]);
//    bool isExpansionValid = (minExpansionID <= 2);
//    if (isExpansionValidByGroupID.ContainsKey(spawnGroupID) == false)
//        isExpansionValidByGroupID[spawnGroupID] = isExpansionValid;
//    else if (isExpansionValid == false)
//        isExpansionValidByGroupID[spawnGroupID] = true;



//    //int npcID = int.Parse(spawnEntriesColumn["npcID"]);
//    ////if (npcID == 72103)
//    ////{
//    ////    int x = 5;
//    ////    int y = 5;
//    ////}
//    //if (spawnGroupID == 222432)
//    //{
//    //    int x = 5;
//    //    int y = 5;
//    //}

//    //// Group ID container
//    //if (eqNPCIDsByGroupID.ContainsKey(spawnGroupID) == false)
//    //    eqNPCIDsByGroupID.Add(spawnGroupID, new List<int>());
//    //eqNPCIDsByGroupID[spawnGroupID].Add(npcID);

//    //if (groupIDsByEQNPCID.ContainsKey(npcID) == false)
//    //    groupIDsByEQNPCID.Add(npcID, new List<int>());
//    //groupIDsByEQNPCID[npcID].Add(spawnGroupID);

//    //// Expansion

//}

//// Update the creature templates
//string creatureTemplatesFile = "E:\\ConverterData\\CreatureTemplates.csv";
//List<Dictionary<string, string>> creatureTemplatesRows = FileTool.ReadAllRowsFromFileWithHeader(creatureTemplatesFile, "|");
//foreach(Dictionary<string, string> creatureTemplateColumn in creatureTemplatesRows)
//{
//    // Update the column if this should be deleted
//    int npcID = int.Parse(creatureTemplateColumn["eq_id"]);
//    if (npcID == 72103)
//    {
//        int x = 5;
//        int y = 5;
//    }
//    bool hasValidInstance = false;
//    bool hasInvalidInstance = false;
//    if (groupIDsByEQNPCID.ContainsKey(npcID) == true)
//    {
//        foreach (int groupID in groupIDsByEQNPCID[npcID])
//        {
//            if (isExpansionValidByGroupID.ContainsKey(groupID) && isExpansionValidByGroupID[groupID])
//                hasValidInstance = true;
//            else
//                hasInvalidInstance = true;
//        }
//    }
//    creatureTemplateColumn["ValidInstances"] = hasValidInstance ? "1" : "0";
//    creatureTemplateColumn["InvalidInstances"] = hasInvalidInstance ? "1" : "0";
//}

//string creatureTemplatesWithUpdatesFile = "E:\\ConverterData\\CreatureTemplatesWithUpdates.csv";
//FileTool.WriteFile(creatureTemplatesWithUpdatesFile, creatureTemplatesRows);

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Convert tradeskills into a flattened list

Console.WriteLine("[1] Flatten Tradeskill Data");
Console.WriteLine("[2] Update Spawn Locations");
Console.WriteLine("[3] Write tradeskill IDs for produced items in item templates");
Console.WriteLine(" ");
Console.Write("Command: ");
string? enteredCommand = Console.ReadLine();
if (enteredCommand != null)
{
    switch (enteredCommand)
    {
        case "1": UtilityConsole.ConvertTradeskillsToFlattenedList(); break;
        case "2": UtilityConsole.UpdateSpawnLocations(); break;
        case "3": UtilityConsole.UpdateTradeskillReferencesInItemTemplates(); break;
        default: Console.WriteLine("Unknown command entered"); break;
    }
}
Console.WriteLine("Done. Press any key...");
Console.ReadKey();
