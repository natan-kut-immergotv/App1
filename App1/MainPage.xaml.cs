using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Globalization;
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

            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

            WebView2Control.CoreWebView2Initialized += WebView2Control_CoreWebView2Initialized;

            WebView2Control.Loaded += (_, __) =>
            {
                WebView2Control.Focus(FocusState.Programmatic);
            };

            WebView2Control.KeyDown += WebView2Control_KeyDown;

            Gamepad.GamepadAdded += (_, g) => gamepad = g;

            // ✅ Solución: suscripción sin nulabilidad
            Windows.UI.Xaml.Media.CompositionTarget.Rendering += (_, __) => UpdateGamepadState();
        }

        private void WebView2Control_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            WebView2Control.CoreWebView2.DOMContentLoaded += InjectStyleScript;

            // ✅ Cargar tu app React
            WebView2Control.Source = new Uri("https://channel14.smarttv-dev.immergo.tv/");
        }

        private async void InjectStyleScript(CoreWebView2 sender, object args)
        {
            string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            string direction = (lang == "he" || lang == "ar") ? "rtl" : "ltr";

            string script = $@"
                const targetWidth = 1920;
                const targetHeight = 1080;
                const actualWidth = window.innerWidth;
                const scale = actualWidth / targetWidth;
                const direction = '{direction}';

                document.documentElement.setAttribute('dir', direction);
                document.documentElement.style.margin = '0';
                document.documentElement.style.padding = '0';
                document.documentElement.style.overflow = 'hidden';
                document.documentElement.style.width = `${{targetWidth}}px`;
                document.documentElement.style.height = `${{targetHeight}}px`;

                document.body.style.margin = '0';
                document.body.style.padding = '0';
                document.body.style.overflow = 'hidden';
                document.body.style.position = 'absolute';
                document.body.style.top = '0';
                document.body.style.{(direction == "rtl" ? "right" : "left")} = '0';
                document.body.style.width = `${{targetWidth}}px`;
                document.body.style.height = `${{targetHeight}}px`;
                document.body.style.transformOrigin = direction === 'rtl' ? 'top right' : 'top left';
                document.body.style.transform = `scale(${{scale}})`;

                const style = document.createElement('style');
                style.innerHTML = `
                    html, body {{
                        height: ${{targetHeight}}px !important;
                        max-height: ${{targetHeight}}px !important;
                    }}
                    *[style*='100vh'] {{
                        height: ${{targetHeight}}px !important;
                    }}
                `;
                document.head.appendChild(style);
                console.log('✅ Estilos y escalado WebView2 aplicados.');
            ";

            try
            {
                await WebView2Control.CoreWebView2.ExecuteScriptAsync(script);
                await Task.Delay(100);
                WebView2Control.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("❌ Error al inyectar script: " + ex.Message);
            }
        }

        private async void WebView2Control_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            int keyCode = GetKeyCode(e.Key);
            if (keyCode != 0)
            {
                await SendKeyToWebViewAsync(keyCode);
                e.Handled = true;
            }
        }

        // ✅ Ahora sin parámetros anulables y sin warnings
        private async void UpdateGamepadState()
        {
            if (gamepad == null || WebView2Control.CoreWebView2 == null) return;

            var reading = gamepad.GetCurrentReading();
            if (reading.Buttons != lastButtons)
            {
                if (reading.Buttons.HasFlag(GamepadButtons.DPadUp) && !lastButtons.HasFlag(GamepadButtons.DPadUp))
                    await SendKeyToWebViewAsync(38); // Up
                if (reading.Buttons.HasFlag(GamepadButtons.DPadDown) && !lastButtons.HasFlag(GamepadButtons.DPadDown))
                    await SendKeyToWebViewAsync(40); // Down
                if (reading.Buttons.HasFlag(GamepadButtons.DPadLeft) && !lastButtons.HasFlag(GamepadButtons.DPadLeft))
                    await SendKeyToWebViewAsync(37); // Left
                if (reading.Buttons.HasFlag(GamepadButtons.DPadRight) && !lastButtons.HasFlag(GamepadButtons.DPadRight))
                    await SendKeyToWebViewAsync(39); // Right

                if (reading.Buttons.HasFlag(GamepadButtons.A) && !lastButtons.HasFlag(GamepadButtons.A))
                    await SendKeyToWebViewAsync(13); // Enter
                if (reading.Buttons.HasFlag(GamepadButtons.X) && !lastButtons.HasFlag(GamepadButtons.X))
                    await SendKeyToWebViewAsync(88); // x
                if (reading.Buttons.HasFlag(GamepadButtons.Y) && !lastButtons.HasFlag(GamepadButtons.Y))
                    await SendKeyToWebViewAsync(89); // y → búsqueda
                if (reading.Buttons.HasFlag(GamepadButtons.B) && !lastButtons.HasFlag(GamepadButtons.B))
                    await SendKeyToWebViewAsync(27); // Escape → volver
            }

            lastButtons = reading.Buttons;
        }

        private static int GetKeyCode(VirtualKey key) => key switch
        {
            VirtualKey.Up => 38,
            VirtualKey.Down => 40,
            VirtualKey.Left => 37,
            VirtualKey.Right => 39,
            VirtualKey.Enter => 13,
            VirtualKey.X => 88,
            VirtualKey.Y => 89,
            VirtualKey.GamepadB => 27,
            _ => 0
        };

        private static string KeyCodeToKey(int keyCode) => keyCode switch
        {
            37 => "ArrowLeft",
            38 => "ArrowUp",
            39 => "ArrowRight",
            40 => "ArrowDown",
            13 => "Enter",
            88 => "x",
            89 => "y",
            27 => "Escape",
            _ => ""
        };

        private static string KeyCodeToCode(int keyCode) => keyCode switch
        {
            37 => "ArrowLeft",
            38 => "ArrowUp",
            39 => "ArrowRight",
            40 => "ArrowDown",
            13 => "Enter",
            88 => "KeyX",
            89 => "KeyY",
            27 => "Escape",
            _ => $"Key{keyCode}"
        };

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
