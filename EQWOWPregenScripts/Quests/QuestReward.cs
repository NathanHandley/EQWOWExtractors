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
    }
}
