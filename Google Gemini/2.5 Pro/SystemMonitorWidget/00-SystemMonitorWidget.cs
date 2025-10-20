// SystemMonitorWidget.cs
// A complete, single-file WinUI 3 application.
//
// HOW TO COMPILE AND RUN:
// 1. Make sure you have the ".NET Desktop Development" and "Universal Windows Platform development"
//    workloads installed via the Visual Studio Installer.
// 2. Ensure you have the latest Windows App SDK installed.
// 3. Create a new C# Console App project in Visual Studio.
// 4. Replace the entire contents of Program.cs with this code.
// 5. Add the required NuGet packages to your project:
//    - Microsoft.WindowsAppSDK
//    - CommunityToolkit.Mvvm
// 6. In your project file (.csproj), ensure the following:
//    <OutputType>WinExe</OutputType>
//    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework> // Or a later version
//    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
//    <UseWinUI>true</UseWinUI>
//    <EnableMsixTooling>true</EnableMsixTooling>
// 7. Build and run the project.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

// --- Main Application Entry Point ---
public static class Program
{
    [STAThread]
    static void Main()
    {
        // Initialize WinRT and the WinUI 3 application model
        WinRT.ComWrappersSupport.InitializeForWindowing();
        Application.Start((p) =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}

// --- WinUI Application Class ---
public class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Create and activate the main window
        var window = new SystemMonitorWindow();
        window.Activate();
    }
}

// --- Model for a single system metric ---
public partial class SystemMetric : ObservableObject
{
    [ObservableProperty]
    private string? category;

    [ObservableProperty]
    private double value;

    [ObservableProperty]
    private string? formattedValue;

    [ObservableProperty]
    private string? used;

    [ObservableProperty]
    private string? total;

    [ObservableProperty]
    private string? details;
}

// --- ViewModel to manage and update system metrics ---
public partial class SystemMonitorViewModel : ObservableObject
{
    // Observable properties for each metric, which the UI will bind to.
    [ObservableProperty]
    private SystemMetric cpuUsage = new() { Category = "CPU Usage" };

    [ObservableProperty]
    private SystemMetric ramUsage = new() { Category = "Memory Usage" };

    [ObservableProperty]
    private SystemMetric gpuUsage = new() { Category = "GPU Usage" };

    [ObservableProperty]
    private SystemMetric diskUsage = new() { Category = "Disk Usage" };

    // Performance counters to get system data
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _ramCounter;
    private readonly DriveInfo _primaryDrive;

    public SystemMonitorViewModel()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            _primaryDrive = DriveInfo.GetDrives().FirstOrDefault(d => d.Name == Path.GetPathRoot(Environment.SystemDirectory)) ?? DriveInfo.GetDrives().First();
        }
        catch (Exception ex)
        {
            // Handle cases where performance counters can't be initialized (e.g., permissions)
            Debug.WriteLine($"Error initializing performance counters: {ex.Message}");
            CpuUsage.Details = "Could not load counter.";
            RamUsage.Details = "Could not load counter.";
        }

        InitializeMetrics();
    }

    private void InitializeMetrics()
    {
        // Get initial static values
        CpuUsage.Details = $"Cores: {Environment.ProcessorCount}";

        // Start a timer to update metrics every second
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) => UpdateMetrics();
        timer.Start();

        // Perform an initial update
        UpdateMetrics();
    }

    private void UpdateMetrics()
    {
        // --- CPU ---
        if (_cpuCounter != null)
        {
            var cpuValue = _cpuCounter.NextValue();
            CpuUsage.Value = cpuValue;
            CpuUsage.FormattedValue = $"{cpuValue:F2}%";
        }

        // --- RAM ---
        if (_ramCounter != null)
        {
            // This is a more reliable way to get total physical memory
            using (var mc = new System.Management.ManagementClass("Win32_ComputerSystem"))
            using (var moc = mc.GetInstances())
            {
                foreach (var mo in moc)
                {
                    double totalRam = Math.Round(Convert.ToDouble(mo["TotalPhysicalMemory"]) / (1024 * 1024 * 1024), 2);
                    double usedRam = totalRam - (_ramCounter.NextValue() / 1024);
                    double ramPercentage = (usedRam / totalRam) * 100;

                    RamUsage.Value = ramPercentage;
                    RamUsage.FormattedValue = $"{ramPercentage:F2}%";
                    RamUsage.Used = $"Used: {usedRam:F2} GB";
                    RamUsage.Total = $"Total: {totalRam:F2} GB";
                    break; // Only need the first instance
                }
            }
        }
        
        // --- GPU ---
        // NOTE: Getting GPU usage is complex and often requires vendor-specific libraries (NVML, AGS)
        // or lower-level Windows APIs not easily accessible from managed C#.
        // We will simulate the data for this example.
        var random = new Random();
        double gpuValue = random.Next(20, 60) + random.NextDouble();
        GpuUsage.Value = gpuValue;
        GpuUsage.FormattedValue = $"{gpuValue:F2}%";
        GpuUsage.Details = "Note: Data is simulated";

        // --- DISK ---
        if (_primaryDrive != null)
        {
            long totalSize = _primaryDrive.TotalSize;
            long freeSpace = _primaryDrive.AvailableFreeSpace;
            long usedSpace = totalSize - freeSpace;
            double diskPercentage = (double)usedSpace / totalSize * 100;

            DiskUsage.Value = diskPercentage;
            DiskUsage.FormattedValue = $"{diskPercentage:F2}%";
            DiskUsage.Used = $"Used: {(double)usedSpace / (1024 * 1024 * 1024):F2} GB";
            DiskUsage.Total = $"Total: {(double)totalSize / (1024 * 1024 * 1024):F2} GB";
        }
    }
}


