using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microservices.Common.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RawRabbit;

namespace Microservices.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/users")]
    public class UsersController : Controller
    {
        private readonly IBusClient _busClient;

        public UsersController(IBusClient busClient)
        {
            _busClient = busClient;
        }

        [HttpPost("")]
        public async Task<IActionResult> Post([FromBody]CreateUser command)
        {
            await _busClient.PublishAsync(command);

            return Accepted();
        }
    }
}