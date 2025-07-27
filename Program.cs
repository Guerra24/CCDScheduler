using CCDScheduler;
using DotNet.Globbing;
using H.NotifyIcon.Core;
using HideConsoleOnCloseManaged;
using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Text.Json;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

PInvoke.AllocConsole();
PInvoke.SetConsoleOutputCP(65001u);
PInvoke.SetConsoleTitle("CCD Scheduler");
PInvoke.ShowWindow(PInvoke.GetConsoleWindow(), SHOW_WINDOW_CMD.SW_HIDE);
HideConsoleOnClose.EnableForWindow(PInvoke.GetConsoleWindow());

GlobOptions.Default.Evaluation.CaseInsensitive = true;

var conf = JsonSerializer.Deserialize<Config>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.json")), JsonSettings.Options)!;

var rules = new List<MatcherAffinity>();

foreach (var rule in conf.ProcessRules)
    foreach (var selector in rule.Selectors)
        rules.Add(new MatcherAffinity(Glob.Parse(selector), rule.Affinity, rule.Delay));

using var watcher = new ManagementEventWatcher(new WqlEventQuery("Win32_ProcessStartTrace", TimeSpan.FromSeconds(1)));
watcher.EventArrived += (sender, e) =>
{
    System.Threading.Tasks.Task.Run(async () =>
    {
        try
        {
            int processId = int.Parse(e.NewEvent.Properties["ProcessId"].Value.ToString()!);
            using Process proc = Process.GetProcessById(processId);
            var path = proc.MainModule!.FileName;

            try
            {
                foreach (var rule in rules)
                    if (rule.Glob.IsMatch(path))
                    {
                        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(rule.Delay));
                        if (proc.HasExited)
                            return;
                        proc.ProcessorAffinity = rule.Affinity;
                        Console.WriteLine($"{path}: {rule.Affinity:x}");
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        catch { }
    });
};

watcher.Start();

using var iconStream = H.Resources.icon_ico.AsStream();
using var icon = new Icon(iconStream);

using var trayIcon = new TrayIconWithContextMenu(Guid.Parse("d47d23c3-92cc-44f4-b04d-d27cca3569eb"))
{
    Icon = icon.Handle,
    ToolTip = "CDD Scheduler"
};

bool running = true;

trayIcon.ContextMenu = new PopupMenu
{
    Items =
    {

        new PopupMenuItem("Show Console", (_,_) =>
        {
            PInvoke.ShowWindow(PInvoke.GetConsoleWindow(), SHOW_WINDOW_CMD.SW_SHOW);
            PInvoke.ShowWindow(PInvoke.GetConsoleWindow(), SHOW_WINDOW_CMD.SW_RESTORE);
        }),
        new PopupMenuItem("Run on Startup", (sender,_) =>
        {
            var item = (PopupMenuItem)sender!;
            var task = TaskService.Instance.GetTask("CCDSchedulerTask");
            if (task == null)
            {
                using var td = TaskService.Instance.NewTask();
                td.Principal.LogonType = TaskLogonType.InteractiveToken;
                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Triggers.Add(new LogonTrigger());
                td.Actions.Add(new ExecAction(Environment.ProcessPath, workingDirectory: AppContext.BaseDirectory));
                
                task = TaskService.Instance.RootFolder.RegisterTaskDefinition("CCDSchedulerTask", td);
                
                item.Checked = true;
            } else
            {
                TaskService.Instance.RootFolder.DeleteTask("CCDSchedulerTask");
                item.Checked = false;
            }
            task.Dispose();
        }),
        new PopupMenuSeparator(),
        new PopupMenuItem("Exit", (_,_) =>
        {
            running = false;
        })
    }
};

var task = TaskService.Instance.GetTask("CCDSchedulerTask");
if (task == null)
{
    ((PopupMenuItem)trayIcon.ContextMenu.Items.ElementAt(1)).Checked = false;
}
else
{
    ((PopupMenuItem)trayIcon.ContextMenu.Items.ElementAt(1)).Checked = true;
    task.Dispose();
}

trayIcon.Create();

while (running)
    Thread.Sleep(1000);

watcher.Stop();
TaskService.Instance.Dispose();