using Microservices.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microservices.Api.Handlers
{
    public class UserCreatedHandler : IEventHandler<UserCreated>
    {
        public Task HandleAsync(UserCreated @event)
        {
            throw new NotImplementedException();
        }
    }
}
