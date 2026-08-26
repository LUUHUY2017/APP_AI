using System.Windows;
using Xiaozhi.Core.Utils;

namespace Xiaozhi.App.Wpf.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var config = ConfigManager.Instance.Config;
        TxtServerUrl.Text = config.SystemOptions.Network.WebSocketUrl;
        TxtToken.Text = config.SystemOptions.Network.WebSocketAccessToken;
        TxtWakeWord.Text = "xiaozhi";
        ChkWakeWord.IsChecked = config.WakeWordOptions.UseWakeWord;
    }

    private void GetTokenQr_Click(object sender, RoutedEventArgs e)
    {
        var actWindow = new ActivationWindow();
        actWindow.Owner = this;
        if (actWindow.ShowDialog() == true && !string.IsNullOrEmpty(actWindow.ActivatedToken))
        {
            TxtToken.Text = actWindow.ActivatedToken;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var config = ConfigManager.Instance.Config;
        config.SystemOptions.Network.WebSocketUrl = TxtServerUrl.Text;
        config.SystemOptions.Network.WebSocketAccessToken = TxtToken.Text;
        config.WakeWordOptions.UseWakeWord = ChkWakeWord.IsChecked ?? true;
        ConfigManager.Instance.SaveConfig(config);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
