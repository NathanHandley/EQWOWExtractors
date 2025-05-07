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
// Condition reactions
string questReactionsInputFileName = "E:\\ConverterData\\QuestReactionsInput.csv";
List<Dictionary<string, string>> discardedRows = new List<Dictionary<string, string>>();
List<QuestReaction> reactions = new List<QuestReaction>();
List<Dictionary<string, string>> questReactionColumnRows = FileTool.ReadAllRowsFromFileWithHeader(questReactionsInputFileName, "|");
foreach (Dictionary<string, string> questReactionColumns in questReactionColumnRows)
{
    int curReactionCount = reactions.Count;
    QuestReaction curQuestReaction = new QuestReaction();
    curQuestReaction.QuestID = int.Parse(questReactionColumns["wow_questid"]);
    curQuestReaction.ZoneShortName = questReactionColumns["zone_shortname"];
    curQuestReaction.QuestGiverName = questReactionColumns["questgiver_name"];
    string reactionString = questReactionColumns["reaction"].Trim();

    // Categorize
    if (reactionString.Contains("e.self:Say(") || reactionString.Contains("e.other:Say("))
    {
        string formattedString = StringHelper.ConvertText(reactionString.Replace("e.self:Say(", "").Replace("e.other:Say(", ""));
        curQuestReaction.ReactionType = "say";
        curQuestReaction.ReactionValue1 = formattedString;
        reactions.Add(curQuestReaction);
    }
    else if (reactionString.Contains("e.self:Shout("))
    {
        string formattedString = StringHelper.ConvertText(reactionString.Replace("e.self:Shout(", ""));
        curQuestReaction.ReactionType = "yell";
        curQuestReaction.ReactionValue1 = formattedString;
        reactions.Add(curQuestReaction);
    }
    else if (reactionString.Contains("e.self:Emote"))
    {
        curQuestReaction.ReactionType = "emote";
        string formattedString = StringHelper.ConvertText(reactionString.Replace("e.self:Emote(", ""));
        curQuestReaction.ReactionValue1 = formattedString;
        reactions.Add(curQuestReaction);
    }
    //else if (reactionString.StartsWith("eq.depop"))
    //{
    //    // eq.depop()

    //    // eq.depop(15167); -- 15167 can be any number

    //    // eq.depop_all(116007); -- 116007 can be number

    //    // eq.depop_with_timer()

    //    // eq.depop_with_timer(116063); -- 116063 can be number
    //}
    //else if (reactionString.StartsWith("eq.follow(e.other:GetID());"))
    //{
    //    // eq.move_to(-1581,-3682,-18,236,true); -- can be different numbers
    //}
    else if (reactionString.StartsWith("eq.spawn2("))
    {
        curQuestReaction.ReactionType = "spawn";
        List<string> methodParameters = StringHelper.ExtractMethodParameters(reactionString, "eq.spawn2");
        curQuestReaction.ReactionValue1 = methodParameters[0];
        if (methodParameters[3].Contains("e.self:GetX("))
        {
            curQuestReaction.ReactionValue2 = "playerX";
            if (methodParameters[3].Contains("-") || methodParameters[3].Contains("+"))
                curQuestReaction.ReactionValue6 = StringHelper.GetAddedMathPart(methodParameters[3]);
        }
        else
            curQuestReaction.ReactionValue2 = methodParameters[3];
        if (methodParameters[4].Contains("e.self:GetY("))
        {
            curQuestReaction.ReactionValue3 = "playerY";
            if (methodParameters[4].Contains("-") || methodParameters[4].Contains("+"))
                curQuestReaction.ReactionValue7 = StringHelper.GetAddedMathPart(methodParameters[4]);
        }
        else
            curQuestReaction.ReactionValue3 = methodParameters[4];
        if (methodParameters[5].Contains("e.self:GetZ("))
        {
            curQuestReaction.ReactionValue4 = "playerZ";
            if (methodParameters[5].Contains("-") || methodParameters[5].Contains("+"))
                curQuestReaction.ReactionValue8 = StringHelper.GetAddedMathPart(methodParameters[5]);
        }
        else
            curQuestReaction.ReactionValue4 = methodParameters[5];
        if (methodParameters[6].Contains("e.self:GetHeading"))
            curQuestReaction.ReactionValue5 = "playerHeading";
        else
            curQuestReaction.ReactionValue5 = methodParameters[6];
        reactions.Add(curQuestReaction);
    }
    else if (reactionString.StartsWith("eq.unique_spawn("))
    {
        curQuestReaction.ReactionType = "spawnunique";
        List<string> methodParameters = StringHelper.ExtractMethodParameters(reactionString, "unique_spawn");
        curQuestReaction.ReactionValue1 = methodParameters[0];
        if (methodParameters[3].Contains("e.self:GetX("))
        {
            curQuestReaction.ReactionValue2 = "playerX";
            if (methodParameters[3].Contains("-") || methodParameters[3].Contains("+"))
                curQuestReaction.ReactionValue6 = StringHelper.GetAddedMathPart(methodParameters[3]);
        }
        else
            curQuestReaction.ReactionValue2 = methodParameters[3];
        if (methodParameters[4].Contains("e.self:GetY("))
        {
            curQuestReaction.ReactionValue3 = "playerY";
            if (methodParameters[4].Contains("-") || methodParameters[4].Contains("+"))
                curQuestReaction.ReactionValue7 = StringHelper.GetAddedMathPart(methodParameters[4]);
        }
        else
            curQuestReaction.ReactionValue3 = methodParameters[4];
        if (methodParameters[5].Contains("e.self:GetZ("))
        {
            curQuestReaction.ReactionValue4 = "playerZ";
            if (methodParameters[5].Contains("-") || methodParameters[5].Contains("+"))
                curQuestReaction.ReactionValue8 = StringHelper.GetAddedMathPart(methodParameters[5]);
        }
        else
            curQuestReaction.ReactionValue4 = methodParameters[5];
        if (methodParameters.Count > 6 && methodParameters[6].Contains("e.self:GetHeading"))
            curQuestReaction.ReactionValue5 = "playerHeading";
        else if (methodParameters.Count > 6)
            curQuestReaction.ReactionValue5 = methodParameters[6];
        reactions.Add(curQuestReaction);
    }

    // Handle the inlined attack player
    if (reactionString.Contains("AddToHateList(e.other,1);") || reactionString.Contains("eq.attack(e.other:GetName())"))
    {
        QuestReaction attackReaction = new QuestReaction();
        attackReaction.QuestID = int.Parse(questReactionColumns["wow_questid"]);
        attackReaction.ZoneShortName = questReactionColumns["zone_shortname"];
        attackReaction.QuestGiverName = questReactionColumns["questgiver_name"];
        attackReaction.ReactionType = "attackplayer";
        reactions.Add(attackReaction);
    }

    if (reactionString.Contains("eq.depop()") || reactionString.Contains("eq.depop_with_timer()")) // TODO: Handle timer?
    {
        QuestReaction attackReaction = new QuestReaction();
        attackReaction.QuestID = int.Parse(questReactionColumns["wow_questid"]);
        attackReaction.ZoneShortName = questReactionColumns["zone_shortname"];
        attackReaction.QuestGiverName = questReactionColumns["questgiver_name"];
        attackReaction.ReactionType = "despawn";
        attackReaction.ReactionValue1 = "self";
        reactions.Add(attackReaction);
    }
    else if (reactionString.Contains("eq.follow(e.other:GetID());"))
    {

    }
    else if (reactionString.Contains("eq.depop("))
    {
        QuestReaction despawnReaction = new QuestReaction();
        despawnReaction.QuestID = int.Parse(questReactionColumns["wow_questid"]);
        despawnReaction.ZoneShortName = questReactionColumns["zone_shortname"];
        despawnReaction.QuestGiverName = questReactionColumns["questgiver_name"];
        despawnReaction.ReactionType = "despawn";
        List<string> methodParameters = StringHelper.ExtractMethodParameters(reactionString, "depop");
        despawnReaction.ReactionValue1 = methodParameters[0];
        reactions.Add(despawnReaction);
    }
    else if (reactionString.Contains("eq.depop_with_timer(")) // TODO: Handle timer?
    {
        QuestReaction despawnReaction = new QuestReaction();
        despawnReaction.QuestID = int.Parse(questReactionColumns["wow_questid"]);
        despawnReaction.ZoneShortName = questReactionColumns["zone_shortname"];
        despawnReaction.QuestGiverName = questReactionColumns["questgiver_name"];
        despawnReaction.ReactionType = "despawn";
        List<string> methodParameters = StringHelper.ExtractMethodParameters(reactionString, "depop_with_timer");
        despawnReaction.ReactionValue1 = methodParameters[0];
        reactions.Add(despawnReaction);
    }

    // Everything else is discarded
    if (curReactionCount == reactions.Count)
    {
        Dictionary<string, string> discardedRow = new Dictionary<string, string>();
        discardedRow.Add("wow_questid", curQuestReaction.QuestID.ToString());
        discardedRow.Add("zone_shortname", curQuestReaction.ZoneShortName);
        discardedRow.Add("questgiver_name", curQuestReaction.QuestGiverName);
        discardedRow.Add("reaction", reactionString);
        discardedRows.Add(discardedRow);
    }
}

