// See https://aka.ms/new-console-template for more information

using EQWOWPregenScripts;

QuestExtractor questExtractor = new QuestExtractor();
questExtractor.ExtractQuests();

Console.WriteLine("Done. Press any key...");
Console.ReadKey();
