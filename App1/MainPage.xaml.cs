using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace App1
{
    public sealed partial class MainPage : Page
    {
        private Gamepad gamepad;
        private GamepadButtons lastButtons = GamepadButtons.None;

        public MainPage()
        {
            this.InitializeComponent();

            WebView2Control.CoreWebView2Initialized += WebView2Control_CoreWebView2Initialized;

            WebView2Control.Loaded += (s, e) =>
            {
                WebView2Control.Focus(Windows.UI.Xaml.FocusState.Programmatic);

                var view = ApplicationView.GetForCurrentView();
                view.TryEnterFullScreenMode();
            };

            // Captura de teclado físico (solo en PC)
            WebView2Control.KeyDown += WebView2Control_KeyDown;

            // Captura de Gamepad (DPad + A + X)
            Gamepad.GamepadAdded += (sender, e) => gamepad = e;
            CompositionTarget.Rendering += UpdateGamepadState;
        }

        private async void WebView2Control_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            int keyCode = 0;

            switch (e.Key)
            {
                case VirtualKey.Up:
                    keyCode = 38; break; // Arrow Up
                case VirtualKey.Down:
                    keyCode = 40; break; // Arrow Down
                case VirtualKey.Left:
                    keyCode = 37; break; // Arrow Left
                case VirtualKey.Right:
                    keyCode = 39; break; // Arrow Right
                case VirtualKey.Enter:
                    keyCode = 13; break; // Enter
                case VirtualKey.X:
                    keyCode = 88; break; // X
            }

            if (keyCode != 0 && WebView2Control.CoreWebView2 != null)
            {
                await SendKeyToWebViewAsync(keyCode);
                e.Handled = true;
            }
        }

        private async void UpdateGamepadState(object sender, object e)
        {
            if (gamepad == null || WebView2Control.CoreWebView2 == null) return;

            var reading = gamepad.GetCurrentReading();

            // Detectar solo cambios de estado para evitar repetición excesiva
            if (reading.Buttons != lastButtons)
            {
                if (reading.Buttons.HasFlag(GamepadButtons.DPadUp) && !lastButtons.HasFlag(GamepadButtons.DPadUp))
                    await SendKeyToWebViewAsync(38); // Arrow Up

                if (reading.Buttons.HasFlag(GamepadButtons.DPadDown) && !lastButtons.HasFlag(GamepadButtons.DPadDown))
                    await SendKeyToWebViewAsync(40); // Arrow Down

                if (reading.Buttons.HasFlag(GamepadButtons.DPadLeft) && !lastButtons.HasFlag(GamepadButtons.DPadLeft))
                    await SendKeyToWebViewAsync(37); // Arrow Left

                if (reading.Buttons.HasFlag(GamepadButtons.DPadRight) && !lastButtons.HasFlag(GamepadButtons.DPadRight))
                    await SendKeyToWebViewAsync(39); // Arrow Right

                if (reading.Buttons.HasFlag(GamepadButtons.A) && !lastButtons.HasFlag(GamepadButtons.A))
                    await SendKeyToWebViewAsync(13); // Enter (A)

                if (reading.Buttons.HasFlag(GamepadButtons.X) && !lastButtons.HasFlag(GamepadButtons.X))
                    await SendKeyToWebViewAsync(88); // Letra X
            }

            lastButtons = reading.Buttons;
        }

        private async Task SendKeyToWebViewAsync(int keyCode)
        {
            // Forzar foco antes de enviar el keydown
            WebView2Control.Focus(Windows.UI.Xaml.FocusState.Programmatic);

            string script = $@"
                var evt = new KeyboardEvent('keydown', {{ keyCode: {keyCode}, which: {keyCode} }});
                document.dispatchEvent(evt);
            ";
            await WebView2Control.CoreWebView2.ExecuteScriptAsync(script);
        }

        private void WebView2Control_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            WebView2Control.CoreWebView2.OpenDevToolsWindow();
        }
    }
}
