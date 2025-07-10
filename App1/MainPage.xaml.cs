using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace App1
{
    public sealed partial class MainPage : Page
    {
        private Gamepad? gamepad;
        private GamepadButtons lastButtons = GamepadButtons.None;

        public MainPage()
        {
            this.InitializeComponent();

            WebView2Control.CoreWebView2Initialized += WebView2Control_CoreWebView2Initialized;

            WebView2Control.Loaded += (s, e) =>
            {
                ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                WebView2Control.Focus(FocusState.Programmatic);
            };

            WebView2Control.KeyDown += WebView2Control_KeyDown;

            Gamepad.GamepadAdded += (sender, e) => gamepad = e;
            Windows.UI.Xaml.Media.CompositionTarget.Rendering += UpdateGamepadState;
        }

        private void WebView2Control_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            WebView2Control.NavigationCompleted += WebView2Control_NavigationCompleted;
        }

        private async void WebView2Control_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            string script = @"
        const targetWidth = 1920;
        const actualWidth = window.innerWidth;
        const actualHeight = window.innerHeight;

        const scale = actualWidth / targetWidth;

        document.body.style.transform = `scale(${scale})`;
        document.body.style.transformOrigin = 'top left';
        document.body.style.width = `${targetWidth}px`;
        document.body.style.height = `${actualHeight / scale}px`;
        document.body.style.overflow = 'auto';

        console.log('✅ Escalado y altura ajustada. scale:', scale, 'adjusted height:', actualHeight / scale);
    ";

            try
            {
                await WebView2Control.CoreWebView2.ExecuteScriptAsync(script);
                Debug.WriteLine("✅ Script ejecutado correctamente.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("❌ Error al inyectar script: " + ex.Message);
            }
        }


        private async void WebView2Control_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            int keyCode = GetKeyCode(e.Key);
            if (keyCode != 0 && WebView2Control.CoreWebView2 != null)
            {
                await SendKeyToWebViewAsync(keyCode);
                e.Handled = true;
            }
        }

        private async void UpdateGamepadState(object? sender, object? e)
        {
            if (gamepad == null || WebView2Control.CoreWebView2 == null) return;

            var reading = gamepad.GetCurrentReading();
            if (reading.Buttons != lastButtons)
            {
                if (reading.Buttons.HasFlag(GamepadButtons.DPadUp) && !lastButtons.HasFlag(GamepadButtons.DPadUp))
                    await SendKeyToWebViewAsync(38);
                if (reading.Buttons.HasFlag(GamepadButtons.DPadDown) && !lastButtons.HasFlag(GamepadButtons.DPadDown))
                    await SendKeyToWebViewAsync(40);
                if (reading.Buttons.HasFlag(GamepadButtons.DPadLeft) && !lastButtons.HasFlag(GamepadButtons.DPadLeft))
                    await SendKeyToWebViewAsync(37);
                if (reading.Buttons.HasFlag(GamepadButtons.DPadRight) && !lastButtons.HasFlag(GamepadButtons.DPadRight))
                    await SendKeyToWebViewAsync(39);
                if (reading.Buttons.HasFlag(GamepadButtons.A) && !lastButtons.HasFlag(GamepadButtons.A))
                    await SendKeyToWebViewAsync(13);
                if (reading.Buttons.HasFlag(GamepadButtons.X) && !lastButtons.HasFlag(GamepadButtons.X))
                    await SendKeyToWebViewAsync(88);
            }
            lastButtons = reading.Buttons;
        }

        private static int GetKeyCode(VirtualKey key)
        {
            return key switch
            {
                VirtualKey.Up => 38,
                VirtualKey.Down => 40,
                VirtualKey.Left => 37,
                VirtualKey.Right => 39,
                VirtualKey.Enter => 13,
                VirtualKey.X => 88,
                _ => 0
            };
        }

        private static string KeyCodeToKey(int keyCode)
        {
            return keyCode switch
            {
                37 => "ArrowLeft",
                38 => "ArrowUp",
                39 => "ArrowRight",
                40 => "ArrowDown",
                13 => "Enter",
                88 => "x",
                _ => ""
            };
        }

        private static string KeyCodeToCode(int keyCode)
        {
            return keyCode switch
            {
                37 => "ArrowLeft",
                38 => "ArrowUp",
                39 => "ArrowRight",
                40 => "ArrowDown",
                13 => "Enter",
                88 => "KeyX",
                _ => $"Key{keyCode}"
            };
        }

        private async Task SendKeyToWebViewAsync(int keyCode)
        {
            WebView2Control.Focus(FocusState.Programmatic);

            string key = KeyCodeToKey(keyCode);
            string code = KeyCodeToCode(keyCode);

            string script = $@"
                var downEvt = new KeyboardEvent('keydown', {{
                    keyCode: {keyCode}, which: {keyCode}, key: '{key}', code: '{code}', bubbles: true
                }});
                document.dispatchEvent(downEvt);

                var upEvt = new KeyboardEvent('keyup', {{
                    keyCode: {keyCode}, which: {keyCode}, key: '{key}', code: '{code}', bubbles: true
                }});
                document.dispatchEvent(upEvt);
            ";
            await WebView2Control.CoreWebView2.ExecuteScriptAsync(script);
        }
    }
}
