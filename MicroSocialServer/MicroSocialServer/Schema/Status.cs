﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace MicroSocialServer.Schema
{
    class Status
    {
        public User poster;
        public DateTime time;
        public string statusContent;

        //public int databaseId;

        public Status()
        {

        }
    }
}
