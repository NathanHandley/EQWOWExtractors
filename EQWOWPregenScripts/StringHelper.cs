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

using System.Text.RegularExpressions;

namespace EQWOWPregenScripts
{
    internal class StringHelper
    {
        public static string ExtractCurlyBraceContentRaw(List<string> rows, int startRowIndex, out int numOfRowsRead)
        {
            numOfRowsRead = 0;
            if (rows == null || !rows.Any() || startRowIndex < 0 || startRowIndex >= rows.Count)
                return string.Empty;

            // Join rows starting from startRowIndex
            string inputString = string.Join(" ", rows.Skip(startRowIndex));
            int[] lineEndPositions = new int[rows.Count - startRowIndex + 1];
            int currentPos = 0;
            lineEndPositions[0] = -1; // Start of first relevant line

            // Calculate end positions of each line in the joined string
            for (int j = startRowIndex; j < rows.Count; j++)
            {
                currentPos += rows[j].Length + (j < rows.Count - 1 ? 1 : 0); // Add space except for last row
                lineEndPositions[j - startRowIndex + 1] = currentPos;
            }

            // Find the first opening brace
            int startIndex = inputString.IndexOf('{');
            if (startIndex == -1)
                return string.Empty; // No opening brace found

            // Initialize brace depth and move past the opening brace
            int braceDepth = 1;
            int i = startIndex + 1;

            // Find the matching closing brace
            while (i < inputString.Length && braceDepth > 0)
            {
                char c = inputString[i];
                if (c == '{')
                    braceDepth++;
                else if (c == '}')
                    braceDepth--;
                i++;
            }

            // Check if we found a matching closing brace
            if (braceDepth != 0 || i > inputString.Length)
                return string.Empty; // Unmatched or malformed braces

            // Determine the number of rows read based on the closing brace position
            int closingBracePos = i - 1; // Position of the '}'
            for (int j = 1; j < lineEndPositions.Length; j++)
            {
                if (closingBracePos <= lineEndPositions[j])
                {
                    numOfRowsRead = j; // Include the line with the closing brace
                    break;
                }
            }

            // Extract and trim the content between the braces
            string content = inputString.Substring(startIndex + 1, i - startIndex - 2).Trim();
            return content;
        }

        public static List<(string key, string value)> ParseKeyValuePairs(string content)
        {
            List<(string key, string value)> pairs = new List<(string key, string value)>();
            int braceDepth = 0;
            int parenthesisDepth = 0;
            int startIndex = 0;
            string? currentKey = null;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (c == '{')
                    braceDepth++;
                else if (c == '}')
                    braceDepth--;
                else if (c == '(')
                    parenthesisDepth++;
                else if (c == ')')
                    parenthesisDepth--;
                else if (c == '=' && braceDepth == 0 && parenthesisDepth == 0 && currentKey == null)
                {
                    // Found key
                    currentKey = content.Substring(startIndex, i - startIndex).Trim();
                    startIndex = i + 1;
                }
                else if (c == ',' && braceDepth == 0 && parenthesisDepth == 0 && currentKey != null)
                {
                    // Found end of value
                    string value = content.Substring(startIndex, i - startIndex).Trim();
                    pairs.Add((currentKey, value));
                    currentKey = null;
                    startIndex = i + 1;
                }
            }

            // Add the last pair
            if (currentKey != null && startIndex < content.Length)
            {
                string value = content.Substring(startIndex).Trim();
                pairs.Add((currentKey, value));
            }

            return pairs;
        }

        static public List<string> ExtractCurlyBraceParameters(string inputString)
        {
            List<string> results = new List<string>();

            // Find all top-level curly brace blocks
            int i = 0;
            while (i < inputString.Length)
            {
                if (inputString[i] == '{')
                {
                    int braceDepth = 1;
                    int startIndex = i + 1; // Start after '{'
                    i++;

                    // Find the matching closing brace
                    while (i < inputString.Length && braceDepth > 0)
                    {
                        if (inputString[i] == '{')
                            braceDepth++;
                        else if (inputString[i] == '}')
                            braceDepth--;
                        i++;
                    }

                    if (braceDepth == 0 && i <= inputString.Length)
                    {
                        // Extract the content inside the braces
                        string innerContent = inputString.Substring(startIndex, i - startIndex - 1).Trim();
                        if (!string.IsNullOrEmpty(innerContent))
                        {
                            // Step 2: Parse key-value pairs
                            List<(string key, string value)> keyValuePairs = ParseKeyValuePairs(innerContent);

                            // Step 3: Format results
                            foreach (var pair in keyValuePairs)
                            {
                                results.Add($"{pair.key}, {pair.value}");
                            }
                        }
                    }
                }
                else
                {
                    i++;
                }
            }

            return results;
        }

