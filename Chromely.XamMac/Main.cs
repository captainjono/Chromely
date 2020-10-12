using System;
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
    public class NSAppWork : Chromely.Native.MacCocoaHost
    {
        protected override void RunCallback()
        {
            CFRunLoop.Main.Run();
        }
    }

    static class MainClass
    {
        static void Main(string[] args)
        {
            NSTimer timer = null;
            var tt = new TaskCompletionSource<bool>();

            MacCocoaHost.onRun = async () => {

                NSApplication.Init();
                                                    

                timer = NSTimer.CreateRepeatingTimer(0.003, _ =>
                {
                    DispatchQueue.MainQueue.DispatchAsync(() => CefRuntime.DoMessageLoopWork());
                });

                NSRunLoop.Main.AddTimer(timer, NSRunLoopMode.Default);

                var done = false;
                do
                {
                   // Start the run loop but return after each source is handled.
                    var result = CFRunLoop.Main.RunInMode(CFRunLoop.ModeDefault, 100, true);// CFRunLoopRunInMode(kCFRunLoopDefaultMode, 10, YES);
                    
                    // If a source explicitly stopped the run loop, or if there are no
                    // sources or timers, go ahead and exit.
                    if ((result == CFRunLoopExitReason.Finished))
                    {
                        done = true;
                    }
                }
                while (!done);
                Debug.WriteLine("Exiting loop");

            };

            MacCocoaHost.onRun = null;

            CefGlueBrowserProcessHandler.OnShcuedlerWork = (delayMs) =>
            {
                DispatchQueue.MainQueue.DispatchAfter(new DispatchTime((ulong) delayMs * 100000), () =>
                {
                    CefRuntime.DoMessageLoopWork();
                });
            };

            
            //Observabl.In(TimeSpan.FromSeconds(1 / 60)).Do(CefRuntime.DoMessageLoopWork).Subscribe();


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

                mainWin.Close();

                CefRuntime.PostTask(CefThreadId.UI, new HostBase.ActionTask(() =>
                {
                    //compare with cefsimple to see how there shutdown works?
                    //its saying that ceflifespanhandler onbeforeclosed is being called after destruction of the
                    //window? why? i just commneted out the handler in .g.cs to see if it had an effect - it didnt!
                    //i just commented out
                    //https://www.magpcss.org/ceforum/viewtopic.php?f=6&t=11441#p20028
                    //close window
                    //release all browser resources
                    //quit loop
                    //shutdown
                    //release app resources and exit
                    
                    Debug.WriteLine("Quitmessageloop delayed");
                    CefRuntime.QuitMessageLoop();

                }), 2000);
            };

            Chromely.Windows.Window._onCreated = cWindow =>
            {

                ///mainWin = ObjCRuntime.Runtime.GetNSObject<NSWindow>(cWindow.HostHandle, false);
                //allow xam-mac to run
                //need to NSApplication.Init(); here, not before CEF has loaded
                //Use CEF's UI scheduler to update UI
                CefRuntime.PostTask(CefThreadId.UI, new HostBase.ActionTask(() =>
                {
                    try
                    {

                        NSApplication.Init();
                        mainWin = ObjCRuntime.Runtime.GetNSObject<NSWindow>(cWindow.HostHandle, false);

                        Debug.WriteLine("Disposing of Chromely Window");
                        cWindow.Dispose();
                        Debug.WriteLine("Disposed of Chromely Window");

                        CefRuntime.PostTask(CefThreadId.UI, new HostBase.ActionTask(() =>
                        {
                            //Debug.WriteLine("QuiteMessageLoop being called");
                            //CefRuntime.QuitMessageLoop();
                        }), 200);

                    }
                    catch(Exception e)
                    {
                        Debug.Write("ERROR" + e);
                    }
                }), 1000 * 8);
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
