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

using System.Text;

namespace EQWOWPregenScripts
{
    internal class FileTool
    {
        public static string ReadAllDataFromFile(string fileName)
        {
            string returnString = string.Empty;
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(fs, bufferSize: 102400)) // Set a 100 KB buffer
                returnString = reader.ReadToEnd();
            return returnString;
        }

        public static List<string> ReadAllStringLinesFromFile(string fileName, bool stripHeader, bool removeBlankRows)
        {
            // Load in item data
            string inputData = FileTool.ReadAllDataFromFile(fileName);
            List<string> inputRows = new List<string>(inputData.Split(Environment.NewLine));
            if (stripHeader == true)
            {
                if (inputRows.Count == 0)
                    return new List<string>();
                else
                    inputRows.RemoveAt(0);
            }
            if (removeBlankRows == true)
            {
                for (int i = inputRows.Count - 1; i >= 0; i--)
                {
                    if (inputRows[i].Trim().Length == 0)
                        inputRows.RemoveAt(i);
                }
            }
            return inputRows;
        }

        public static List<Dictionary<string, string>> ReadAllRowsFromFileWithHeader(string fileName, string delimeter)
        {
            // Get the rows
            List<Dictionary<string, string>> returnRows = new List<Dictionary<string, string>>();
            List<string> rows = ReadAllStringLinesFromFile(fileName, false, true);

            // For each row, create a blocked return set
            bool isHeader = true;
            List<string> columnNames = new List<string>();
            foreach (string row in rows)
            {
                string[] rowBlocks = row.Split(delimeter);
                if (isHeader == true)
                {
                    foreach (string block in rowBlocks)
                        columnNames.Add(block);
                    isHeader = false;
                }
                else if (rowBlocks.Length == 1)
                    continue;
                else
                {
                    Dictionary<string, string> rowValues = new Dictionary<string, string>();
                    for (int i = 0; i < columnNames.Count; i++)
                        rowValues.Add(columnNames[i], rowBlocks[i]);
                    returnRows.Add(rowValues);
                }
            }

            return returnRows;
        }

        public static void WriteFile(string fileName, List<Dictionary<string, string>> outputColumnRows)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            List<string> outputLines = new List<string>();

            // Header
            StringBuilder headerSB = new StringBuilder();
            for (int i = 0; i < outputColumnRows[0].Keys.Count; i++)
            {
                string columnName = outputColumnRows[0].Keys.ToList()[i];
                headerSB.Append(columnName);
                if (i < outputColumnRows.Count - 1)
                    headerSB.Append("|");
            }
            outputLines.Add(headerSB.ToString());

            // Body
            foreach(Dictionary<string, string> row in outputColumnRows)
            {
                StringBuilder bodySB = new StringBuilder();
                for (int j = 0; j < row.Values.Count; j++)
                {
                    string value = row.Values.ToList()[j];
                    bodySB.Append(value);
                    if (j <  outputColumnRows.Count - 1)
                        bodySB.Append("|");
                }
                outputLines.Add(bodySB.ToString());
            }

            // output the file
            using (var outputFile = new StreamWriter(fileName))
                foreach (string outputLine in outputLines)
                    outputFile.WriteLine(outputLine);
        }
    }
}