        static public bool StringHasTwoFragments(string inputString, string fragment)
        {
            if (string.IsNullOrEmpty(inputString) || string.IsNullOrEmpty(fragment))
                return false;

            int firstIndex = inputString.IndexOf(fragment, StringComparison.OrdinalIgnoreCase);
            if (firstIndex == -1)
                return false;

            // Search for the second occurrence after the first
            int secondIndex = inputString.IndexOf(fragment, firstIndex + fragment.Length, StringComparison.OrdinalIgnoreCase);
            return secondIndex != -1;
        }

        static public string ConvertText(string inputTextLine)
        {
            string workingText = inputTextLine;
            workingText = workingText.Replace(".. e.other:GetCleanName() .. \"", "$N");
            workingText = workingText.Replace(" ..e.other:GetName()..", "$N");
            workingText = workingText.Replace("..e.other:GetName()..", "$N");
            workingText = workingText.Replace(" .. e.other:Class() .. ", "$C");
            workingText = workingText.Replace(" .. e.other:Race() .. ", "$R");
            workingText = workingText.Replace(";", "");
            workingText = workingText.Replace("\"", "");
            workingText = workingText.Replace("[", "");
            workingText = workingText.Replace("]", "");
            if (workingText.Contains("e.self:Say(string.format("))
            {
                workingText = workingText.Replace("e.self:Say(string.format(", "");
                if (workingText.Contains(",e.other:GetName()"))
                {
                    workingText = workingText.Replace(",e.other:GetName()", "");
                    workingText = workingText.Replace("%s", "$N");
                }
                else if (workingText.Contains(",e.other:Race()"))
                {
                    workingText = workingText.Replace(",e.other:Race()", "");
                    workingText = workingText.Replace("%s", "$R");
                }
                else if (workingText.Contains(",e.other:GetCleanName()"))
                {
                    workingText = workingText.Replace(",e.other:GetCleanName()", "");
                    workingText = workingText.Replace("%s", "$N");
                }
                else if (workingText.Contains(", e.other:GetCleanName()"))
                {
                    workingText = workingText.Replace(", e.other:GetCleanName()", "");
                    workingText = workingText.Replace("%s", "$N");
                }
            }
            else if (workingText.Contains("e.self:Say('"))
            {
                workingText = workingText.Replace("e.self:Say('", "");
                workingText = workingText.Replace("'", "");
            }
            if (workingText.EndsWith(")"))
                workingText = workingText.Substring(0, workingText.Length - 1);
            return workingText;
        }

        static public string GetAddedMathPart(string text)
        {
            int minusIndex = text.IndexOf('-');
            int plusIndex = text.IndexOf('+');

            // Find the earliest occurrence of either symbol
            int symbolIndex = -1;
            if (minusIndex != -1 && plusIndex != -1)
                symbolIndex = Math.Min(minusIndex, plusIndex);
            else if (minusIndex != -1)
                symbolIndex = minusIndex;
            else if (plusIndex != -1)
                symbolIndex = plusIndex;

            // If a symbol was found, return it and everything after
            if (symbolIndex != -1)
                return text.Substring(symbolIndex).TrimStart().Replace(" ", "");
            return string.Empty;
        }

