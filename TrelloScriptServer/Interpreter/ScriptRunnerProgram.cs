using TrelloScriptServer.API.Command.Model;
using TrelloScriptServer.API.Trello;

namespace TrelloScriptServer.Interpreter
{
    public class ScriptRunnerProgram
    {
        TrelloAPI api;
        TrelloInterpreter interpreter;
        int verbosity;
        const string appVersion = "v0.1.2";

        //Instance
        static ScriptRunnerProgram instance;
        static SlackBot bot;

        private ScriptRunnerProgram()
        {
            Logger.setPrintToCout(false);
            api = new TrelloAPI("Config/TrelloAPIConfig.json");
            SlackBot.Init("Config/SlackBotConfig.json", "C03KA5KDEFR");// "#testing_bot");
            interpreter = new TrelloInterpreter(api);
            interpreter.StartUpdateThread();
            Console.WriteLine("Interpreter " + appVersion);
            Console.WriteLine("Interpreter started: Update cycle is running in the background. Commands enabled");
            verbosity = 3;
        }

        public static void Init()
        {
            instance = new ScriptRunnerProgram();
        }

        public static CommandResult RunCommand(string line)
        { 
            if(instance == null) { instance = new ScriptRunnerProgram(); }
            return instance._RunCommand(line);
        }

        private CommandResult _RunCommand(string line)
        {
            string[] split = line.Split(" ");
            string command = split[0];
            string[] parameters = null;
            if (split.Length > 1)
            {
                parameters = new string[split.Length - 1];
                for (int i = 1; i < split.Length; i++)
                {
                    parameters[i - 1] = split[i];
                }
            }
            if (command == "start")
            {
                interpreter.StartUpdateThread();
                return CommandResult.Success("Success");
            }
            else if (command == "stop")
            {
                interpreter.StopThread();
                return CommandResult.Success("Success");
            }
            else if (command == "status")
            {
                var res = CommandResult.Success("Success");
                res.Body = interpreter.isRunning ? "Running" : "Not Running";
                return res;
            }
            else if (command == "echo")
            {
                return CommandResult.Success("Hello");
            }
            else if (command == "set")
            {
                if (parameters != null && parameters.Length == 2)
                {
                    if (parameters[0] == "sleepTime")
                    {
                        int Value = Int16.Parse(parameters[1]);
                        if (Value >= 0)
                        {
                            interpreter.setSleep(Value);
                            return CommandResult.Success("Success");
                        }
                        else
                        {
                            return CommandResult.Failure("Invalid value: Must be positive or zero");
                        }
                    }
                    else if (parameters[0] == "listenVerbosity")
                    {
                        int Value = Int16.Parse(parameters[1]);
                        if (Value >= 0)
                        {
                            verbosity = Value;
                            return CommandResult.Success("Success");
                        }
                        else
                        {
                            return CommandResult.Failure("Invalid value: Must be positive or zero");
                        }
                    }
                    else if (parameters[0] == "slackUpdateInterval" && parameters.Length == 2)
                    {
                        var Values = parameters[1].Split(",");
                        if (Values.Length == 3)
                        {
                            List<int> ints = new List<int>();
                            foreach (var v in Values)
                            {
                                int Value = Int16.Parse(v);
                                if (Value >= 0)
                                {
                                    ints.Add(Value);
                                }
                                else
                                {
                                    return CommandResult.Failure("Invalid value: Must be positive or zero");
                                }
                            }
                            interpreter.setSlackUpdateInterval(new TimeSpan(ints[0], ints[1], ints[2]));
                            return CommandResult.Success("Success");
                        }
                        else
                        {
                            return CommandResult.Failure("Invalid value: Format must be Hours,Minutes,Seconds");
                        }
                    }
                    else
                    {
                        return CommandResult.Failure("Invalid command"
                        + "\nUsage: set [Name] [Value]"
                        + "\nPossible [Name] values: sleepTime, listenVerbosity, slackUpdateInterval");
                    }
                }
                else
                {
                    return CommandResult.Failure("Invalid command"
                    + "\nUsage: set [Name] [Value]"
                    + "\nPossible [Name] values: sleepTime, listenVerbosity, slackUpdateInterval");
                }
            }
            else if (command == "get")
            {
                if (parameters != null && parameters.Length == 1)
                {
                    if (parameters[0] == "sleepTime")
                    {
                        var ret = CommandResult.Success("Success");
                        ret.Body = interpreter.getSleep().ToString();
                        return ret;
                    }
                    else if (parameters[0] == "listenVerbosity")
                    {
                        var ret = CommandResult.Success("Success");
                        ret.Body = verbosity.ToString();
                        return ret;
                    }
                    else if (parameters[0] == "slackUpdateInterval")
                    {
                        var ret = CommandResult.Success("Success");
                        var time = interpreter.getSlackUpdateInterval();
                        ret.Body = time.Days * 24 + time.Hours + "," + time.Minutes + "," + time.Seconds;
                        return ret;
                    }
                    else
                    {
                        return CommandResult.Failure("Invalid command"
                        + "\nUsage: get [Name]"
                        + "\nPossible [Name] values: sleepTime, listenVerbosity, slackUpdateInterval");
                    }
                }
                else
                {
                    return CommandResult.Failure("Invalid command"
                    + "\nUsage: get [Name]"
                    + "\nPossible [Name] values: sleepTime, listenVerbosity, slackUpdateInterval");
                }
            }
            else if (command == "slack")
            {
                if (parameters != null && parameters.Length > 0)
                {
                    if (parameters[0] == "message" && parameters.Length >= 2)
                    {
                        string msg = parameters[1];
                        for (int i = 2; i < parameters.Length; i++)
                        {
                            msg += " " + parameters[i];
                        }
                        SlackBot.Message(msg);
                        return CommandResult.Success("Success");
                    }
                    else if (parameters[0] == "expiredCards" && parameters.Length == 1)
                    {
                        interpreter.UpdateSlackBot();
                        return CommandResult.Success("Success");
                    }
                    else
                    {
                        return CommandResult.Failure("Invalid command"
                            + "\nUsage: slack [Command] [Param]"
                            + "\nPossible [Command] values: message, expiredCards");
                    }
                }
                else
                {
                    return CommandResult.Failure("Invalid command"
                        + "\nUsage: slack [Command] [Param]"
                        + "\nPossible [Command] values: message, expiredCards");
                }
            }
            else if (command == "listen")
            {
                /*
                return ">Starting listen session. Press Enter to end session!");
                interpreter.setVerbosity(verbosity);
                Console.ReadLine();
                return ">Ending listen session.");
                interpreter.setVerbosity(0);
                return ">Finished");
                */
                return CommandResult.Failure("Command not avalaible");
            }
            else if (command == "help")
            {
                var res = CommandResult.Success("Success");
                res.Body = "start -> Starts the interpreter which runs and updates in the background"
                + "\nstop -> Stops the interpreter"
                + "\nstatus -> Tells you if the interpreter is running"
                + "\nset [Name] [Value] -> Sets the named variable to the given value"
                + "\nget [Name] -> Gets the named variable"
                + "\nslack [Command] [Param] -> Runs commands on the slack bot."
                + "\nlisten -> Starts a listening session, where the background update session, prints debug information";
                return res;
            }
            else
            {
                return CommandResult.Failure("Invalid command");
            }
        }
    }
}
