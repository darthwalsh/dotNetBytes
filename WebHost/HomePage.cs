﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;

namespace WebHost
{
    public class HomePage : NancyModule
    {
        public HomePage()
        {
            Get["/"] = _ => Response.AsFile("Content/index.html", "text/html");

            Post["/submit.html"] = _ => "submitted";
        }
    }
}