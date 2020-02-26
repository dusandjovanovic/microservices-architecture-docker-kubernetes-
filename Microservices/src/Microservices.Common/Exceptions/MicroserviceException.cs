using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.Common.Exceptions
{
    public class MicroserviceException : Exception
    {
        private string v1;
        private string v2;

        public string Code { get; }

        public MicroserviceException()
        {
        }

        public MicroserviceException(string code)
        {
            Code = code;
        }

        public MicroserviceException(string message, params object[] args) : this(string.Empty, message, args)
        {
        }

        public MicroserviceException(string code, string message, params object[] args) : this(null, code, message, args)
        {
        }

        public MicroserviceException(Exception innerException, string message, params object[] args)
            : this(innerException, string.Empty, message, args)
        {
        }

        public MicroserviceException(Exception innerException, string code, string message, params object[] args)
            : base(string.Format(message, args), innerException)
        {
            Code = code;
        }

        public MicroserviceException(string v1, string v2) : this(v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }
    }
}
