// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CefGlueLifeSpanHandler.cs" company="Chromely Projects">
//   Copyright (c) 2017-2019 Chromely Projects
// </copyright>
// <license>
//      See the LICENSE.md file in the project root for more information.
// </license>
// ----------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using Chromely.CefGlue.Browser.EventParams;
using Chromely.CefGlue.BrowserWindow;
using Chromely.Core.Configuration;
using Chromely.Core.Infrastructure;
using Chromely.Core.Network;
using Xilium.CefGlue;

namespace Chromely.CefGlue.Browser.Handlers
{
    /// <summary>
    /// The CefGlue life span handler.
    /// </summary>
    public class CefGlueLifeSpanHandler : CefLifeSpanHandler
    {
        protected readonly IChromelyConfiguration _config;
        protected readonly IChromelyCommandTaskRunner _commandTaskRunner;
        protected CefGlueBrowser _browser;

        public CefGlueLifeSpanHandler(IChromelyConfiguration config, IChromelyCommandTaskRunner commandTaskRunner, CefGlueBrowser browser)
        {
            _config = config;
            _commandTaskRunner = commandTaskRunner;
            _browser = browser;
        }

        public CefGlueBrowser Browser
        {
            get { return _browser; }
            set { _browser = value; }
        }

        protected override void OnAfterCreated(CefBrowser browser)
        {
            base.OnAfterCreated(browser);
            _browser.InvokeAsyncIfPossible(() =>
            {
                "CefGlueLifeSpanHandler async AfterCreated".LogDebug();   

                _browser.OnBrowserAfterCreated(browser);
            });
        }

        protected override bool DoClose(CefBrowser browser)
        {
            if (CefRuntime.Platform == CefRuntimePlatform.MacOS)
            {
                "Mac shutdown detcted, quitmessageloop called in 500ms".LogDebug();
                CefRuntime.PostTask(CefThreadId.UI, new HostBase.ActionTask(() =>
                {
                    CefRuntime.QuitMessageLoop();
                }), 500); //give cef enough time to catch up 

                this.Dispose(true); // <-- so i added this code to forefully destroy it even though
                //this is probably notentirely correct - the doco says after this function returns
                // the window should be removed

                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void OnBeforeClose(CefBrowser browser)
        {
            "CefGlueLifeSpanHandler OnBeforeClose".LogDebug();
        }

        protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
        {
            _browser.InvokeAsyncIfPossible(() => _browser.OnBeforePopup(new BeforePopupEventArgs(frame, targetUrl, targetFrameName)));

            var isUrlExternal = _config?.UrlSchemes?.IsUrlRegisteredExternalScheme(targetUrl);
            if (isUrlExternal.HasValue && isUrlExternal.Value)
            {
                RegisteredExternalUrl.Launch(targetUrl);
                return true;
            }

            var isUrlCommand = _config?.UrlSchemes?.IsUrlRegisteredCommandScheme(targetUrl);
            if (isUrlCommand.HasValue && isUrlCommand.Value)
            {
                _commandTaskRunner.RunAsync(targetUrl);
                return true;
            }

            return false;
        }
    }
}
