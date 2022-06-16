using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TrelloScriptServer.API.Command.Model;
using TrelloScriptServer.API.Command.Validator;
using TrelloScriptServer.Interpreter;

namespace TrelloScriptServer.API.Command.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkPlaceController : ControllerBase
    {
        private readonly ILogger<WorkPlaceController> _logger;

        public WorkPlaceController(ILogger<WorkPlaceController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{workPlaceName}/run/{command}", Name = "RunCommand")]
        public CommandResult Get(string workPlaceName, string command, string? token, string? parameters)
        {
            string fullcommand = command;
            if(parameters != null) { fullcommand = command + " " + parameters; }
            return WorkPlace.RunCommand(workPlaceName, token, fullcommand);
        }
    }
}
