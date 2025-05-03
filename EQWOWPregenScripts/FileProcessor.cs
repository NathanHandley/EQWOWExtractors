using EQWOWPregenScripts.Quests;

namespace EQWOWPregenScripts
{
    internal class FileProcessor
    {
        private void ExtractFunctionsAndVariables(string npcName, string zoneShortName, List<string> lines, ref List<ExceptionLine> exceptionLines, out List<string> variableLines,
            out List<FunctionBlock> functionBlocks)
        {
            variableLines = new List<string>();
            functionBlocks = new List<FunctionBlock>();
            for (int i = 0; i < lines.Count; i++)
            {
                string curLine = lines[i];

                // Skip blank lines
                if (curLine.Length == 0)
                    continue;

                if (curLine.StartsWith("function event_timer(e)") && (npcName == "Lady_Vox" || npcName == "Lord_Nagafen"))
                {
                    i += 1;
                    for (int j = i; j < lines.Count; j++)
                    {
                        // Discard the event timers for vox and naggy
                        if (lines[j].StartsWith("function event_death") == true)
                        {
                            i = j - 1;
                            continue;
                        }
                    }
                    continue;
                }
                if (curLine.StartsWith("function"))
                {
                    FunctionBlock newFunctionBlock = new FunctionBlock();
                    int readLineCount;
                    newFunctionBlock.LoadStartingAtLine(lines, i, npcName, zoneShortName, ref exceptionLines, out readLineCount);
                    if (readLineCount > 0)
                        i += readLineCount - 1;
                    functionBlocks.Add(newFunctionBlock);
                }
                else if (curLine.StartsWith("local "))
                {
                    string variableLine = curLine;
                    if (curLine.Contains("{"))
                    {
                        int readLineCount;
                        variableLine = StringHelper.ExtractCurlyBraceContentRaw(lines, i, out readLineCount);
                        if (readLineCount > 0)
                            i += readLineCount;
                    }
                    variableLines.Add(variableLine);
                }
                else if (curLine.StartsWith("count = ") || curLine.StartsWith("QUEST_TEXT = "))
                {
                    variableLines.Add(curLine);
                }
                else if (curLine == "end" || curLine.Contains("item_lib.return_items"))
                    continue;
                else
                    exceptionLines.Add(new ExceptionLine(npcName, zoneShortName, "ProcessFile unparsed line", i, lines[i]));
            }
        }

        public void ProcessFile(string fullFilePath, string zoneShortName, ref List<ExceptionLine> exceptionLines, ref List<Quest> quests)
        {
            string npcName = Path.GetFileNameWithoutExtension(fullFilePath);

            // Grab the lines of text, stripping out any comments or blanks
            List<string> lines = new List<string>();
            using (var sr = new StreamReader(fullFilePath))
            {
                string? curLine;
                while ((curLine = sr.ReadLine()) != null)
                {
                    if (curLine != null)
                    {
                        string lineToAdd = curLine.Split("--")[0].Trim();
                        lines.Add(lineToAdd);
                    }
                }
            }

            // Process the lines looking for function blocks and variables
            List<string> variableLines;
            List<FunctionBlock> functionBlocks;
            ExtractFunctionsAndVariables(npcName, zoneShortName, lines, ref exceptionLines, out variableLines, out functionBlocks);

            // Extract quests out of the function blocks
            foreach (FunctionBlock functionBlock in functionBlocks)
                if (functionBlock.HasPossibleQuestData() == true)
                    quests.AddRange(functionBlock.ExtractQuests(ref exceptionLines, variableLines));                
        }
    }
}
