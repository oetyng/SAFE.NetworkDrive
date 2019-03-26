using System;
using System.Windows.Forms;

namespace SAFE.NetworkDrive.UI
{
    class DriveNotifyIcon
    {
        static NotifyIcon _notifyIcon;
        readonly ApplicationManagement _app;
        readonly char _driveLetter;
        
        DriveNotifyIcon(char driveLetter, ApplicationManagement app)
        {
            _app = app;
            _driveLetter = driveLetter;
            SetupNotifyIcon();
        }

        public static DriveNotifyIcon Create(char driveLetter, ApplicationManagement app)
            => new DriveNotifyIcon(driveLetter, app);

        void SetupNotifyIcon()
        {
            if (_notifyIcon == null)
            {
                MenuItem[] menuItems = new MenuItem[3];
                menuItems[0] = new MenuItem("E&xit", OnExit);
                menuItems[1] = new MenuItem("&Unmount All", OnUnmountAll);
                menuItems[2] = new MenuItem("&Settings", OnSettings);

                var icon = new System.Drawing.Icon("./Resources/favicon_white.ico");

                _notifyIcon = new NotifyIcon
                {
                    Text = $"SAFE.NetworkDrive",
                    Icon = icon,
                    ContextMenu = new ContextMenu(menuItems)
                };
                _notifyIcon.BalloonTipClosed += (sender, e) => { var thisIcon = (NotifyIcon)sender; thisIcon.Visible = false; thisIcon.Dispose(); };
                _notifyIcon.DoubleClick += (s, e) => _app.OpenDriveSettings();
                _notifyIcon.Visible = true;
            }

            var mainMenu = _notifyIcon.ContextMenu.MenuItems;
            var menuItem = new MenuItem(_driveLetter.ToString());
            menuItem.Name = _driveLetter.ToString();
            menuItem.MenuItems.Add(new MenuItem("Explore", OnStartExplorer));
            menuItem.MenuItems.Add(new MenuItem("Toggle mount", OnToggleMount));
            mainMenu.Add(menuItem);
        }

        public void RemoveMenuItem()
        {
            var mainMenu = _notifyIcon.ContextMenu.MenuItems;
            mainMenu.RemoveByKey(_driveLetter.ToString());
        }

        void OnToggleMount(object sender, EventArgs e)
            => _app.ToggleMount(_driveLetter);

        void OnStartExplorer(object sender, EventArgs e)
            => _app.Explore(_driveLetter);

        void OnExit(object sender, EventArgs e)
        {
            var res = System.Windows.MessageBox.Show("Exiting SAFE.NetworkDrive mounter.", "Exit", System.Windows.MessageBoxButton.OKCancel);
            if (res == System.Windows.MessageBoxResult.OK)
            {
                _notifyIcon.Visible = false;
                _app.Exit();
            }
        }

        void OnSettings(object sender, EventArgs e)
           => _app.OpenDriveSettings();

        void OnUnmountAll(object sender, EventArgs e)
           => _app.UnmountAll();
    }
}