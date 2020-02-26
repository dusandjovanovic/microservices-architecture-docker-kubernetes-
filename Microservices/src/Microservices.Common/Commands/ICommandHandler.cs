﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microservices.Common.Commands
{
    public interface ICommandHandler<in T> where T: ICommand
    {
        Task HandleAsync(T command);
    }
}