// Write parsed rows
string questReactionsOutputFileName = "E:\\ConverterData\\QuestReactionsOutput.csv";
List<Dictionary<string, string>> outputReactionRows = new List<Dictionary<string, string>>();
foreach (QuestReaction reaction in reactions)
{
    Dictionary<string, string> outputReactionRow = new Dictionary<string, string>();
    outputReactionRow.Add("wow_questid", reaction.QuestID.ToString());
    outputReactionRow.Add("zone_shortname", reaction.ZoneShortName);
    outputReactionRow.Add("questgiver_name", reaction.QuestGiverName);
    outputReactionRow.Add("reaction_type", reaction.ReactionType);
    outputReactionRow.Add("reaction_value1", reaction.ReactionValue1);
    outputReactionRow.Add("reaction_value2", reaction.ReactionValue2);
    outputReactionRow.Add("reaction_value3", reaction.ReactionValue3);
    outputReactionRow.Add("reaction_value4", reaction.ReactionValue4);
    outputReactionRow.Add("reaction_value5", reaction.ReactionValue5);
    outputReactionRow.Add("reaction_value6", reaction.ReactionValue6);
    outputReactionRow.Add("reaction_value7", reaction.ReactionValue7);
    outputReactionRow.Add("reaction_value8", reaction.ReactionValue8);
    outputReactionRows.Add(outputReactionRow);
}
FileTool.WriteFile(questReactionsOutputFileName, outputReactionRows);

// Write discarded rows
string questReactionsDiscardedFileName = "E:\\ConverterData\\QuestReactionsDiscarded.csv";
FileTool.WriteFile(questReactionsDiscardedFileName, discardedRows);

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Console.WriteLine("Done. Press any key...");
Console.ReadKey();
