*Disclaimer
Blanky was an exploratative venture to build a developer friendly platform ontop of Microsoft's Service Fabric. Our development on Blanky is being suspended until Service Fabric becomes generally available, at which point we will review the value of such a platform. Please feel free to submit Pull Requests as I will still consider merging in new content.

Blanky
---------------

Blanky is a collection of additional services and tooling which enrich the developer experience of Microsoft's Service Fabric product. By offering prebaked services which handle common use cases such as health reporting, api stats, etc. the developer is free to only concern themselves with their own value proposition. Additionally, we have created tooling which allows developers to create and deploy services into the Service Fabric cluster from any platform. There are a numerous more opportunities for us to continue to super charge the capabilities and useability of Service Fabric to make it a more competetive product.

<b>Note:</b>
In order to restore the required Nuget packages you will have to add the Nuget Package Source for aspnetvnext which can be found at https://www.myget.org/gallery/aspnetvnext

Resources
---------------

####Node Microservice Generator
Generate a standard microservice node server template using yeoman.<br />
https://www.npmjs.com/package/generator-blanky

####Visual Studio Code Deployment Extension
Deploy your Service Fabric package directly from VSCode using this handy extension.<br />
https://marketplace.visualstudio.com/items?itemName=jcollinge.blanky

Principles
---------------

- Make using service fabric services simple
- Nuget based install (overlays existing templates rather than rewrite - like polyfill in js)
- Highlight cool but unapproachable SF features (naming service, named service instances, watchdogs etc)
- Extend the service fabric REST API to add more features (deployment)
- Provide a best practice template which adds rich features like swagger to each of your services

Architecture
------------

```
   +----------------------------------------------------------------------------------------+---------------------------------------------------------------+
   |                                                                                        |                                                               |
   |                                                                                        |                                                               |
   |                                                                                        |                                                               |
+--+---+                                             +--------------------------------------+-----------------------------------------+                  +--+---+
| Node |          +-------------------+              |                                                                                |                  | Node |
| n-1  |          | Blanky Cluster    | Deploys      |                                                                  Node          |                  | n-1  |
+------+          | Bootstrap script  +------------+-------> Agents                                                                   |                  +------+
                  +-+-----------------+            | |                                                                                |
                    ^                              | |                                                                                |
                    | Invokes                      | |    +---------------+            +-----------------------+                      |
                    |                              | |    | Deployment    +----------->| Service Fabric Client |                      |
                  +-+----------------+  Uses       | |    +---------------+            +------------+----------+                      |
                  | Client           +-------------+ |                                              ^                                 |
                  +------------------+               |    +---------------+                         |                                 |
                                                     |    | Stats         |     +-------------------+                                 |
                                                     |    +---------------+     |                                                     |
                                                     |                          |                                                     |
                                                     |    +---------------+     |      +-----------------------+                      |
                                                     |    | Gateway       +-----+----->| MyMicroservice        |                      |
                                                     |    +---------------+            +-----------------------+                      |
                                                     |                                                                                |
                                                     |    +---------------+            +-----------------------+  X                   |
                                                     |    | Watchdog      +----------->| Health API            |  XX                  |
                                                     |    +---------------+            +-----------------------+   X                  |
                                                     |                                                             X--- Template      |
                                                     |    +---------------+            +-----------------------+   X                  |
                                                     |    | Auth          |            | Swagger API           |  XX                  |
                                                     |    +---------------+            +-------------+---------+  X                   |
                                                     |                                               ^                                |
                                                     |    +---------------+                          |                                |
                                                     |    | API Catalogue +--------------------------+                                |
                                                     |    +---------------+                                                           |
                                                     |                                                                                |
                                                     |                                                                                |
                                                     |                                                                                |
                                                     +--------------------------------------------------------------------------------+

```
