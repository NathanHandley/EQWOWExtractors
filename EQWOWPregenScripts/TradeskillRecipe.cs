using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQWOWPregenScripts
{
    internal class TradeskillRecipe
    {
        public bool Enabled = true;
        public string EQRecipeID = string.Empty;
        public string RecipeOriginalName = string.Empty;
        public string RecipeName = string.Empty;
        public string EQTradeskillID = string.Empty;
        public string SkillNeeded = string.Empty;
        public string Trivial = string.Empty;
        public string NoFail = string.Empty;
        public string ReplaceContainer = string.Empty;
        public string MinExpansion = string.Empty;
        public List<TradeskillItem> ContainerItems = new List<TradeskillItem>();
        public List<TradeskillItem> ComponentItems = new List<TradeskillItem>();
        public List<TradeskillItem> ProducedItems = new List<TradeskillItem>();
        public List<TradeskillItem> RequiredItems = new List<TradeskillItem>();
    }
}
