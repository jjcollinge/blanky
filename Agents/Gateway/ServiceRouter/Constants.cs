using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRouter
{
    public class Constants
    {
        public const string SESSION_KEY_SERVICE_ENDPOINT = "resolvedEndpoint";
        public const string HelpText = @"
                    
BBBBBBBBBBBBBBBBB   lllllll                                    kkkkkkkk                                    
B::::::::::::::::B  l:::::l                                    k::::::k                                    
B::::::BBBBBB:::::B l:::::l                                    k::::::k                                    
BB:::::B     B:::::Bl:::::l                                    k::::::k                                    
  B::::B     B:::::B l::::l   aaaaaaaaaaaaa  nnnn  nnnnnnnn     k:::::k    kkkkkkkyyyyyyy           yyyyyyy
  B::::B     B:::::B l::::l   a::::::::::::a n:::nn::::::::nn   k:::::k   k:::::k  y:::::y         y:::::y 
  B::::BBBBBB:::::B  l::::l   aaaaaaaaa:::::an::::::::::::::nn  k:::::k  k:::::k    y:::::y       y:::::y  
  B:::::::::::::BB   l::::l            a::::ann:::::::::::::::n k:::::k k:::::k      y:::::y     y:::::y   
  B::::BBBBBB:::::B  l::::l     aaaaaaa:::::a  n:::::nnnn:::::n k::::::k:::::k        y:::::y   y:::::y    
  B::::B     B:::::B l::::l   aa::::::::::::a  n::::n    n::::n k:::::::::::k          y:::::y y:::::y     
  B::::B     B:::::B l::::l  a::::aaaa::::::a  n::::n    n::::n k:::::::::::k           y:::::y:::::y      
  B::::B     B:::::B l::::l a::::a    a:::::a  n::::n    n::::n k::::::k:::::k           y:::::::::y       
BB:::::BBBBBB::::::Bl::::::la::::a    a:::::a  n::::n    n::::nk::::::k k:::::k           y:::::::y        
B:::::::::::::::::B l::::::la:::::aaaa::::::a  n::::n    n::::nk::::::k  k:::::k           y:::::y         
B::::::::::::::::B  l::::::l a::::::::::aa:::a n::::n    n::::nk::::::k   k:::::k         y:::::y          
BBBBBBBBBBBBBBBBB   llllllll  aaaaaaaaaa  aaaa nnnnnn    nnnnnnkkkkkkkk    kkkkkkk       y:::::y           
                                                                                        y:::::y            
                                                                                       y:::::y             
                                                                                      y:::::y              
                                                                                     y:::::y               
                                                                                    yyyyyyy                
                                                                                                                                                                                                            

  __  __       _    _                _____                 _            ______    _          _      
 |  \/  |     | |  (_)              / ____|               (_)          |  ____|  | |        (_)     
 | \  / | __ _| | ___ _ __   __ _  | (___   ___ _ ____   ___  ___ ___  | |__ __ _| |__  _ __ _  ___ 
 | |\/| |/ _` | |/ / | '_ \ / _` |  \___ \ / _ \ '__\ \ / / |/ __/ _ \ |  __/ _` | '_ \| '__| |/ __|
 | |  | | (_| |   <| | | | | (_| |  ____) |  __/ |   \ V /| | (_|  __/ | | | (_| | |_) | |  | | (__ 
 |_|  |_|\__,_|_|\_\_|_| |_|\__, | |_____/ \___|_|    \_/ |_|\___\___| |_|_ \__,_|_.__/|_|  |_|\___|
                             __/ |              | |                | |   | | |                      
 __      ____ _ _ __ _ __ __|___/ __ _ _ __   __| |   ___ _   _  __| | __| | |_   _                 
 \ \ /\ / / _` | '__| '_ ` _ \   / _` | '_ \ / _` |  / __| | | |/ _` |/ _` | | | | |                
  \ V  V / (_| | |  | | | | | | | (_| | | | | (_| | | (__| |_| | (_| | (_| | | |_| |                
   \_/\_/ \__,_|_|  |_| |_| |_|  \__,_|_| |_|\__,_|  \___|\__,_|\__,_|\__,_|_|\__, |                
                                                                               __/ |                
                                                                              |___/                 
                    Help
                    ---------------     
    
                    List Operations:
                        - list/services
                        - list/endpoints
                    
                    N.B. To access a service endpoints externally from the cluster use 'RoutedEndpoint'
                         replace 'localhost:8283' with the External IP/Port and ensure
                         port forwarding is correctly configured on the load balancer for the service router. 

                    Route Operations:
                        - route/{ApplicationName}/{ServiceName}/{HttpMethod}
                    
                    N.B. Cluster routing reqeusts originating in a SF cluster will receive Redirect:307 
                         with the actual service endpoint to avoid bottle neck or throughput issues in the ServiceRouter
                    ";
    }
}
