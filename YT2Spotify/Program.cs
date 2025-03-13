using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using WindowsInput;

internal class Program
{
    private const int MouseEventLeftDown = 0x02;
    private const int MouseEventLeftUp = 0x04;
    private const int SW_MINIMIZE = 6;
    private static string lastSong = "";

    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8; // Allow Emojis in the console
        Console.WriteLine("🚀 YouTube Music → Spotify Sync is running!");

        while (true)
        {
            try
            {
                var currentSong = GetCurrentSong();

                if (!string.IsNullOrEmpty(currentSong) && currentSong != lastSong)
                {
                    Console.WriteLine($"🎵 Detected new song: {currentSong}");
                    StartSpotifySong(currentSong);
                    lastSong = currentSong;
                }
                else
                {
                    Console.WriteLine("🔄 Waiting for song change...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error: {ex.Message}");
            }

            Thread.Sleep(1000); // Check every second for faster sync
        }
    }

    private static string GetCurrentSong()
    {
        using var client = new HttpClient();
        var response = client.GetStringAsync("http://localhost:26538/api/v1/song").Result;

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        var title = root.GetProperty("title").GetString();
        var artist = root.GetProperty("artist").GetString();

        return $"{artist} {title}";
    }

    private static void StartSpotifySong(string song)
    {
        var sim = new InputSimulator();

        // Save the current mouse position
        GetCursorPos(out var originalMousePosition);

        // Open Spotify directly via URI search
        var spotifyUri = $"spotify:search:{Uri.EscapeDataString(song)}";
        Process.Start(new ProcessStartInfo(spotifyUri) { UseShellExecute = true });

        Thread.Sleep(2500); // Allow Spotify to load results

        // Focus Spotify window
        var spotifyProcesses = Process.GetProcessesByName("Spotify");
        if (spotifyProcesses.Length > 0)
        {
            SetForegroundWindow(spotifyProcesses[0].MainWindowHandle);
            Thread.Sleep(500);
        }

        // Set coordinates for the first search result (adjust as necessary!)
        var posX = 810; // horizontal position
        var posY = 400; // vertical position

        // Move mouse to Spotify search result and click to play
        SetCursorPos(posX, posY);
        Thread.Sleep(300);
        mouse_event(MouseEventLeftDown | MouseEventLeftUp, 0, 0, 0, 0);
        Thread.Sleep(500);

        // Minimize Spotify after playback starts
        ShowWindow(spotifyProcesses[0].MainWindowHandle, SW_MINIMIZE);

        // Restore mouse position
        SetCursorPos(originalMousePosition.X, originalMousePosition.Y);

        Console.WriteLine($"▶️ Spotify is now playing: {song}");
    }

    // Win32 API imports
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern void mouse_event(
        int dwFlags,
        int dx,
        int dy,
        int dwData,
        int dwExtraInfo
    );

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // Struct for mouse position
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
