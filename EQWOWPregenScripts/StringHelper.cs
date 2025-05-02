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
    }
}
