﻿using System;

namespace SAFE.NetworkDrive.UI
{
    class ApplicationManagement
    {
        public Action<char> Explore { get; set; }
        public Action OpenDriveSettings { get; set; }
        public Action<char> ToggleMount { get; set; }
        public Action UnmountAll { get; set; }
        public Action Exit { get; set; }
    }
}