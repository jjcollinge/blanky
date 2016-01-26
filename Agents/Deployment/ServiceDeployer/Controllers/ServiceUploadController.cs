using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Http;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Management.Automation;
using System.Text;
using System.Management.Automation.Runspaces;

namespace ServiceDeployer.Controllers
{
    [Route("api/[controller]")]
    public class ServiceUploadController : Controller
    {
        private const string ROOT_DIR = @"C:\temp\";
        private const string DEPLOY_SCRIPT_PATH = @"\scripts\Deploy.ps1";
        private const string LOCAL_ZIP_NAME = "latest_deployment.zip";
        private const string SERVICE_FABRIC_SDK_PSMODULE_PATH = @"C:\Program Files\Microsoft SDKs\Service Fabric\Tools\PSModule\ServiceFabricSDK\ServiceFabricSDK.psm1";
        private const string SERVICE_FABRIC_PSMODULE_PATH = @"C:\WINDOWS\system32\WindowsPowerShell\v1.0\Modules\ServiceFabric\ServiceFabric.psd1";

        // GET: api/ServiceUploadController
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // POST api/ServiceUploadController
        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            HttpRequest req = this.Request;

            if (req == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            IFormFile file = req.Form.Files.First();
            
            if(file == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            /*
                Folder name should include version information
            */

            // Save the compressed zip to the local machine
            var filePath = ROOT_DIR + LOCAL_ZIP_NAME;
            await file.SaveAsAsync(filePath);

            // Unzip the archive to inflated folder
            using (ZipStorer zip = ZipStorer.Open(filePath, FileAccess.Read))
            {
                var dir = zip.ReadCentralDir();

                foreach(var entry in dir)
                {
                    zip.ExtractFile(entry, ROOT_DIR + entry.FilenameInZip);
                }

                zip.Close();
            }

            /*
                The deployment script is responsible for:
                    1. Connecting to the cluster
                    2. Copying the application to the cluster's image store
                    3. Test and register the application type with the cluster
                    4. Clean up any existing application data
                    5. Create an instance of the application type
            */
            var deploymentScriptPath = ROOT_DIR + DEPLOY_SCRIPT_PATH;
            if(RunScript(deploymentScriptPath))
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

        }    

        private bool psInvoke(PowerShell ps)
        {
            try
            {
                ps.Invoke();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        private bool RunScript(string scriptFilePath)
        {
            bool success;
            using (Runspace runspace = RunspaceFactory.CreateRunspace())
            {
                // Prepare Powershell enviroment
                runspace.Open();
                PowerShell ps = PowerShell.Create();
                ps.Runspace = runspace;
                ps.Commands.AddCommand("Set-ExecutionPolicy")
                    .AddParameter("ExecutionPolicy", "Unrestricted")
                    .AddParameter("Scope", "CurrentUser");
                success = psInvoke(ps);

                // Make sure required modules are available
                ps.Commands.AddCommand("Import-Module")
                    .AddArgument(SERVICE_FABRIC_PSMODULE_PATH);
                success = psInvoke(ps);

                ps.Commands.AddCommand("Import-Module")
                    .AddArgument(SERVICE_FABRIC_SDK_PSMODULE_PATH);
                success = psInvoke(ps);

                // Run script
                ps.AddScript(@".\" + scriptFilePath);
                success = psInvoke(ps);
            }

            // Do some error checking
            return success;
        }
    }
}
