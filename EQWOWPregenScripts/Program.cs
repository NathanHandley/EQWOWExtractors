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
