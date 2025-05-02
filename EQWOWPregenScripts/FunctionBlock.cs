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

namespace EQWOWPregenScripts
{
    internal class FunctionBlock
    {
        public string functionName = string.Empty;
        public List<string> blockLines = new List<string>();

        public void LoadStartingAtLine(List<string> inputLines, int startingLineIndex, string npcName, string zoneShortName, ref List<ExceptionLine> exceptionLines,
            out int readLineCount)
        {
            readLineCount = 0;
            string startLine = inputLines[startingLineIndex].Split("--")[0].Trim();
            if (startingLineIndex >= inputLines.Count)
            {
                exceptionLines.Add(new ExceptionLine(npcName, zoneShortName, "Invalid startingLineIndex in FunctionBlock Load", startingLineIndex, string.Empty));
                return;
            }
            if (startLine.StartsWith("function") == false)
            {
                exceptionLines.Add(new ExceptionLine(npcName, zoneShortName, "Line is not a function line", startingLineIndex, inputLines[startingLineIndex]));
                return;
            }

            int scopeSteps = 0;
            for (int i = startingLineIndex; i < inputLines.Count; i++)
            {
                // Grab the line, removing comments and cap whitespaces
                string curLine = inputLines[i].Split("--")[0].Trim();
                blockLines.Add(curLine);
                readLineCount++;

                // Function line
                if (curLine.StartsWith("function "))
                {
                    functionName = curLine.Split("function ")[1];
                    scopeSteps++;
                    continue;
                }
                else if (curLine.StartsWith("if"))
                    scopeSteps++;
                else if (curLine.StartsWith("while"))
                    scopeSteps++;
                else if (curLine.StartsWith("for"))
                    scopeSteps++;

                if (curLine.StartsWith("end "))
                    scopeSteps--;
                else if (curLine.EndsWith("end"))
                    scopeSteps--;

                // When all scope is resolved, exit
                if (scopeSteps == 0)
                    return;
            }

            exceptionLines.Add(new ExceptionLine(npcName, zoneShortName, "Could not find an end line for function", startingLineIndex, inputLines[startingLineIndex]));
            return;
        }
    }
}
