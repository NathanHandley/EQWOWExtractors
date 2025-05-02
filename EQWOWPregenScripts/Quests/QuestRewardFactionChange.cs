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
    public class QuestRewardFactionChange
    {
        public int FactionId;
        public int ChangeAmount;
        public QuestRewardFactionChange() { }
        public QuestRewardFactionChange(int factionId, int changeAmount)
        {
            FactionId = factionId;
            ChangeAmount = changeAmount;
        }

        static public QuestRewardFactionChange? GetFactionChangeFromLine(string line, string zoneShortName, string questgiverName, ref List<ExceptionLine> exceptionLines)
        {
            QuestRewardFactionChange? factionChange = null;
            // Hard-coded
            if (zoneShortName == "innothule" && questgiverName == "Lynuga")
                return null;

            // Strip comments
            string workingLine = line.Split("--")[0];

            // TODO: Handle these conditions
            if (workingLine.Contains(" or"))
                return null;
            if (workingLine.Contains("random"))
                return null;
            if (workingLine.Contains("ChooseRandom"))
                return null;

            // Clean out the line and pull the values
            workingLine = workingLine.Replace("e.other:Faction(e.self,", "");
            workingLine = workingLine.Replace(";", "");
            string[] blocks = workingLine.Split(",");
            int factionID = int.Parse(blocks[0]);
            int changeAmt = int.Parse(blocks[1].Replace(")", ""));
            factionChange = new QuestRewardFactionChange(factionID, changeAmt);

            return factionChange;
        }
    }
}
