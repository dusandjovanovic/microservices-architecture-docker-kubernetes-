using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.Common.Commands
{
    public interface IAuthenticatedCommand : ICommand
    {
        Guid UserId { get; set; }
    }
}
