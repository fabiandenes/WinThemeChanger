using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Threading.Tasks;

public static class ThemeChanger
{

    private const double Latitude = 47.4979;
    private const double Longitude = 19.0402;

    private const int CheckIntervalMs = 600000;

    private static NotifyIcon? _notifyIcon;

private static System.Windows.Forms.Timer? _timer;
    private static Theme _currentTheme = Theme.Unknown;

    [STAThread]
    public static void Main()
    {

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "ThemeChanger"
        };

        _timer = new System.Windows.Forms.Timer
        {
            Interval = CheckIntervalMs,
            Enabled = true
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        Application.Run();
    }
    
    private static void Timer_Tick(object? sender, EventArgs e)
    {
        _ = CheckThemeAsync();
    }   

    private static async Task CheckThemeAsync()
    {
        try
        {

            var (sunrise, sunset) = await GetSunriseSunsetTimesAsync();
            var now = DateTime.Now;

            var desiredTheme = (now > sunrise && now < sunset) ? Theme.Light : Theme.Dark;

            if (desiredTheme != _currentTheme)
            {
                SetWindowsTheme(desiredTheme);
                _currentTheme = desiredTheme;

                ShowNotification("Téma váltás", "A téma most: " + desiredTheme.ToString());
            }
        }
        catch (Exception ex)
        {
            ShowNotification("Hiba", "Nem sikerült lekérdezni a napkelte/napnyugta időpontokat.");            Console.WriteLine($"Hiba: {ex.Message}");
        }
    }

    private static async Task<(DateTime sunrise, DateTime sunset)> GetSunriseSunsetTimesAsync()
    {
        string url = $"https://api.sunrise-sunset.org/json?lat={Latitude}&lng={Longitude}&formatted=0";

        using var client = new HttpClient();
        var response = await client.GetStringAsync(url);

        var json = JsonDocument.Parse(response);
        var results = json.RootElement.GetProperty("results");

        var sunrise = DateTime.Parse(results.GetProperty("sunrise").GetString()!).ToLocalTime();
        var sunset = DateTime.Parse(results.GetProperty("sunset").GetString()!).ToLocalTime();

        return (sunrise, sunset);
    }

    private static void SetWindowsTheme(Theme theme)
    {
        const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        int value = (theme == Theme.Light) ? 1 : 0;

        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
        if (key != null)
        {

            key.SetValue("AppsUseLightTheme", value, RegistryValueKind.DWord);

            key.SetValue("SystemsUsersLightTheme", value, RegistryValueKind.DWord);
        }
    }

    private static void ShowNotification(string title, string message)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.ShowBalloonTip(2000, title, message, ToolTipIcon.Info);
        }
    }
    
    private enum Theme {Unknown, Light, Dark}
}