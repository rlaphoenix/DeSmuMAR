# DeSmuMAR

[![.NET version tests](https://img.shields.io/github/actions/workflow/status/rlaphoenix/desmumar/build.yml?branch=master)](https://github.com/rlaphoenix/desmumar/releases)

DeSmuME Aspect Ratio is an automated aspect ratio based resizing method for DeSmuME for playing in specific Aspect Ratio's.

Supports DeSmuME v0.9.12+. May support v0.9.11 if compiled around 2020 onwards, and not the original 2015 release.

## Installation

1. Download and install the [.NET 6.0 Runtime] if not yet installed.
2. Download [DeSmuME] v0.9.12 or newer and extract the zip files.
   (If the exe filename is e.g., `DeSmuME_0.9.13_x64.exe`, rename it to `DeSmuME.exe`)
3. Download [DeSmuMAR] and extract the files into the same folder as DeSmuME.
   
  [.NET 6.0 Runtime]: <https://dotnet.microsoft.com/download/dotnet/6.0>
  [DeSmuME]: <https://desmume.org/download>
  [DeSmuMAR]: <https://github.com/rlaphoenix/DeSmuMAR/releases/latest/download/DeSmuMAR.zip>

## Usage

It's intended to be used as a wrapper. Use `DeSmuMAR.exe` exactly as you would `DeSmuME.exe`; feel free to use it as the `.nds` file extension association, as DeSmuME shortcuts, rename that shortcut to anything (e.g. `DeSmuME`), etc.

DeSmuMAR will only ask you the configuration you wish to use at first launch. If you wish to make changes, then you will need to manually edit the `DeSmuMAR.ini` settings file next to `DeSmuMAR.exe`. You may also delete the settings file and re-launch for it to re-ask you.

All DeSmuMAR actually does is take into consideration the chosen aspect ratio, resolution (as height) and DeSmuME's current display size and resizes the window accordingly. Sadly DeSmuME has no real support for setting an aspect ratio.

## Credits

The Icon file [DeSmuME.ico] is owned by [@TASVideos] of the [DeSmuME] project and is used to feel more like a Wrapper of DeSmuME when used as a shortcut or file type association.

  [DeSmuME.ico]: <DeSmuME.ico>
  [DeSmuME]: <https://github.com/TASVideos/DeSmuME>
  [@TASVideos]: <https://github.com/TASVideos>

## License

This project uses the GNU General Public License v3.0 (GPLv3). By using this project in any way you agree you the terms of the License.
You can view the license on [Choose-a-License] (for an overview and explanation) or the [LICENSE] file included with this project.

  [Choose-a-License]: <https://choosealicense.com/licenses/gpl-3.0/>
  [LICENSE]: <LICENSE>
