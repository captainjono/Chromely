// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CefGlueBrowserProcessHandler.cs" company="Chromely Projects">
//   Copyright (c) 2017-2019 Chromely Projects
// </copyright>
// <license>
//      See the LICENSE.md file in the project root for more information.
// </license>
// ----------------------------------------------------------------------------------------------------------------------

using System;
    using System.Runtime.CompilerServices;
using Chromely.Core.Configuration;
using Xilium.CefGlue;

namespace Chromely.CefGlue.Browser.Handlers
{
    /// <summary>
    /// The cef glue browser process handler.
    /// </summary>
    public class CefGlueBrowserProcessHandler : CefBrowserProcessHandler
    {
        protected readonly IChromelyConfiguration _config;

        public CefGlueBrowserProcessHandler(IChromelyConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// The on before child process launch.
        /// </summary>
        /// <param name="browser_cmd">
        /// The command line.
        /// </param>
        protected override void OnBeforeChildProcessLaunch(CefCommandLine browser_cmd)
        {
            try
            {
                // Disable security features
                browser_cmd.AppendSwitch("default-encoding", "utf-8");
                browser_cmd.AppendSwitch("allow-file-access-from-files");
                browser_cmd.AppendSwitch("allow-universal-access-from-files");
                browser_cmd.AppendSwitch("disable-web-security");
                browser_cmd.AppendSwitch("ignore-certificate-errors");

         //       browser_cmd.AppendSwitch("disable-gpu");

                browser_cmd.AppendSwitch("disable-frame-rate-limit");
                browser_cmd.AppendSwitch("disable-gpu-vsync");
                browser_cmd.AppendSwitch("devtools-protocol-log-file", "cefdevtools.log");
                browser_cmd.AppendSwitch("proxy-auto-detect");


                if (!String.IsNullOrWhiteSpace(_config.SubProcessPath))
                {
                    if (!browser_cmd.HasSwitch("cefglue"))
                    {
                        browser_cmd.SetProgram(_config.SubProcessPath);

                        browser_cmd.AppendSwitch("cefglue", "w");
                    }
                }

                if (_config.DebuggingMode)
                {
                    Console.WriteLine("On CefGlue child process launch arguments:");
                    Console.WriteLine(browser_cmd.ToString());
                }
            }
            catch (Exception e)
            {
                
            }
        }

        protected override void OnScheduleMessagePumpWork(long delayMs)
        {
            OnShcuedlerWork.Invoke(delayMs);
        }

        public static Action<long> OnShcuedlerWork;

    }


}
