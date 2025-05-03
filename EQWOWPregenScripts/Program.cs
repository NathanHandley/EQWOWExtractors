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

List<ExceptionLine> exceptionLines = new List<ExceptionLine>();
List<Quest> quests = new List<Quest>();

FileProcessor fileProcessor = new FileProcessor();
string zoneQuestFolderRoot = Path.Combine("E:\\ConverterData\\Quests", "zonequests");
string[] zoneFolders = Directory.GetDirectories(zoneQuestFolderRoot);
List<string> filesToSkip = new List<string>() { "Shuttle_I", "Shuttle_II", "Shuttle_III", "Shuttle_IV", "SirensBane", "Stormbreaker", "Golden_Maiden", 
    "Sea_King", "Suddenly", "pirate_runners_skiff", "Maidens_Voyage", "Muckskimmer", "Captains_Skiff", "Barrel_Barge", "Bloated_Belly", "script_init" };
foreach (string zoneFolder in zoneFolders)
{
    // Shortname
    string zoneShortName = Path.GetFileName(zoneFolder);

    string[] questNPCFiles = Directory.GetFiles(zoneFolder, "*.lua");
    foreach (string questNPCFile in questNPCFiles)
    {
        string npcName = Path.GetFileNameWithoutExtension(questNPCFile);
        if (filesToSkip.Contains(npcName))
            continue;

        fileProcessor.ProcessFile(questNPCFile, zoneShortName, ref exceptionLines, ref quests);
    }
}
Quest.OutputQuests(quests);
ExceptionLine.OutputExceptionLines(exceptionLines);

Console.WriteLine("Done. Press any key...");
Console.ReadKey();
