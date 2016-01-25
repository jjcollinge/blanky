What it does
=================

The gateway service resolves the endpoints of services inside 
Service Fabric and, when possible, issues a redirect to the calling 
client or, when not possible, proxys the traffic. 

Issuing redirects is preferable as it means that the gateway service
doesn't become a bottleneck. 

The gateway runs on all nodes in the clusters. This enables 
normal apps with no service fabric awareness to makes simple
http calls like http://localhost/route/someapp/someservice and 
have these resolved to the node hosting that service. 

This opens the door to using not just C# to write the Microservices. 



Setup
================



- Install Service Fabric runtime, SDK and tools - 1.4.87: https://azure.microsoft.com/en-us/documentation/articles/service-fabric-get-started/
- Launch 'Developer Command Prompt for VS2015' as admin and upgrade DNVM by running: https://github.com/aspnet/home#cmd
- In the command prompt, run dnvm install 1.0.0-rc2-16357 -a x86 -u.
- In the command prompt, run dnvm install 1.0.0-rc2-16357 -a x64 -u.
- Clone the repo and open the solution in Visual Studio running as admin.
- In Visual Studio, go to Options -> NuGet Package Manager -> Package Sources, and add a new package source: https://www.myget.org/F/aspnetvnext/api/v3/index.json.
- After all the packages are restored, F5 to run the app.


Also add nuget feed for xunit
https://www.myget.org/F/xunit/

How it works
=====================

In service router the 'launchSettings.json' sets the dnx version we want to run 
and also sets the 'ASPNET_ENV' to debug. 

When started the app runs the 'ServiceRouter' task from the 'project.json' file
which starts the 'Main' method in 'Program.cs'. 

This method either initialises the 'ServiceRouter' service or, when the env variable 
is set to debug, starts the web app indepenantly of Service Fabric. This allows
it to start outside of a full app deploy.  

To handle any Fabric specific calls it uses DI. When started with 'Debug' environment
 variable ASPNet5 runs the 'startup.cs' and instead of the normal service
 configuration calls the 'ConfigureDebugServices'
method. 

This configures the ServiceFabric client, passed into through depenancy injection, 
to connect as an external client allowing debugging without a full deployment cycle. 

