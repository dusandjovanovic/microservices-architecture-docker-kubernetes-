﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.Common.Mongo
{
    public class MongoOptions
    {
        public string ConnectionString { get; set; }

        public string Database { get; set; }

        public string Seed { get; set; }
    }
}
