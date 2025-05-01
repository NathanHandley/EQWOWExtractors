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
    public class ExceptionLine
    {
        static private string OutputExceptionLinesFile = "E:\\ConverterData\\Quests\\ExceptionsNew.csv";

        public ExceptionLine(string questgiverName, string zoneShortName, string exceptionReason, int lineRow, string lineText)
        {
            QuestgiverName = questgiverName;
            ZoneShortName = zoneShortName;
            ExceptionReason = exceptionReason;
            LineRow = lineRow;
            LineText = lineText;
            DataToString(true);
        }
        private string ZoneShortName = string.Empty;
        private string QuestgiverName = string.Empty;
        private string ExceptionReason = string.Empty;
        private int LineRow;
        private string LineText = string.Empty;

        public string DataToString(bool forConsole = false)
        {
            StringBuilder sb = new StringBuilder();
            string delimeter = "|";
            if (forConsole == true)
                delimeter = ", ";
            sb.Append(QuestgiverName);
            sb.Append(delimeter);
            sb.Append(ZoneShortName);
            sb.Append(delimeter);
            sb.Append(ExceptionReason);
            sb.Append(delimeter);
            sb.Append(LineRow);
            sb.Append(delimeter);
            sb.Append(LineText);
            if (forConsole == true)
                Console.WriteLine(sb.ToString());
            return sb.ToString();
        }

        static public string HeaderToString()
        {
            return "Questgiver_Name|Zone_Shortname|Exception_Reason|LineRow|LineText";
        }

        static public void OutputExceptionLines(List<ExceptionLine> exceptionLines)
        {
            if (File.Exists(OutputExceptionLinesFile))
                File.Delete(OutputExceptionLinesFile);
            using (var outputFile = new StreamWriter(OutputExceptionLinesFile))
            {
                outputFile.WriteLine(ExceptionLine.HeaderToString());
                foreach (ExceptionLine exceptionLine in exceptionLines)
                    outputFile.WriteLine(exceptionLine.DataToString());
            }
        }
    }
}
