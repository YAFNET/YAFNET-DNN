![YAFLogo](https://raw.githubusercontent.com/YAFNET/YAFNET/master/yafsrc/YetAnotherForum.NET/wwwroot/images/Logos/YAFLogo.svg)

**YetAnotherForum.NET** (YAF.NET) ASP.NET Open Source Forum solution! The **YAF.NET** project is an international collaboration of like-minded, skilled, and creative individuals who are striving to make **YAF.NET** the most robust and malleable forum solutions available.

[![Build](https://img.shields.io/github/actions/workflow/status/YAFNET/YAFNET-DNN/release.yml?branch=master&label=build)](https://github.com/YAFNET/YAFNET-DNN/actions/workflows/release.yml)
[![YAF.NET Version](https://img.shields.io/github/v/release/YAFNET/YAFNET?label=YAF.NET)](https://github.com/YAFNET/YAFNET-DNN/releases/latest)
[![DNN Platform](https://img.shields.io/badge/dynamic/xml?url=https%3A%2F%2Fraw.githubusercontent.com%2FYAFNET%2FYAFNET-DNN%2Fmaster%2Fyaf_dnn%2FYAF.DNN.Module.csproj&query=%2F%2F*%5Blocal-name()%3D%27PackageReference%27%5D%5B%40Include%3D%27DotNetNuke.Core%27%5D%2F*%5Blocal-name()%3D%27Version%27%5D&label=DNN%20Platform&color=0d6efd)](https://github.com/dnnsoftware/Dnn.Platform)
[![license](https://img.shields.io/github/license/yafnet/yafnet)](LICENSE.md)
[![ko-fi](https://img.shields.io/badge/Ko--fi-support-FF5E5B?logo=ko-fi&logoColor=white)](https://ko-fi.com/w8tcha)

### Features
[Full Feature List](https://github.com/YAFNET/YAFNET/wiki/YAF.NET-Features).

## DNN® (DotNetNuke) Module
This is the DNN Module Version of YetAnotherForum.NET which runs YAF inside a Module (DotNetNuke 10.04.01 or higher).

### Screen Shots

![mainscreen](https://raw.githubusercontent.com/YAFNET/YAFNET/master/yafsrc/YetAnotherForum.NET/wwwroot/assets/main.webp)

![forumsscreen](https://raw.githubusercontent.com/YAFNET/YAFNET/master/yafsrc/YetAnotherForum.NET/wwwroot/assets/forum.webp)

![topicsscreen](https://raw.githubusercontent.com/YAFNET/YAFNET/master/yafsrc/YetAnotherForum.NET/wwwroot/assets/topic.webp)

Admin Control Panel
![adminpanel](https://raw.githubusercontent.com/YAFNET/YAFNET/master/yafsrc/YetAnotherForum.NET/wwwroot/assets/admin.webp)

There is also a Second Child Module the *YAF.NET Forums What's New* Module which shows The Latest Posts in a List
![whatsnew](http://www.watchersnet.de/Portals/0/screenshots/dnn/ScreenshotYafLatestPosts.jpg)

### Getting Started with Development

This project is dependent upon the parent solution, YAFNET.  This requires that the steps you follow be specific.

1. Create a local directory for your project o live, such as C:\dev\YAFDev\ (just an example path).
2. Fork this YAFNET-DNN project into your account and then clone it into the local folder you just created. There should now be a C:\dev\YAFDev\yaf_dnn\ folder, as well as a README and other Git files.
3. (Optional) Attach an upstream to this YAFNET-DNN project in Git.
4. The `netfx` branch of [the YAFNET project](https://github.com/YAFNET/YAFNET) is wired in as the `YAFNET` git submodule. From a Windows PowerShell prompt in the repo root, run `.\init-yafsrc.ps1`. This initializes the submodule and creates an NTFS junction `yafsrc\` -> `YAFNET\yafsrc\`, so the resulting path is still C:\dev\YAFDev\yafsrc\ as before.
5. Open and then build the YAFNET solution.
6. Open and then build the YAFNET-DNN solution.

To later update to a newer commit of the YAFNET netfx branch, run `git submodule update --remote YAFNET` (the `yafsrc` junction does not need to be recreated).

Congratulations! You're now ready to begin development.

### Support
If you have any questions, please visit the YAF Community Support forum: [https://forum.yetanotherforum.net](https://forum.yetanotherforum.net), or visit the Wiki for More Informations.


## License

Yet Another Forum.NET is licensed under the Apache 2.0 license. 