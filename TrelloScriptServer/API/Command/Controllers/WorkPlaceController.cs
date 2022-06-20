using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TrelloScriptServer.Services.Command;
using TrelloScriptServer.Services.WorkPlace;

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
            string[] split = null;
            if (parameters != null)
            {
                split = parameters.Split(' ');
            }
            return WorkPlaceService.RunCommand(workPlaceName, token, command, split);
        }
    }
}
