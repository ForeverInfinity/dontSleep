using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DontSleep
{
    class TrayApp
    {
        //控件初始化
        Container container = new Container();
        NotifyIcon notifyIcon = new NotifyIcon();
        ContextMenuStrip mainMenu = new ContextMenuStrip();
        ToolStripMenuItem menuItemPreventSystemSleep = new ToolStripMenuItem("阻止系统休眠");
        ToolStripMenuItem menuItemPreventScreenOff = new ToolStripMenuItem("阻止屏幕休眠");
        ToolStripSeparator menuSeparator1 = new ToolStripSeparator();
        ToolStripMenuItem menuItemLockAccount = new ToolStripMenuItem("锁定屏幕");
        ToolStripMenuItem menuItemScreenOff = new ToolStripMenuItem("关闭屏幕");
        ToolStripSeparator menuSeparator2 = new ToolStripSeparator();
        ToolStripMenuItem menuItemAutoRun = new ToolStripMenuItem("随系统启动");
        ToolStripMenuItem menuItemExit = new ToolStripMenuItem("退出程序");


        public TrayApp()
        {
            //绑定控件
            container.Add(notifyIcon);
            container.Add(mainMenu);

            //绑定菜单选项
            mainMenu.Items.Add(menuItemPreventSystemSleep);
            mainMenu.Items.Add(menuItemPreventScreenOff);
            mainMenu.Items.Add(menuSeparator1);
            mainMenu.Items.Add(menuItemLockAccount);
            mainMenu.Items.Add(menuItemScreenOff);
            mainMenu.Items.Add(menuSeparator2);
            mainMenu.Items.Add(menuItemAutoRun);
            mainMenu.Items.Add(menuItemExit);

            //注册菜单事件
            menuItemPreventSystemSleep.Click += MenuItemPreventSystemSleep_Click;
            menuItemPreventScreenOff.Click += MenuItemPreventScreenSleep_Click;
            menuItemLockAccount.Click += MenuItemLockAccount_Click;
            menuItemScreenOff.Click += MenuItemScreenOff_Click;
            menuItemAutoRun.Click += MenuItemAutoRun_Click;
            menuItemExit.Click += MenuItemExit_Click;

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }



        public void Run()
        {
            notifyIcon.ContextMenuStrip = mainMenu;
            notifyIcon.Text = Properties.Resources.Name;
            SetTray();
            menuItemAutoRun.Checked = Controller.AutoRunController.AutoStart;
            menuItemPreventScreenOff.Enabled = false;
            notifyIcon.Visible = true;
            Application.Run();
        }
        public void Exit()
        {
            container.Dispose();
            Application.Exit();
        }
        private void SetTray()
        {
            string trayText = Properties.Resources.Name;
            if (Controller.SleepController.PreventSystemSleep)
            {
                trayText += "\n";
                trayText += Properties.Resources.Tip_PreventSystemSleep;
                if (Controller.SleepController.PreventMonitorOff)
                {
                    trayText += "\n";
                    trayText += Properties.Resources.Tip_PreventScreenOff;
                }
                notifyIcon.Icon = Properties.Resources.Icon16_on;
            }

            else
                notifyIcon.Icon = Properties.Resources.Icon16_off;
            notifyIcon.Text = trayText;
        }
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            Debug.WriteLine($"PowerModeChanged:{e.Mode}");
            if (e.Mode == PowerModes.Resume)
            {
                Controller.SleepController.PreventSystemSleep = Controller.SleepController.PreventSystemSleep;
                Controller.SleepController.PreventMonitorOff = Controller.SleepController.PreventMonitorOff;
            }

        }

        private void MenuItemPreventSystemSleep_Click(object sender, EventArgs e)
        {
            menuItemPreventSystemSleep.Checked = Controller.SleepController.PreventSystemSleep = !Controller.SleepController.PreventSystemSleep;
            menuItemPreventScreenOff.Enabled = menuItemPreventSystemSleep.Checked;
            SetTray();
        }

        private void MenuItemPreventScreenSleep_Click(object sender, EventArgs e)
        {
            menuItemPreventScreenOff.Checked = Controller.SleepController.PreventMonitorOff = !Controller.SleepController.PreventMonitorOff;
            SetTray() ;
        }
        private void MenuItemLockAccount_Click(object sender, EventArgs e)
        {
            Controller.AccountController.LockAccount();
        }
        private void MenuItemScreenOff_Click(object sender, EventArgs e)
        {
            Controller.MonitorController.TurnOffMonitor();
        }
        private void MenuItemAutoRun_Click(object sender, EventArgs e)
        {

            menuItemAutoRun.Checked = Controller.AutoRunController.AutoStart = !Controller.AutoRunController.AutoStart;
        }
        private void MenuItemExit_Click(object sender, EventArgs e)
        {
            Exit();
        }
    }

    static class Controller
    {
        public static class AutoRunController
        {
            static readonly RegistryKey run = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Microsoft").OpenSubKey("Windows").OpenSubKey("CurrentVersion").OpenSubKey("Run", true);
            public static bool AutoStart
            {
                get
                {
                    return GetAutoStart();
                }
                set
                {
                    SetAutoStart(value);
                }
            }

            static bool SetAutoStart(bool aotoStart)
            {
                try
                {
                    if (aotoStart)
                        run.SetValue(Properties.Resources.Name, Application.ExecutablePath, RegistryValueKind.String);
                    else
                        run.DeleteValue(Properties.Resources.Name);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            static bool GetAutoStart()
            {
                try
                {
                    string value = (string)run.GetValue(Properties.Resources.Name);
                    if (value == Application.ExecutablePath)
                        return true;
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }
        public static class SleepController
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
            [Flags]
            public enum EXECUTION_STATE : uint
            {
                ES_AWAYMODE_REQUIRED = 0x00000040,
                ES_CONTINUOUS = 0x80000000,
                ES_DISPLAY_REQUIRED = 0x00000002,
                ES_SYSTEM_REQUIRED = 0x00000001
                // Legacy flag, should not be used.
                // ES_USER_PRESENT = 0x00000004
            }

            static bool preventSystemSleep = false;
            public static bool PreventSystemSleep
            {
                get
                {
                    return preventSystemSleep;
                }
                set
                {
                    preventSystemSleep = value;
                    Call_SetThreadExecutionState();
                }
            }
            static bool preventMonitorOff = false;
            public static bool PreventMonitorOff
            {
                get
                {
                    return preventMonitorOff;
                }
                set
                {
                    preventMonitorOff = value;
                    Call_SetThreadExecutionState();
                }
            }
            static void Call_SetThreadExecutionState()
            {
                EXECUTION_STATE esFlags = EXECUTION_STATE.ES_CONTINUOUS;
                if (preventSystemSleep)
                    if (preventMonitorOff)
                        esFlags |= EXECUTION_STATE.ES_DISPLAY_REQUIRED;
                    else
                        esFlags |= EXECUTION_STATE.ES_SYSTEM_REQUIRED;
                Debug.WriteLine($"SetFlags:{esFlags}");
                SetThreadExecutionState(esFlags);
            }
        }

        public static class MonitorController
        {
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
            [DllImport("user32.dll")]
            static extern IntPtr GetShellWindow();

            static readonly uint WM_SYSCOMMAND = 0x112;                    //系统消息
            static readonly IntPtr SC_MONITORPOWER = new IntPtr(0xF170);   //关闭显示器的系统命令
            static readonly IntPtr MONITOR_OFF = new IntPtr(0x002);
            public static void TurnOffMonitor()
            {
                Debug.WriteLine($"ShellWindowHandle:{GetShellWindow().ToInt32()}");
                PostMessage(GetShellWindow(), WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_OFF);
            }
        }

        public static class AccountController
        {
            [DllImport("user32.dll", SetLastError = true)]
            static extern bool LockWorkStation();

            public static void LockAccount()
            {
                LockWorkStation();
            }
        }
    }
}

