﻿//  Author: Nathan Handley(nathanhandley @protonmail.com)
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

using System.Text;
using MySql.Data.MySqlClient;

namespace EQWOWPregenScripts
{
    internal class UtilityConsole
    {
        private static string ConnectionString = "Server=127.0.0.1;Database=acore_world;Uid=scriptread;Pwd=scriptreadpass;";

        public static void ConvertTradeskillsToFlattenedList()
        {
            // Get the item lookup
            Dictionary<string, string> itemNamesByEQID = new Dictionary<string, string>();
            Dictionary<string, int> itemMaxChangesByEQID = new Dictionary<string, int>();
            string itemFile = "E:\\ConverterData\\ItemTemplates.csv";
            List<Dictionary<string, string>> itemFileRows = FileTool.ReadAllRowsFromFileWithHeader(itemFile, "|");
            foreach (Dictionary<string, string> itemFileColumns in itemFileRows)
            {
                if (itemFileColumns["enabled"] == "0")
                    continue;
                itemNamesByEQID.Add(itemFileColumns["id"].Trim(), itemFileColumns["Name"].Trim());
                if (itemFileColumns["stacksize"] == "1" || itemFileColumns["stacksize"] == "0")
                    itemMaxChangesByEQID.Add(itemFileColumns["id"].Trim(), int.Parse(itemFileColumns["maxcharges"].Trim()));
            }

            // Read the recipes
            string tradeskillRecipesFile = "E:\\ConverterData\\Tradeskill_Recipe.csv";
            List<Dictionary<string, string>> tradeskillRecipeRows = FileTool.ReadAllRowsFromFileWithHeader(tradeskillRecipesFile, "|");
            Dictionary<string, TradeskillRecipe> recipesByID = new Dictionary<string, TradeskillRecipe>();
            foreach (Dictionary<string, string> tradeskillRecipeColumns in tradeskillRecipeRows)
            {
                TradeskillRecipe recipe = new TradeskillRecipe();
                recipe.EQRecipeID = tradeskillRecipeColumns["id"].Trim();
                recipe.RecipeName = tradeskillRecipeColumns["name"].Trim();
                recipe.RecipeOriginalName = tradeskillRecipeColumns["name"].Trim();
                recipe.EQTradeskillID = tradeskillRecipeColumns["tradeskill"].Trim();
                recipe.SkillNeeded = tradeskillRecipeColumns["skillneeded"].Trim();
                recipe.Trivial = tradeskillRecipeColumns["trivial"].Trim();
                recipe.NoFail = tradeskillRecipeColumns["nofail"].Trim();
                recipe.ReplaceContainer = tradeskillRecipeColumns["replace_container"].Trim();
                recipe.MinExpansion = tradeskillRecipeColumns["min_expansion"].Trim();
                if (int.Parse(tradeskillRecipeColumns["min_expansion"]) > 2)
                    recipe.Enabled = false;
                recipesByID.Add(recipe.EQRecipeID, recipe);
            }

            // Add the items
            string tradeskillRecipeItems = "E:\\ConverterData\\Tradeskill_Recipe_Entries.csv";
            List<Dictionary<string, string>> tradeskillRecipeItemRows = FileTool.ReadAllRowsFromFileWithHeader(tradeskillRecipeItems, "|");
            Dictionary<string, List<TradeskillRecipe>> recipesByProducedItemID = new Dictionary<string, List<TradeskillRecipe>>();
            foreach (Dictionary<string, string> tradeskillRecipeItemColumns in tradeskillRecipeItemRows)
            {
                string recipeID = tradeskillRecipeItemColumns["recipe_id"].Trim();
                if (recipesByID.ContainsKey(recipeID) == false)
                {
                    Console.WriteLine("Could not find recipe with ID " + recipeID);
                    continue;
                }
                TradeskillRecipe curRecipe = recipesByID[recipeID];
                string itemID = tradeskillRecipeItemColumns["item_id"].Trim();
                string itemName = string.Empty;
                string isContainer = tradeskillRecipeItemColumns["iscontainer"].Trim();
                if (itemNamesByEQID.ContainsKey(itemID) == true)
                    itemName = itemNamesByEQID[itemID];
                else
                {
                    itemName = "INVALID_ID";
                    if (isContainer == "0" || int.Parse(isContainer) > 1000)
                        curRecipe.Enabled = false;
                }

                // Only capture containers for generic
                if (isContainer != "0")
                {
                    if (curRecipe.EQTradeskillID == "75")
                        curRecipe.ContainerItems.Add(new TradeskillItem(itemID, itemName));
                    continue;
                }

                // Component items
                string componentCount = tradeskillRecipeItemColumns["componentcount"].Trim();
                if (componentCount != "0")
                {
                    bool collisionFound = false;
                    foreach (TradeskillItem item in curRecipe.ComponentItems)
                    {
                        if (item.EQItemID == itemID)
                        {
                            Console.WriteLine("Recipe " + curRecipe.EQRecipeID + " component item collision with id " + item.EQItemID + " and name " + item.ItemName);
                            collisionFound = true;
                            break;
                        }
                    }
                    if (collisionFound == true)
                        continue;
                    curRecipe.ComponentItems.Add(new TradeskillItem(itemID, itemName, componentCount));
                }

                // Produced items
                string successCount = tradeskillRecipeItemColumns["successcount"].Trim();
                if (successCount != "0")
                {
                    bool collisionFound = false;
                    foreach (TradeskillItem item in curRecipe.ProducedItems)
                    {
                        if (item.EQItemID == itemID)
                        {
                            Console.WriteLine("Recipe " + curRecipe.EQRecipeID + " produced item collision with id " + item.EQItemID + " and name " + item.ItemName);
                            collisionFound = true;
                            break;
                        }
                    }
                    for (int i = curRecipe.ComponentItems.Count - 1; i >= 0; i--)
                    {
                        TradeskillItem curComponentItem = curRecipe.ComponentItems[i];
                        if (curComponentItem.EQItemID == itemID)
                        {
                            if (itemMaxChangesByEQID.ContainsKey(curComponentItem.EQItemID) && itemMaxChangesByEQID[curComponentItem.EQItemID] > 0)
                            {
                                curRecipe.ProducedItems.Add(new TradeskillItem(itemID, itemName, "1"));
                                curRecipe.RecipeName = curRecipe.RecipeOriginalName + " (Recharge)";
                            }
                            else
                            {
                                curRecipe.ComponentItems.RemoveAt(i);
                                curRecipe.RequiredItems.Add(new TradeskillItem(itemID, itemName));
                            }
                            collisionFound = true;
                            break;
                        }
                    }
                    if (collisionFound == true)
                        continue;
                    curRecipe.ProducedItems.Add(new TradeskillItem(itemID, itemName, successCount));
                    if (recipesByProducedItemID.ContainsKey(itemID) == false)
                        recipesByProducedItemID.Add(itemID, new List<TradeskillRecipe>());
                    if (recipesByProducedItemID[itemID].Contains(curRecipe) == false)
                        recipesByProducedItemID[itemID].Add(curRecipe);
                }
            }

            // Loop through and update any recipes as invalid if it's based on invalid components
            bool foundMoreInvalidItems = false;
            do
            {
                foundMoreInvalidItems = false;
                foreach (TradeskillRecipe recipe in recipesByID.Values)
                {
                    if (recipe.Enabled == false)
                        continue;

                    foreach (TradeskillItem componentItem in recipe.ComponentItems)
                    {
                        if (recipesByProducedItemID.ContainsKey(componentItem.EQItemID))
                        {
                            bool hasEnabled = false;
                            foreach (TradeskillRecipe producingRecipe in recipesByProducedItemID[componentItem.EQItemID])
                            {
                                if (producingRecipe.Enabled == true)
                                    hasEnabled = true;
                            }
                            if (hasEnabled == false)
                            {
                                recipe.Enabled = false;
                                foundMoreInvalidItems = true;
                            }
                        }
                    }
                    foreach (TradeskillItem requiredItem in recipe.RequiredItems)
                    {
                        if (recipesByProducedItemID.ContainsKey(requiredItem.EQItemID))
                        {
                            bool hasEnabled = false;
                            foreach (TradeskillRecipe producingRecipe in recipesByProducedItemID[requiredItem.EQItemID])
                            {
                                if (producingRecipe.Enabled == true)
                                    hasEnabled = true;
                            }
                            if (hasEnabled == false)
                            {
                                recipe.Enabled = false;
                                foundMoreInvalidItems = true;
                            }
                        }
                    }
                }
            } while (foundMoreInvalidItems == true);

            // Determine how big to make each output section
            int maxNumOfComponents = 0;
            int maxNumOfProduced = 0;
            int maxNumOfContainers = 0;
            int maxNumOfRequired = 0;
            foreach (TradeskillRecipe recipe in recipesByID.Values)
            {
                maxNumOfComponents = Math.Max(maxNumOfComponents, recipe.ComponentItems.Count);
                maxNumOfProduced = Math.Max(maxNumOfProduced, recipe.ProducedItems.Count);
                maxNumOfContainers = Math.Max(maxNumOfContainers, recipe.ContainerItems.Count);
                maxNumOfRequired = Math.Max(maxNumOfRequired, recipe.RequiredItems.Count);
            }

            // Output a file for this
            List<string> outputLines = new List<string>();
            StringBuilder outputSB = new StringBuilder();
            outputSB.Append("eq_recipeID|enabled|name|eq_tradeskillID|skill_needed|trival|no_fail|replace_container|min_expansion");
            for (int i = 0; i < maxNumOfProduced; i++)
            {
                outputSB.Append("|produced_eqid_");
                outputSB.Append(i.ToString());
                outputSB.Append("|produced_name_");
                outputSB.Append(i.ToString());
                outputSB.Append("|produced_count_");
                outputSB.Append(i.ToString());
            }
            for (int i = 0; i < maxNumOfComponents; i++)
            {
                outputSB.Append("|component_eqid_");
                outputSB.Append(i.ToString());
                outputSB.Append("|component_name_");
                outputSB.Append(i.ToString());
                outputSB.Append("|component_count_");
                outputSB.Append(i.ToString());
            }
            for (int i = 0; i < maxNumOfContainers; i++)
            {
                outputSB.Append("|container_eqid_");
                outputSB.Append(i.ToString());
                outputSB.Append("|container_name_");
                outputSB.Append(i.ToString());
            }
            for (int i = 0; i < maxNumOfRequired; i++)
            {
                outputSB.Append("|required_eqid_");
                outputSB.Append(i.ToString());
                outputSB.Append("|required_name_");
                outputSB.Append(i.ToString());
            }
            outputLines.Add(outputSB.ToString());
            foreach (TradeskillRecipe recipe in recipesByID.Values)
            {
                outputSB.Clear();
                outputSB.Append(recipe.EQRecipeID);
                outputSB.Append("|");
                outputSB.Append(recipe.Enabled ? "1" : "0");
                outputSB.Append("|");
                outputSB.Append(recipe.RecipeName);
                outputSB.Append("|");
                outputSB.Append(recipe.EQTradeskillID);
                outputSB.Append("|");
                outputSB.Append(recipe.SkillNeeded);
                outputSB.Append("|");
                outputSB.Append(recipe.Trivial);
                outputSB.Append("|");
                outputSB.Append(recipe.NoFail);
                outputSB.Append("|");
                outputSB.Append(recipe.ReplaceContainer);
                outputSB.Append("|");
                outputSB.Append(recipe.MinExpansion);
                for (int i = 0; i < maxNumOfProduced; i++)
                {
                    if (i < recipe.ProducedItems.Count)
                    {
                        outputSB.Append("|");
                        outputSB.Append(recipe.ProducedItems[i].EQItemID);
                        outputSB.Append("|");
                        outputSB.Append(recipe.ProducedItems[i].ItemName);
                        outputSB.Append("|");
                        outputSB.Append(recipe.ProducedItems[i].Count);
                    }
                    else
                    {
                        outputSB.Append("|||");
                    }
                }
                for (int i = 0; i < maxNumOfComponents; i++)
                {
                    if (i < recipe.ComponentItems.Count)
                    {
                        outputSB.Append("|");
                        outputSB.Append(recipe.ComponentItems[i].EQItemID);
                        outputSB.Append("|");
                        outputSB.Append(recipe.ComponentItems[i].ItemName);
                        outputSB.Append("|");
                        outputSB.Append(recipe.ComponentItems[i].Count);
                    }
                    else
                    {
                        outputSB.Append("|||");
                    }
                }
                for (int i = 0; i < maxNumOfContainers; i++)
                {
                    if (i < recipe.ContainerItems.Count)
                    {
                        outputSB.Append("|");
                        outputSB.Append(recipe.ContainerItems[i].EQItemID);
                        outputSB.Append("|");
                        outputSB.Append(recipe.ContainerItems[i].ItemName);
                    }
                    else
                    {
                        outputSB.Append("||");
                    }
                }
                for (int i = 0; i < maxNumOfRequired; i++)
                {
                    if (i < recipe.RequiredItems.Count)
                    {
                        outputSB.Append("|");
                        outputSB.Append(recipe.RequiredItems[i].EQItemID);
                        outputSB.Append("|");
                        outputSB.Append(recipe.RequiredItems[i].ItemName);
                    }
                    else
                    {
                        outputSB.Append("||");
                    }
                }
                outputLines.Add(outputSB.ToString());
            }

            string outputFileName = "E:\\ConverterData\\ConvertedRecipes.csv";
            using (var outputFile = new StreamWriter(outputFileName))
                foreach (string outputLine in outputLines)
                    outputFile.WriteLine(outputLine);
        }

