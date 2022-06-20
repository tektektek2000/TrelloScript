using Newtonsoft.Json.Linq;
using TrelloScriptServer.API.Slack;
using TrelloScriptServer.API.Trello.API;
using TrelloScriptServer.Services.Command;
using TrelloScriptServer.Services.Slack;
using TrelloScriptServer.Services.Trello;

namespace TrelloScriptServer.Services.WorkPlace
{
    public class WorkPlaceService : Service
    {
        WorkPlaceConfig config;
        List<Service> services;
        CommandValidator validator;
        int verbosity;

        //Properties
        public string Name { get { return config.name; } }

        //static variables
        static string appVersion = "v0.3.0";
        static List<WorkPlaceService> instances = new List<WorkPlaceService>();

        private WorkPlaceService(WorkPlaceConfig workPlaceConfig)
        {
            config = workPlaceConfig;
            validator = new CommandValidator(config.commandConfig.token);
            services = new List<Service>();
            if (config.services != null) 
            {
                foreach (var serviceConfig in config.services)
                {
                    services.Add(serviceConfig.InstatitiateService());
                }
            }
            foreach(var service in services)
            {
                service.Init(this);
            }
        }

        public static CommandResult RunCommand(string workPlaceName, string token, string command, string[] parameters)
        {
            foreach (WorkPlaceService workPlace in instances)
            {
                if (workPlace.Name == workPlaceName)
                {
                    if (workPlace.validator.Validate(token))
                    {
                        return workPlace.RunCommand(command, parameters);
                    }
                    return CommandResult.Failure("Invalid token");
                }
            }
            return CommandResult.Failure("No workplace with this name");
        }

        public static void InitWorkPlaces(string jsonConfigPath)
        {
            Logger.setPrintToCout(false);
            Console.WriteLine("TrelloScriptServer " + appVersion);
            var json = JArray.Parse(File.ReadAllText(jsonConfigPath));
            foreach (var it in json)
            {
                WorkPlaceConfig workPlaceConfig = new WorkPlaceConfig();
                workPlaceConfig.name = it["name"].ToString();
                workPlaceConfig.commandConfig = it["commandConfig"].ToObject<CommandConfig>();
                workPlaceConfig.services = new List<ServiceConfig>();
                foreach (var it2 in it["services"])
                {
                    if (it2["type"].ToString() == "trello")
                    {
                        TrelloConfig trelloConfig = it2["config"].ToObject<TrelloConfig>();
                        workPlaceConfig.services.Add(trelloConfig);
                    }
                    else if (it2["type"].ToString() == "slack")
                    {
                        SlackConfig slackConfig = it2["config"].ToObject<SlackConfig>();
                        workPlaceConfig.services.Add(slackConfig);
                    }
                }
                WorkPlaceService newWorkPlace = new WorkPlaceService(workPlaceConfig);
                instances.Add(newWorkPlace);
            }
            foreach(var it in instances)
            {
                it.Init(it);
                it.Start();
            }
        }

        public override CommandResult Start()
        {
            CommandResult ret = CommandResult.Success("Success", new List<string>());
            foreach (var it in services)
            {
                var res = it.Start();
                if (!res.Successfull)
                {
                    ret.Warnings.Add(it.getServiceName() + " - Error: " + res.Message);
                }
                if(res.Warnings != null)
                {
                    foreach(var warning in res.Warnings)
                    {
                        ret.Warnings.Add(it.getServiceName() + " - Warning: " + warning);
                    }
                }
            }
            return ret;
        }

        public override CommandResult Stop()
        {
            CommandResult ret = CommandResult.Success("Success", new List<string>());
            foreach (var it in services)
            {
                var res = it.Stop();
                if (!res.Successfull)
                {
                    ret.Warnings.Add(it.getServiceName() + " - Error: " + res.Message);
                }
                if (res.Warnings != null)
                {
                    foreach (var warning in res.Warnings)
                    {
                        ret.Warnings.Add(it.getServiceName() + " - Warning: " + warning);
                    }
                }
            }
            return ret;
        }

        public override void Init(WorkPlaceService workPlaceService)
        {
            foreach(var service in services)
            {
                service.Init(workPlaceService);
            }
        }

        public override bool Status()
        {
            return true;
        }

        public override string getServiceName()
        {
            return config.name + " Work Place";
        }

        public override CommandResult RunCommand(string command, string[] parameters)
        {
            if (command == "start")
            {
                return Start(); ;
            }
            else if (command == "stop")
            {
                return Stop();
            }
            else if (command == "status")
            {
                var res = CommandResult.Success("Success");
                res.Body = "";
                foreach (var service in services)
                {
                    res.Body += service.getServiceName() + " - " + ( service.Status() ? "Running" : "Not Running" ) + "\n";
                }
                return res;
            }
            else if (command == "echo")
            {
                return CommandResult.Success("Hello");
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
                foreach (var service in services)
                {
                    if (command == service.getServiceName())
                    {
                        if (parameters != null && parameters.Length > 0)
                        {
                            string newCommand = parameters[0];
                            string[] newParameter = null;
                            if(parameters.Length > 1)
                            {
                                newParameter = new string[parameters.Length - 1];
                                for(int i = 1; i < parameters.Length; i++)
                                {
                                    newParameter[i - 1] = parameters[i];
                                }
                            }
                            return service.RunCommand(newCommand, newParameter);
                        }
                        else
                        {
                            var res = CommandResult.Success("Success");
                            res.Body = service.getServiceName() + " - " + (service.Status() ? "Running" : "Not Running");
                            return res;
                        }
                    } 
                }
                return CommandResult.Failure("Invalid command");
            }
        }

        public override List<CommandHelp> Help()
        {
            throw new NotImplementedException();
        }

        public T getService<T>() where T : Service
        {
            foreach (var it in services)
            {
                T service = it as T;
                if (service != null) { return service; }
            }
            return null;
        }
    }
}