        static public List<string> ExtractMethodParameters(string inputLine, string methodName)
        {
            // Find the method call
            string escapedMethodName = Regex.Escape(methodName);
            string pattern = $@"{escapedMethodName}\(";
            Match methodMatch = Regex.Match(inputLine, pattern);
            if (!methodMatch.Success)
                return new List<string>();

            // Parse the parameters up to the matching closing parenthesis
            int startIndex = methodMatch.Index + methodMatch.Length;
            int parenthesisDepth = 1;
            int braceDepth = 0;
            int i = startIndex;

            // Find the end of the parameter list
            while (i < inputLine.Length && parenthesisDepth > 0)
            {
                char c = inputLine[i];
                if (c == '(')
                    parenthesisDepth++;
                else if (c == ')')
                    parenthesisDepth--;
                else if (c == '{')
                    braceDepth++;
                else if (c == '}')
                    braceDepth--;
                i++;
            }

            // Unmatched parenthesis or braces
            if (parenthesisDepth != 0 || braceDepth != 0)
                return new List<string>();

            // Extract the parameter string
            string parameters = inputLine.Substring(startIndex, i - startIndex - 1).Trim();

            // Split parameters, respecting nested parentheses, braces, and quoted strings
            List<string> paramList = new List<string>();
            parenthesisDepth = 0;
            braceDepth = 0;
            int paramStart = 0;
            bool inSingleQuote = false;
            bool inDoubleQuote = false;

            for (int j = 0; j < parameters.Length; j++)
            {
                char c = parameters[j];

                // Handle quote states
                if (c == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                }
                else if (c == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                }
                // Handle escaped quotes
                else if ((c == '\\' && j + 1 < parameters.Length) &&
                         (parameters[j + 1] == '\'' || parameters[j + 1] == '"'))
                {
                    j++; // Skip the escaped character
                    continue;
                }
                // Track nesting
                else if (!inSingleQuote && !inDoubleQuote)
                {
                    if (c == '(')
                        parenthesisDepth++;
                    else if (c == ')')
                        parenthesisDepth--;
                    else if (c == '{')
                        braceDepth++;
                    else if (c == '}')
                        braceDepth--;
                    else if (c == ',' && parenthesisDepth == 0 && braceDepth == 0)
                    {
                        // Found a parameter separator at the top level
                        string param = parameters.Substring(paramStart, j - paramStart).Trim();
                        if (!string.IsNullOrEmpty(param))
                            paramList.Add(param);
                        paramStart = j + 1;
                    }
                }
            }

            // Add the last parameter
            if (paramStart < parameters.Length)
            {
                string param = parameters.Substring(paramStart).Trim();
                if (!string.IsNullOrEmpty(param))
                    paramList.Add(param);
            }

            return paramList;
        }

        static public int GetSingleRangedIntFromString(string inputString, string zoneShortName, string questgiverName, ref List<ExceptionLine> exceptionLines)
        {
            // Try to just pull it first
            int parsedValue;
            bool isValid = int.TryParse(inputString, out parsedValue);
            if (isValid)
                return parsedValue;

            // Try to identify the format
            if (inputString.Contains("math.random"))
            {
                List<string> parameters = ExtractMethodParameters(inputString, "random");
                if (parameters.Count == 0)
                {
                    exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Int in string had a random method, but no parameters could be found", 0, inputString));
                    return 0;
                }

                int parameter1Value = int.Parse(parameters[0]);
                if (parameters.Count == 1)
                {
                    // Use the midpoint between number and zero
                    if (parameter1Value == 0)
                        return 0;
                    else
                        return parameter1Value / 2;
                }
                else if (parameters.Count == 2)
                {
                    // Use a midpoint between the two numbers
                    int parameter2Value = int.Parse(parameters[1]);
                    if (parameter2Value == 0)
                        return 0;
                    else
                        return parameter1Value + (parameter2Value - parameter1Value) / 2;
                }
                else
                {
                    exceptionLines.Add(new ExceptionLine(questgiverName, zoneShortName, "Int in string had a random method, but there were more than 2 parameters found", 0, inputString));
                    return 0;
                }
            }

            return 0;
        }

        static public (string key, string value) GetLocalbleDataFromLine(string line)
        {
            (string key, string value) pairVariable = new (string.Empty, string.Empty);
            if (line.Contains("=") == false)
                return pairVariable;

            string workingLine = line.Replace("local", "").Trim();
            pairVariable.key = workingLine.Split("=")[0].Trim();
            pairVariable.value = workingLine.Split("=")[1].Trim();
            return pairVariable;
        }

        public static string GetContentWithinOuterDelimeter(string text, string delimiter)
        {
            int firstQuote = text.IndexOf(delimiter);
            int lastQuote = text.LastIndexOf(delimiter);

            // Check if we found both quotes and they're not the same position
            if (firstQuote != -1 && lastQuote != -1 && firstQuote != lastQuote)
            {
                // Extract substring starting after first quote to include everything up to last quote
                return text.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
            }
            return string.Empty;
        }
    }
}
