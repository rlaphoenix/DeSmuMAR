# DeSmuMAR

DeSmuME Aspect Ratio is an automated aspect ratio based resizing method for DeSmuME for playing in specific Aspect Ratio's.

## Installation

Notes:

1. `DeSmuMAR.exe` must always be next to `DeSmuME.exe`.
2. DeSmuMAR gives you the option to install a required nightly version automatically if not found. See Installation steps below.

### Requirements

- [.NET Core 3.1 Runtime]
- [DeSmuME] Nightly. v0.9.11 and older is unsupported. The download page has recommendations for each OS.

### Setup

1. Download the [Latest Release] and extract the downloaded files next to `DeSmuME.exe`. If you don't already have DeSmuME, then extract to the location you wish `DeSmuME.exe` to be located and DeSmuMAR will give you the option to download the latest nightly release.

  [.NET Core 3.1 Runtime]: <https://dotnet.microsoft.com/download/dotnet/3.1>
  [DeSmuME]: <https://desmume.org/download>
  [Latest Release]: <https://github.com/rlaPHOENiX/DeSmuMAR/releases/latest/download/DeSmuMAR.zip>

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
