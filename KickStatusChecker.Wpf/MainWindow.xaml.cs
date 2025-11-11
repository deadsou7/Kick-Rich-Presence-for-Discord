using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using KickStatusChecker.Wpf.ViewModels;
using KickStatusChecker.Wpf.Services;

namespace KickStatusChecker.Wpf;

public partial class MainWindow : Window
{
    private readonly NotifyIcon _notifyIcon;
    private bool _isExitRequested;

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        Logger.LogInfo("Initializing MainWindow");
        
        InitializeComponent();

        var viewModel = new MainViewModel();
        viewModel.RequestExit += OnRequestExit;
        viewModel.RequestMinimize += OnRequestMinimize;
        DataContext = viewModel;

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Kick Stream Monitor",
            Visible = false
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open", null, (_, _) => RestoreFromTray());
        contextMenu.Items.Add("Exit", null, (_, _) => ExitFromTray());
        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => RestoreFromTray();
        
        Logger.LogInfo("MainWindow initialized successfully");
    }

    private void OnRequestExit(object? sender, EventArgs e)
    {
        _isExitRequested = true;
        Close();
    }

    private void OnRequestMinimize(object? sender, EventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void ExitFromTray()
    {
        _isExitRequested = true;
        Close();
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
        _notifyIcon.Visible = false;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && ViewModel.MinimizeToTray)
        {
            Hide();
            ShowInTaskbar = false;
            _notifyIcon.Visible = true;
        }
        else if (WindowState == WindowState.Normal)
        {
            ShowInTaskbar = true;
            _notifyIcon.Visible = false;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExitRequested && ViewModel.MinimizeToTray)
        {
            Logger.LogInfo("Application minimized to tray instead of closing");
            e.Cancel = true;
            Hide();
            ShowInTaskbar = false;
            _notifyIcon.Visible = true;
            return;
        }

        Logger.LogInfo("Application is closing");
        
        base.OnClosing(e);
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        ViewModel.Dispose();
        
        Logger.LogInfo("Application closed successfully");
    }
}
