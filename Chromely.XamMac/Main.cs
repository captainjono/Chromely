using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Caliburn.Light;
using Chromely.Core;
using Chromely.Core.Configuration;
using Chromely.Core.Host;
using Chromely.Core.Infrastructure;
using Chromely.Core.Logging;
using Chromely.Core.Network;
using Chromely.Windows;

namespace Chromely.XamMac
{
    static class MainClass
    {
        static void Main(string[] args)
        {
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
                    ["logSeverity"] = "verbose"
                },
                CommandLineOptions = new List<string>(new[] { "disable-web-security" })
            };

            Chromely.Windows.Window._onCreated = cWindow =>
            {
                //the window handle
                //var xamMacWindow = FromNativeHandle(cWindow.HostHandle);
            };

            var chromelyContainer = new SimpleContainer();
            chromelyContainer.RegisterByTypeSingleton(typeof(IChromelyWindow), typeof(DemoWindow));

            AppBuilder.Create()
                .UseApp<DemoApp>()
                .UseConfiguration<DemoAppCfg>(config)
                .UseContainer<SimpleContainer>(chromelyContainer)
                .UseLogger<DebugLogger>()
                .Build()
                .Run(args);
        }

        public NSWindow FromNativeHandle(IWindow nativeHost)
        {
            Debug.WriteLine("Attempting to lookup Chromly native window");

            var nativeHandle = nativeHost.HostHandle;
            var mainWin = _mainWindow = ObjCRuntime.Runtime.GetNSObject<NSWindow>(nativeHandle, false);

            $"Found Chromely Native Window @ {nativeHandle}".LogDebug("Replay");

            //CefRuntime.PostTask( 
            MainWindow.TitlebarAppearsTransparent = true;

                
            

            return mainWin;
        }


        public class DemoApp : ChromelyApp
        {
            public override void RegisterEvents(IChromelyContainer container)
            {
                EnsureContainerValid(container);
            }

            public override IChromelyWindow CreateWindow()
            {
                Debug.WriteLine("CreateWindow DemoApp");
                return (IChromelyWindow)Container.GetInstance(typeof(IChromelyWindow), typeof(IChromelyWindow).Name);
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
                System.Diagnostics.Debug.WriteLine($"[{state}] {msg}");
            }

            public void Info(string message)
            {
                WriteDebug("Info", message);
            }

            public void Verbose(string message)
            {
                WriteDebug("Info", message);
            }

            public void Debug(string message)
            {
                WriteDebug("Info", message);
            }

            public void Warn(string message)
            {
                WriteDebug("Warn", message);
            }

            public void Critial(string message)
            {
                WriteDebug("Error", message);
            }

            public void Fatal(string message)
            {
                WriteDebug("Error", message);
            }

            public void Error(string message)
            {
                WriteDebug("Error", message);

            }

            public void Error(Exception exception)
            {
                WriteDebug("Error", exception.ToString());

            }

            public void Error(Exception exception, string message)
            {
                WriteDebug("Error", message);

            }
        }
    }
}
