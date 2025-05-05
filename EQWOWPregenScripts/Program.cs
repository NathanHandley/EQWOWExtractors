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


// Create item lookups
string itemTemplatesFile = "E:\\ConverterData\\itemTemplates.csv";
Dictionary<int, string> itemNamesByEQIDs = new Dictionary<int, string>();
foreach (Dictionary<string, string> itemColumns in FileTool.ReadAllRowsFromFileWithHeader(itemTemplatesFile, "|"))
    itemNamesByEQIDs.Add(Convert.ToInt32(itemColumns["id"]), itemColumns["Name"]);

// Map the item names to the first required item
string questTemplatesFileInput = "E:\\ConverterData\\QuestTemplates.csv";
List<Dictionary<string, string>> questTemplatesRows = FileTool.ReadAllRowsFromFileWithHeader(questTemplatesFileInput, "|");
for (int i = 0; i < questTemplatesRows.Count; i++)
{
    Dictionary<string, string> questColumns = questTemplatesRows[i];
    int requiredItem1ID = int.Parse(questColumns["req_item_id1"]);
    if (requiredItem1ID != -1)
    {
        if (itemNamesByEQIDs.ContainsKey(requiredItem1ID) == false)
            questColumns["req_item1_name"] = "INVALID";
        else
            questColumns["req_item1_name"] = itemNamesByEQIDs[requiredItem1ID];
    }
    int rewardItem1ID = int.Parse(questColumns["reward_item_ID1"]);
    if (rewardItem1ID != -1)
    {
        if (itemNamesByEQIDs.ContainsKey(rewardItem1ID) == false)
            questColumns["reward_item1_name"] = "INVALID";
        else
            questColumns["reward_item1_name"] = itemNamesByEQIDs[rewardItem1ID];
    }
}

// Write a new file for it
string questTemplatesFileOutput = "E:\\ConverterData\\QuestTemplatesUpdated.csv";
FileTool.WriteFile(questTemplatesFileOutput, questTemplatesRows);

Console.WriteLine("Done. Press any key...");
Console.ReadKey();
