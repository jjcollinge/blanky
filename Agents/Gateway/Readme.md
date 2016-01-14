Setup
================



- Install Service Fabric runtime, SDK and tools - 1.4.87: https://azure.microsoft.com/en-us/documentation/articles/service-fabric-get-started/
- Launch 'Developer Command Prompt for VS2015' as admin and upgrade DNVM by running: https://github.com/aspnet/home#cmd
- In the command prompt, run dnvm install 1.0.0-rc2-16357 -a x86 -u.
- In the command prompt, run dnvm install 1.0.0-rc2-16357 -a x64 -u.
- Clone the repo and open the solution in Visual Studio running as admin.
- In Visual Studio, go to Options -> NuGet Package Manager -> Package Sources, and add a new package source: https://www.myget.org/F/aspnetvnext/api/v3/index.json.
- After all the packages are restored, F5 to run the app.