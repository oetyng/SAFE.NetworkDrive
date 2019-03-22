# SAFE.NetworkDrive

This repository contains source code for the SAFE.NetworkDrive library (netcore 3.0, C# 8.0).

### Alpha version with WPF GUI and console app:

Event sourced virtual drive, writing encrypted WAL to SQLite, synchronizing to MockNetwork and local materialization to in-memory virtual filesystem.

### Features 

- Multiple local user accounts.
- Multiple local drive configs per local user account.
- One SAFENetwork account per drive.
- Encrypted local data.
- Adds local user if not exists.
- Add/Remove drives.
- Mount / unmount.
- Remove local user.
- Tray icon.

Not yet included:
 - Connecting to live network.

![login](https://user-images.githubusercontent.com/32025054/54231781-74e99c80-4509-11e9-8761-81ad92427a7e.png)
![menuitems](https://user-images.githubusercontent.com/32025054/54231782-74e99c80-4509-11e9-98ef-aa82b4eef4fe.png) ![notifyicon](https://user-images.githubusercontent.com/32025054/54231783-74e99c80-4509-11e9-81d9-a6a0a316efc1.png)
![drive_unmounted](https://user-images.githubusercontent.com/32025054/54231780-74e99c80-4509-11e9-91a5-c5e26c0ea5c3.png)
![drive_mounted](https://user-images.githubusercontent.com/32025054/54231778-74510600-4509-11e9-998c-a7722dddbd6b.png)
![add_drive](https://user-images.githubusercontent.com/32025054/54231777-74510600-4509-11e9-92a6-4b0e2e3b039a.png)

### Prerequisites for app:

Dokan driver (Dokan_x64.msi at https://github.com/dokan-dev/dokany/releases/tag/v1.2.2.1000)

https://github.com/dokan-dev/dokany/wiki/Installation
Dotnet core runtime 3.0 ( .NET Core Installer: [x64](https://dotnet.microsoft.com/download/thank-you/dotnet-runtime-3.0.0-preview3-windows-x64-installer))
https://dotnet.microsoft.com/download/dotnet-core/3.0

### How to use it:

Make sure you have Dokan installed as well as dotnetcore 3.0 runtime.
Compile the app from source. 
Then run SAFE.NetworkDrive.UI.exe or SAFE.NetworkDrive.Console.exe.

### Known problems:

- Renaming or moving the first folder on the drive will result in a `BSOD` (blue screen of death) from a `page_fault_in_nonpaged_area`.
This is likely a problem in the `dokan` driver. I've made them aware of this, but I'm not expecting it to be fixed anytime soon.

Beware that there might be BSOD:s in other cases as well.

### Prerequisites for development

- Visual Studio 2019 with dotnet core development workload.

### Supported Platforms

- Windows (x64)

### Required SDK/Tools
- Visual Studio 2019 Preview
- netcore SDK 3.0.100-preview3-010431

## Further Help

Get your developer related questions clarified on the [SAFE Dev Forum](https://forum.safedev.org/). If you're looking to share any other ideas or thoughts on the SAFE Network you can reach out on the [SAFE Network Forum](https://safenetforum.org/).


## Contribution

Copyrights are retained by their contributors. No copyright assignment is required to contribute to this project.


## License

Licensed under the General Public License (GPL), version 3 (LICENSE http://www.gnu.org/licenses/gpl-3.0.en.html).
