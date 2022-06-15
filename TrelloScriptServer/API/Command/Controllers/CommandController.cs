using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TrelloScriptServer.API.Command.Model;
using TrelloScriptServer.API.Command.Validator;
using TrelloScriptServer.Interpreter;

namespace TrelloScriptServer.API.Command.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommandController : ControllerBase
    {
        private readonly ILogger<CommandController> _logger;

        public CommandController(ILogger<CommandController> logger)
        {
            _logger = logger;
            CommandValidator.Init("Config/CommandAPIConfig.json");
        }

        [HttpGet("run/{command}", Name = "RunCommand")]
        public CommandResult Get(string command, string? token, string? parameters)
        {
            if(token != null && CommandValidator.Validate(token))
            {
                string fullcommand = command;
                if(parameters != null) { fullcommand = command + " " + parameters; }
                return ScriptRunnerProgram.RunCommand(fullcommand);
            }
            return CommandResult.Failure("Invalid token");
        }
    }
}
