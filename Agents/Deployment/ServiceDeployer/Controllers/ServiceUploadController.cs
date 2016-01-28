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
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Collections.ObjectModel;

namespace ServiceDeployer.Controllers
{
    [Route("api/[controller]")]
    public class ServiceUploadController : Controller
    {
        // Define constants
        private const string ROOT_DIR = @"..\work\";
        private const string DEPLOY_SCRIPT_PATH = @"approot\src\ServiceDeployer\deploymentScript.ps1";

        private class Service
        {
            public string AppPackagePath { get; set; }
            public string AppName { get; set; }
            public string AppType { get; set; }
            public string AppTypeVersion { get; set; }
            public string AppImageStoreName { get; set; }
        }

        private class DeployResponse
        {
            public DeployResponse(HttpStatusCode code, string msg)
            {
                this.StatusCode = code;
                this.message = msg;
            }
            public HttpStatusCode StatusCode { get; set; }
            public string message { get; set; }
        }

        // GET: api/ServiceUpload
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // POST api/ServiceUpload
        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            HttpRequest req = this.Request;

            if (req == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            IFormFile zippedFile = req.Form.Files.First();
            
            if(zippedFile == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            // Save the compressed zip to the local machine
            var zipGuid = Guid.NewGuid().ToString();
            var zippedFilePath = ROOT_DIR + "\\" + zipGuid + ".zip";
            var zippedUnzipPath = ROOT_DIR + "\\" + zipGuid + "\\";

            await zippedFile.SaveAsAsync(zippedFilePath);

            // Unzip the archive to inflated folder
            using (ZipStorer zip = ZipStorer.Open(zippedFilePath, FileAccess.Read))
            {
                var dir = zip.ReadCentralDir();

                foreach (var entry in dir)
                {
                    zip.ExtractFile(entry, zippedUnzipPath + entry.FilenameInZip);
                }

                zip.Close();
            }

            // Recursively search the FS for the application manifest file
            var appManifestFilesSearchResults = Directory.GetFiles(zippedUnzipPath, "ApplicationManifest.xml", SearchOption.AllDirectories);
            var appManifestFilePath = appManifestFilesSearchResults.Single();

            // Load the application manifest and find the required information
            XmlSerializer serialiser = new XmlSerializer(typeof(ApplicationManifest));
            FileStream filestream = new FileStream(appManifestFilePath, FileMode.Open);
            var appManifest = (ApplicationManifest)serialiser.Deserialize(filestream);

            var appName = $"fabric:/{appManifest.ApplicationTypeName}_{appManifest.ApplicationTypeVersion}";
            var appPackagePath = Directory.GetParent(appManifestFilePath).FullName;

            Service service = new Service
            {
                AppName = appName,
                AppPackagePath = appPackagePath,
                AppImageStoreName = $"Store\\{appManifest.ApplicationTypeName}",
                AppType = appManifest.ApplicationTypeName,
                AppTypeVersion = appManifest.ApplicationTypeVersion
            };

            /*
                The deployment script is responsible for:
                    1. Connecting to the cluster
                    2. Copying the application to the cluster's image store
                    3. Test and register the application type with the cluster
                    4. Clean up any existing application data
                    5. Create an instance of the application type
            */
            var response = deployService(service);

            return new HttpResponseMessage {
                StatusCode = response.StatusCode,
                ReasonPhrase = response.message
            };
        }    

        private DeployResponse deployService(Service service)
        {
            DeployResponse response;
            using (Runspace runspace = RunspaceFactory.CreateRunspace())
            {
                // Prepare Powershell enviroment
                runspace.Open();
                PowerShell ps = PowerShell.Create();
                ps.Runspace = runspace;

                // Run script - this is very blackbox and won't handle ps exceptions
                ps.AddScript($@".\{DEPLOY_SCRIPT_PATH} -verbose -appPackagePath '{service.AppPackagePath}' -appName '{service.AppName}' -appType '{service.AppType}' -appTypeVersion '{service.AppTypeVersion}' -appImageStoreName '{service.AppImageStoreName}'");
                bool success;
                string result = invokeDeploymentScript(ps, out success);

                if (!success)
                {
                    response = new DeployResponse(HttpStatusCode.InternalServerError, result);
                }
                else
                {
                    response = new DeployResponse(HttpStatusCode.OK, result);
                }
            }

            // Do some error checking
            return response;
        }

        private string invokeDeploymentScript(PowerShell ps, out bool success)
        {
            StringBuilder scriptOutput = new StringBuilder();
            success = false;

            // Invoke the deployment script, compile ouput and return to caller

            try
            {
                ps.Invoke();
            }
            catch (Exception e)
            {
                scriptOutput.AppendLine($"EXCEPTION: {e.Message}");
                if (success == true) success = false;
            }

            foreach(var err in ps.Streams.Error)
            {
                scriptOutput.AppendLine($"ERROR: {err.ToString()}");
                if (success == true) success = false;
            } 

            foreach (var line in ps.Streams.Verbose)
            {
                scriptOutput.AppendLine($"VERBOSE: {line.ToString()}");
                if (success != true) success = true;
            }

            // Assume that if no exception has thrown that the ps script ran successfully.
            // Worth adding some error checking on scriptOutput
            return scriptOutput.ToString();
        }
    }
}
