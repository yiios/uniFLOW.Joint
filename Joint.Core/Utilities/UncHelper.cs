﻿using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNC = UNCAccessWithCredentials.UNCAccessWithCredentials;
using Microsoft.Extensions.Logging;
using UniFlowGW.Services;

namespace Core.Utilities
{
    public class UncHelper
    {
        UNC unc;
        public string[] TargetPaths { get; } = { };

        public UncHelper(SettingService settings,
            ILogger<UncHelper> logger)
        {
            logger.LogInformation("[UncHelper] NetUseWithCredentials");
            var targetPaths = settings["UniflowService:TaskTargetPath"];
            TargetPaths = targetPaths.Split(';');
            //return;
            foreach (var targetPath in TargetPaths)
            {
                if (targetPath.StartsWith(@"\\"))
                {
                    logger.LogInformation("Unc initialize: " + targetPath);
                    unc = new UNC();
                    var user = settings["UniflowService:UncUser"];
                    var domain = settings["UniflowService:UncDomain"];
                    var pwd = settings["UniflowService:UncPassword"];
                    var result = unc.NetUseWithCredentials(targetPath, user, domain, pwd);
                    logger.LogInformation("[UncHelper] NetUseWithCredentials Result:" + result);
                }
            }
        }
    }
}
