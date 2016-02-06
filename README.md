Blanky
---------------

Enrich Azure Service Fabric for developers

Resources
---------------
- Node microservice generator: https://www.npmjs.com/package/generator-blanky
- Visual Studio Code Deployment Extension: https://marketplace.visualstudio.com/items?itemName=jcollinge.blanky

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
                    |                              | |    | Deployment    +----------->+Service Fabric Client  |                      |
                  +-+----------------+  Uses       | |    +---------------+            +------------+----------+                      |
                  | Client           +-------------+ |                                              ^                                 |
                  +------------------+               |    +---------------+                         |                                 |
                                                     |    | Stats         |     +-------------------+                                 |
                                                     |    +---------------+     |                                                     |
                                                     |                          |                                                     |
                                                     |    +---------------+     |      +-----------------------+                      |
                                                     |    | Gateway       +-----+----->+MyMicroservice         |                      |
                                                     |    +---------------+            +-----------------------+                      |
                                                     |                                                                                |
                                                     |    +---------------+            +-----------------------+  X                   |
                                                     |    | Watchdog      +----------->+Health API             |  XX                  |
                                                     |    +---------------+            +-----------------------+   X                  |
                                                     |                                                             X--- Template      |
                                                     |    +---------------+            +-----------------------+   X                  |
                                                     |    | Auth          |            |Swagger API            |  XX                  |
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