        public static void UpdateSpawnLocations()
        {
            // Get a map of the instance IDs
            Dictionary<string, string> instanceIDsByCreatureGUID = new Dictionary<string, string>();
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT guid, Comment FROM creature WHERE guid >= 310000 AND guid <= 399999";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string? guid = reader["guid"].ToString();
                            string? comments = reader["Comment"].ToString();
                            if (guid != null && comments != null)
                            {
                                if (comments.Trim().Length == 0)
                                    continue;
                                string[] blocks = comments.Split(" ");
                                if (blocks.Length == 1)
                                    continue;
                                string instanceID = blocks[blocks.Length-1];
                                instanceIDsByCreatureGUID.Add(guid, instanceID);
                            }
                        }
                    }
                }
                connection.Close();
            }

            // Read the spawn locations to update
            Dictionary<string, SpawnLocation> spawnLocationsByInstanceID = new Dictionary<string, SpawnLocation>();
            string newSpawnLocationsFile = "E:\\ConverterData\\NewSpawnLocations.txt";
            List<Dictionary<string, string>> newSpawnLocationsRows = FileTool.ReadAllRowsFromFileWithHeader(newSpawnLocationsFile, "|");
            foreach (Dictionary<string, string> newSpawnLocationsColumns in newSpawnLocationsRows)
            {
                string creatureGUID = newSpawnLocationsColumns["CreatureGUID"];
                if (instanceIDsByCreatureGUID.ContainsKey(creatureGUID) == false)
                {
                    Console.WriteLine("Error, could not find creatureGUID of " + creatureGUID + " in the instanceIDsByCreatureGUID");
                    continue;
                }
                string instanceID = instanceIDsByCreatureGUID[creatureGUID];

                SpawnLocation newSpawnLocation = new SpawnLocation();
                newSpawnLocation.X = newSpawnLocationsColumns["X"];
                newSpawnLocation.Y = newSpawnLocationsColumns["Y"];
                newSpawnLocation.Z = newSpawnLocationsColumns["Z"];
                spawnLocationsByInstanceID.Add(instanceID, newSpawnLocation);
            }

            // Update records in the spawn instance file into a new file
            string inputSpawnInstancesFile = "E:\\ConverterData\\SpawnInstances.csv";
            List<Dictionary<string, string>> spawnInstanceRows = FileTool.ReadAllRowsFromFileWithHeader(inputSpawnInstancesFile, "|");
            foreach (Dictionary<string, string> spawnInstanceColumns in spawnInstanceRows)
            {
                if (spawnLocationsByInstanceID.ContainsKey(spawnInstanceColumns["eqid"]))
                {
                    SpawnLocation curLocation = spawnLocationsByInstanceID[spawnInstanceColumns["eqid"]];
                    spawnInstanceColumns["x"] = curLocation.X;
                    spawnInstanceColumns["y"] = curLocation.Y;
                    spawnInstanceColumns["z"] = curLocation.Z;
                }
            }
            string outputSpawnInstancesFile = "E:\\ConverterData\\SpawnInstancesUpdated.csv";
            FileTool.WriteFile(outputSpawnInstancesFile, spawnInstanceRows);
        }

        public static void UpdateTradeskillReferencesInItemTemplates()
        {
            // Read in the recipe-item relationships
            string tradeskillRecipesFile = "E:\\ConverterData\\TradeskillRecipes.csv";
            Dictionary<string, List<string>> eqItemIDsByProducingTradeskillID = new Dictionary<string, List<string>>();
            List<Dictionary<string, string>> recipeRows = FileTool.ReadAllRowsFromFileWithHeader(tradeskillRecipesFile, "|");
            foreach (Dictionary<string, string> recipeColumns in recipeRows)
            {
                if (recipeColumns["enabled"] == "0")
                    continue;
                string producingTradeskillID = recipeColumns["eq_tradeskillID"];
                if (producingTradeskillID == "75")
                    continue;
                List<string> eqItemIDs = new List<string>();
                if (recipeColumns["produced_eqid_0"].Length > 0)
                    eqItemIDs.Add(recipeColumns["produced_eqid_0"]);
                if (recipeColumns["produced_eqid_1"].Length > 0)
                    eqItemIDs.Add(recipeColumns["produced_eqid_1"]);
                if (recipeColumns["produced_eqid_2"].Length > 0)
                    eqItemIDs.Add(recipeColumns["produced_eqid_2"]);
                if (recipeColumns["produced_eqid_3"].Length > 0)
                    eqItemIDs.Add(recipeColumns["produced_eqid_3"]);
                foreach (string eqItemID in eqItemIDs)
                {
                    if (eqItemIDsByProducingTradeskillID.ContainsKey(eqItemID) == false)
                        eqItemIDsByProducingTradeskillID.Add(eqItemID, new List<string>());
                    if (eqItemIDsByProducingTradeskillID[eqItemID].Contains(producingTradeskillID) == false)
                        eqItemIDsByProducingTradeskillID[eqItemID].Add(producingTradeskillID);
                }
            }

            // Write the relationships to the item templates file
            string itemFile = "E:\\ConverterData\\ItemTemplates.csv";
            List<Dictionary<string, string>> itemFileRows = FileTool.ReadAllRowsFromFileWithHeader(itemFile, "|");
            foreach (Dictionary<string, string> itemFileColumns in itemFileRows)
            {
                if (itemFileColumns["enabled"] == "0")
                    continue;
                string eqItemID = itemFileColumns["id"];
                if (eqItemIDsByProducingTradeskillID.ContainsKey(eqItemID) == true)
                {
                    string tradeskillIDString = string.Empty;
                    for (int i = 0; i < eqItemIDsByProducingTradeskillID[eqItemID].Count; i++)
                    {
                        if (i > 0)
                            tradeskillIDString += ", ";
                        tradeskillIDString += eqItemIDsByProducingTradeskillID[eqItemID][i];
                    }
                    itemFileColumns["source_tradeskill"] = tradeskillIDString;
                }
            }
            string itemFileOutput = "E:\\ConverterData\\ItemTemplatesOutput.csv";
            FileTool.WriteFile(itemFileOutput, itemFileRows);
        }
    
        public static void ExtractSpellsEFF()
        {
            EQSpellsEFF spellsEFF = new EQSpellsEFF();
            spellsEFF.LoadFromDisk("E:\\ConverterData\\spells.eff");
            List<string> outputLines = new List<string>();

            // Header
            StringBuilder outputSB = new StringBuilder();
            outputSB.Append("EffectID|");
            outputSB.Append("EffectTier|");
            outputSB.Append("TypeID|");
            outputSB.Append("SpriteListEffect|");
            outputSB.Append("SoundID|");
            for (int i = 0; i < 3; i++)
            {
                outputSB.Append("SpriteName" + i + "|");
                outputSB.Append("LocationID" + i + "|");
                outputSB.Append("EmissionType" + i + "|");
                outputSB.Append("ColorR" + i + "|");
                outputSB.Append("ColorG" + i + "|");
                outputSB.Append("ColorB" + i + "|");
                outputSB.Append("ColorA" + i + "|");
                outputSB.Append("Gravity" + i + "|");
                outputSB.Append("EmitterX" + i + "|");
                outputSB.Append("EmitterY" + i + "|");
                outputSB.Append("EmitterZ" + i + "|");
                outputSB.Append("SpawnRadius" + i + "|");
                outputSB.Append("SpawnAngle" + i + "|");
                outputSB.Append("SpawnLifespan" + i + "|");
                outputSB.Append("SpawnVelocity" + i + "|");
                outputSB.Append("SpawnRate" + i + "|");
                outputSB.Append("SpawnScale" + i + "|");
            }
            for (int i = 0; i < 12; i++)
                outputSB.Append("SpriteListName" + i + "|");
            for (int i = 0; i < 12; i++)
                outputSB.Append("SpriteListUnknown"+i+"|");
            for (int i = 0; i < 12; i++)
                outputSB.Append("SpriteListCircularShift"+i+"|");
            for (int i = 0; i < 12; i++)
                outputSB.Append("SpriteListVertForce"+i+"|");
            for (int i = 0; i < 12; i++)
                outputSB.Append("SpriteListRadii"+i+"|");
            for (int i = 0; i < 12; i++)
                outputSB.Append("SpriteListMovement"+i+"|");
            for (int i = 0; i < 12; i++)
                outputSB.Append("SpriteListScale"+i+"|");
            //for (int i = 0; i < 9; i++)
            //    outputSB.Append("Unknown" + i + "|");
            outputLines.Add(outputSB.ToString());

            // Data
            for (int j = 0; j < spellsEFF.SpellEffects.Count; j++)
            {
                EQSpellsEFF.EQSpellEffect curEffect = spellsEFF.SpellEffects[j];
                for (int k = 0; k < 3; k++)
                {
                    outputSB.Clear();
                    outputSB.Append(j + "|");
                    outputSB.Append(k + "|");
                    outputSB.Append(curEffect.SectionDatas[k].TypeString + "|");
                    outputSB.Append(curEffect.SectionDatas[k].SpriteListEffect + "|");
                    outputSB.Append(curEffect.SectionDatas[k].SoundID + "|");
                    for (int i = 0; i < 3; i++)
                    {
                        outputSB.Append(curEffect.SectionDatas[k].SpriteNames[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].LocationIDs[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmissionTypeIDs[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterColors[i].R + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterColors[i].G + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterColors[i].B + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterColors[i].A + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterGravities[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterSpawnXs[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterSpawnYs[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterSpawnZs[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterSpawnRadii[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterSpawnAngles[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterSpawnLifespans[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterSpawnVelocities[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterSpawnRates[i] + "|");
                        outputSB.Append(curEffect.SectionDatas[k].EmitterSpawnScale[i] + "|");
                    }
                    for (int i = 0; i < 12; i++)
                        outputSB.Append(curEffect.SectionDatas[k].SpriteListNames[i] + "|");
                    for (int i = 0; i < 12; i++)
                        outputSB.Append(curEffect.SectionDatas[k].SpriteListUnknown[i] + "|");
                    for (int i = 0; i < 12; i++)
                        outputSB.Append(curEffect.SectionDatas[k].SpriteListCircularShifts[i] + "|");
                    for (int i = 0; i < 12; i++)
                        outputSB.Append(curEffect.SectionDatas[k].SpriteListVerticalForces[i] + "|");
                    for (int i = 0; i < 12; i++)
                        outputSB.Append(curEffect.SectionDatas[k].SpriteListRadii[i] + "|");
                    for (int i = 0; i < 12; i++)
                        outputSB.Append(curEffect.SectionDatas[k].SpriteListMovements[i] + "|");
                    for (int i = 0; i < 12; i++)
                        outputSB.Append(curEffect.SectionDatas[k].SpriteListScales[i] + "|");
                    //for (int i = 0; i < 9; i++)
                    //    outputSB.Append(curEffect.SectionDatas[k].UnknownData[i] + "|");
                    outputLines.Add(outputSB.ToString());
                }
            }
            string outputFileName = "E:\\ConverterData\\spellseff.csv";
            using (var outputFile = new StreamWriter(outputFileName))
                foreach (string outputLine in outputLines)
                    outputFile.WriteLine(outputLine);
        }
    }
}
