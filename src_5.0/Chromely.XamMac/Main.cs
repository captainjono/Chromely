﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Caliburn.Light;
using Chromely.CefGlue.Browser.Handlers;
using Chromely.CefGlue.BrowserWindow;
using Chromely.Core;
using Chromely.Core.Configuration;
using Chromely.Core.Host;
using Chromely.Core.Infrastructure;
using Chromely.Core.Logging;
using Chromely.Core.Network;
using Chromely.Native;
using Chromely.Windows;
using CoreFoundation;
using Foundation;
using Xilium.CefGlue;

namespace Chromely.XamMac
{
    static class MainClass
    {
        static void Main(string[] args)
        {
            var ended = false;
            new Thread(() =>
            {
                while (true)
                {
                    if (ended)
                        break;
                    
                    Thread.Sleep(200);
                    GC.Collect(2);
                }
            }).Start();

            var config = new DemoAppCfg()
            {
                DebuggingMode = true,
                //CachePath = "absotule path here",
                AppName = "XamarinMacCEF",
                StartUrl = "https://google.com",
                SubProcessPath = ChromelyRuntime.Platform == ChromelyPlatform.MacOSX ? Process.GetCurrentProcess().MainModule?.FileName : null,
                WindowOptions = new WindowOptions
                {
                    Title = "Xam.Mac CEF Demo",
                    Position = new WindowPosition(0, 0),
                    Size = new WindowSize(1000, 900),
                    //DisableResizing = true,
                    //StartCentered = true,
                    DisableMinMaximizeControls = true,
                    WindowState = WindowState.Normal,
                },
                Platform = ChromelyRuntime.Platform,
                AppExeLocation = AppDomain.CurrentDomain.BaseDirectory,
                CefDownloadOptions = new CefDownloadOptions(true, true),
                UrlSchemes = new List<UrlScheme>(new[]
                {
                    new UrlScheme("demo", "https", "cef.app", string.Empty, UrlSchemeType.Resource, true)

                }),
                CommandLineArgs = new Dictionary<string, string>()
                {
                    ["cefLogFile"] = "chromely.cef.log",
                    ["logSeverity"] = "verbose",
                    //["external-message-pump"] = true.ToString()
                },
                CommandLineOptions = new List<string>(new[] { "disable-web-security"})
            };
    

                NSWindow mainWin = null;

            CefGlueLifeSpanHandler._doClose = _ =>
            {
                //looks like terminating the application sends the right messages.
                //modify lobchromely to implement the cefshutdown on the swizzelled terminate?

                CefRuntime.PostTask(CefThreadId.UI, new HostBase.ActionTask(() =>
                {
                    //"cefglue doclose".LogDebug();
                    //mainWin.Close();
                    CefRuntime.QuitMessageLoop();
                }));

            };

            Chromely.Windows.Window._onCreated = cWindow =>
            {
                CefRuntime.PostTask(CefThreadId.UI, new HostBase.ActionTask(() =>
                {
                    "Init xam app".LogDebug();
                    NSApplication.Init();
                    mainWin = ObjCRuntime.Runtime.GetNSObject<NSWindow>(cWindow.HostHandle, false);
                    //mainWin = null;
                    //allow xam-mac to run
                    //need to NSApplication.Init(); here, not before CEF has loaded
                    //Use CEF's UI scheduler to update UI
                    CefRuntime.PostTask(CefThreadId.UI, new HostBase.ActionTask(() =>
                    {
                        try
                        {
                            "Disposing of Chromely Window".LogDebug();
                            cWindow.Dispose();
                            //  CefRuntime.QuitMessageLoop();
                            // Debug.WriteLine("Disposed of Chromely Window");
                        }
                        catch (Exception e)
                        {
                            "While running".LogError(e);
                        }
                    }), 1000 * 5);
                }));
            };

            var chromelyContainer = new SimpleContainer();
            chromelyContainer.RegisterByTypeSingleton(typeof(IChromelyWindow),  typeof(DemoWindow));

            AppBuilder.Create()
                .UseApp<DemoApp>()
                .UseConfiguration<DemoAppCfg>(config)
                .UseContainer<SimpleContainer>(chromelyContainer)
                .UseLogger<DebugLogger>()
                .Build()
                .Run(args);

            ended = true;
            "Exiting gracefully".LogDebug();
        }


        public class DemoApp : ChromelyApp
        {
            public override void RegisterEvents(IChromelyContainer container)
            {
                EnsureContainerValid(container);
            }

            public override IChromelyWindow CreateWindow()
            {
                "CreateWindow DemoApp".LogDebug();
                var win = (IChromelyWindow)Container.GetInstance(typeof(IChromelyWindow), typeof(IChromelyWindow).Name);
                
               // "CreateWindow returned".LogDebug();
                return win;
            }
        }

        public class DemoWindow : ChromelyWindow
        {
            public DemoWindow(IChromelyNativeHost nativeHost, IChromelyContainer container, IChromelyConfiguration config, IChromelyRequestTaskRunner requestTaskRunner, IChromelyCommandTaskRunner commandTaskRunner) : base(nativeHost, container, config, requestTaskRunner, commandTaskRunner)
            {
            }
        }

        public class DemoAppCfg : IChromelyConfiguration
        {
            public string AppName { get; set; }
            public string StartUrl { get; set; }
            public string AppExeLocation { get; set; }
            public string ChromelyVersion { get; set; }
            public ChromelyPlatform Platform { get; set; }
            public bool DebuggingMode { get; set; }
            public string DevToolsUrl { get; set; }
            public IDictionary<string, string> CommandLineArgs { get; set; }
            public List<string> CommandLineOptions { get; set; }
            public List<ControllerAssemblyInfo> ControllerAssemblies { get; set; }
            public IDictionary<string, string> CustomSettings { get; set; }
            public List<ChromelyEventHandler<object>> EventHandlers { get; set; }
            public IDictionary<string, object> ExtensionData { get; set; }
            public IChromelyJavaScriptExecutor JavaScriptExecutor { get; set; }
            public List<UrlScheme> UrlSchemes { get; set; }
            public CefDownloadOptions CefDownloadOptions { get; set; }
            public IWindowOptions WindowOptions { get; set; }
            public string CachePath { get; set; }
            public string SubProcessPath { get; set; }
        }

        public class DebugLogger : IChromelyLogger
        {
            public void WriteDebug(string state, string msg)
            {
                msg.LogDebug();
            }

            public void Info(string message)
            {
                message.LogInfo();
            }

            public void Verbose(string message)
            {
                message.LogDebug();

            }

            public void Debug(string message)
            {
                message.LogDebug();

            }

            public void Warn(string message)
            {
                message.LogDebug();
            }

            public void Critial(string message)
            {
                message.LogError();
            }

            public void Fatal(string message)
            {
                message.LogError();

            }

            public void Error(string message)
            {
                message.LogError();

            }

            public void Error(Exception exception)
            {
                "Error".LogError(exception);

            }

            public void Error(Exception exception, string message)
            {
                message.LogError(exception);
            }
        }
    }
}
