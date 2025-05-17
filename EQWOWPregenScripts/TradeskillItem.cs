using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQWOWPregenScripts
{
    internal class TradeskillItem
    {
        public string EQItemID = string.Empty;
        public string ItemName = string.Empty;
        public string Count = string.Empty;

        public TradeskillItem(string eQItemID, string itemName)
        {
            EQItemID = eQItemID;
            ItemName = itemName;
        }
        public TradeskillItem(string eQItemID, string itemName, string count)
        {
            EQItemID = eQItemID;
            ItemName = itemName;
            Count = count;
        }
    }
}
