🎨 XYZRenderLauncher

A sleek Windows Forms launcher for batch rendering with Blender, featuring GPU selection and Telegram notifications. Perfect for speeding up your renders with minimal hassle.

⚡ Features

Launch Blender renders directly from a clean UI
Choose CPU, CUDA, or OPTIX devices
Set frame ranges, output folders, and render settings easily
Telegram notifications for render completion
Lightweight and simple — no bloat

🛠 Prerequisites

.NET SDK ≥ 8 or Visual Studio with Desktop .NET workload
Blender installed (set path in app)
Windows PowerShell (for CLI commands)

🏗 Build with Visual Studio

Open XYZRenderLauncher.sln
Set Configuration = Release and Platform = x64
Make sure Resources/logo.ico exists and LICENSE is in project root
Build → Rebuild Solution


PowerShell command for build

dotnet publish .\XYZRenderLauncher.csproj -c Release -r win-x64 --self-contained false -o .\publish

dotnet publish .\XYZRenderLauncher.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\publish_single

▶️ Running the App

Unzip and launch XYZRenderLauncher.exe
Set Blender executable path
Configure start/end frames, output folder, and device
Hit Render and watch the magic happen
(Optional) enter Telegram bot token & chat ID for notifications



✉️ Contact

thisxyzvisual@gmail.com
