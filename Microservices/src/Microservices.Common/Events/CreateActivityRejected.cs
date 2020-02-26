using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.Common.Events
{
    public class CreateActivityRejected
    {
        public Guid Id { get; }
        public string Reason { get; }
        public string Code { get; }

        protected CreateActivityRejected()
        {
        }

        public CreateActivityRejected(Guid id,
            string reason, string code)
        {
            Id = id;
            Reason = reason;
            Code = code;
        }
    }
}