// --- Main Application Window ---
public class SystemMonitorWindow : Window
{
    private AppWindow _appWindow;

    public SystemMonitorWindow()
    {
        this.Title = "System Monitor";

        // --- Window Customization ---
        _appWindow = GetAppWindowForCurrentWindow();

        // Use CompactOverlayPresenter for an always-on-top, minimal window
        if (_appWindow.Presenter is OverlappedPresenter overlappedPresenter)
        {
            _appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
        }
        
        _appWindow.Resize(new SizeInt32(380, 520)); // Set initial size

        // Hide system title bar to create the "headless" look
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(null);

        // Load the UI from an embedded XAML string
        string xaml = GetXamlContent();
        this.Content = XamlReader.Load(xaml);

        // Set the DataContext for data binding
        if (this.Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = new SystemMonitorViewModel();
        }
    }

    // Helper to get the AppWindow
    private AppWindow GetAppWindowForCurrentWindow()
    {
        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(wndId);
    }

    // --- Embedded XAML for the UI ---
    private string GetXamlContent()
    {
        return @"
<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
      Background='#1C1C1E'>

    <StackPanel Margin='20'>
        <TextBlock Text='System Metrics' FontSize='20' FontWeight='SemiBold' Foreground='White' Margin='0,0,0,20'/>

        <!-- CPU Usage Card -->
        <Border Background='#2C2C2E' CornerRadius='12' Padding='16' Margin='0,0,0,12'>
            <StackPanel>
                <TextBlock Text='{Binding CpuUsage.Category}' Foreground='#E5E5EA' FontSize='14' Margin='0,0,0,4'/>
                <TextBlock Text='{Binding CpuUsage.FormattedValue}' Foreground='White' FontSize='32' FontWeight='Bold'/>
                <Grid Margin='0,12,0,0'>
                    <ProgressBar Value='{Binding CpuUsage.Value}' Maximum='100' Height='6' CornerRadius='3' 
                                 Foreground='#34C759' Background='#4A4A4D' BorderThickness='0'/>
                    <TextBlock Text='{Binding CpuUsage.Details}' Foreground='#8E8E93' FontSize='12' HorizontalAlignment='Right' VerticalAlignment='Center'/>
                </Grid>
            </StackPanel>
        </Border>

        <!-- Memory Usage Card -->
        <Border Background='#2C2C2E' CornerRadius='12' Padding='16' Margin='0,0,0,12'>
            <StackPanel>
                <TextBlock Text='{Binding RamUsage.Category}' Foreground='#E5E5EA' FontSize='14' Margin='0,0,0,4'/>
                <TextBlock Text='{Binding RamUsage.FormattedValue}' Foreground='White' FontSize='32' FontWeight='Bold'/>
                <Grid Margin='0,12,0,0'>
                    <ProgressBar Value='{Binding RamUsage.Value}' Maximum='100' Height='6' CornerRadius='3' 
                                 Foreground='#FF9500' Background='#4A4A4D' BorderThickness='0'/>
                    <TextBlock Text='{Binding RamUsage.Used}' Foreground='#8E8E93' FontSize='12' HorizontalAlignment='Left' VerticalAlignment='Center'/>
                    <TextBlock Text='{Binding RamUsage.Total}' Foreground='#8E8E93' FontSize='12' HorizontalAlignment='Right' VerticalAlignment='Center'/>
                </Grid>
            </StackPanel>
        </Border>
        
        <!-- Disk Usage Card -->
        <Border Background='#2C2C2E' CornerRadius='12' Padding='16' Margin='0,0,0,12'>
            <StackPanel>
                <TextBlock Text='{Binding DiskUsage.Category}' Foreground='#E5E5EA' FontSize='14' Margin='0,0,0,4'/>
                <TextBlock Text='{Binding DiskUsage.FormattedValue}' Foreground='White' FontSize='32' FontWeight='Bold'/>
                <Grid Margin='0,12,0,0'>
                    <ProgressBar Value='{Binding DiskUsage.Value}' Maximum='100' Height='6' CornerRadius='3' 
                                 Foreground='#FF3B30' Background='#4A4A4D' BorderThickness='0'/>
                    <TextBlock Text='{Binding DiskUsage.Used}' Foreground='#8E8E93' FontSize='12' HorizontalAlignment='Left' VerticalAlignment='Center'/>
                    <TextBlock Text='{Binding DiskUsage.Total}' Foreground='#8E8E93' FontSize='12' HorizontalAlignment='Right' VerticalAlignment='Center'/>
                </Grid>
            </StackPanel>
        </Border>

        <!-- GPU Usage Card -->
        <Border Background='#2C2C2E' CornerRadius='12' Padding='16'>
            <StackPanel>
                <TextBlock Text='{Binding GpuUsage.Category}' Foreground='#E5E5EA' FontSize='14' Margin='0,0,0,4'/>
                <TextBlock Text='{Binding GpuUsage.FormattedValue}' Foreground='White' FontSize='32' FontWeight='Bold'/>
                <Grid Margin='0,12,0,0'>
                    <ProgressBar Value='{Binding GpuUsage.Value}' Maximum='100' Height='6' CornerRadius='3' 
                                 Foreground='#5856D6' Background='#4A4A4D' BorderThickness='0'/>
                    <TextBlock Text='{Binding GpuUsage.Details}' Foreground='#8E8E93' FontSize='12' HorizontalAlignment='Right' VerticalAlignment='Center'/>
                </Grid>
            </StackPanel>
        </Border>

    </StackPanel>
</Grid>
        ";
    }
}
