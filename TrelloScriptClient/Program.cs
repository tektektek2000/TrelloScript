// See https://aka.ms/new-console-template for more information
using TrelloScriptClient.API;

const string appVersion = "v0.1.0";

CommandAPI commandAPI = new CommandAPI("Config/CommandAPIConfig.json");
string line = "";
Console.WriteLine("TrelloScriptClient " + appVersion);
while(line != "quit")
{
    Console.Write(">");
    line = Console.ReadLine();
    var split = line.Split();
    string command = split[0];
    string parameters = "";
    if(split.Length > 1)
    {
        parameters = split[1];
        for (int i = 2; i < split.Length; i++)
        {
            parameters += " " + split[i];
        }
    }
    if (command.Length > 0 && command != "quit")
    {
        var result = commandAPI.runCommand(command, parameters);
        if (result.Body != null)
        {
            Console.WriteLine(result.Body);
        }
        if (result.Warnings != null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var warning in result.Warnings)
            {
                Console.WriteLine("Warning: " + warning);
            }
        }
        if (result.Successfull)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("OK: ");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Fail: ");
        }
        if (result.Message != null)
        {
            Console.WriteLine(result.Message);
        }
        Console.ForegroundColor = ConsoleColor.White;
    }
}
