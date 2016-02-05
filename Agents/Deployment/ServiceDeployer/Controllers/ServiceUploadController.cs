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
            HttpResponseMessage res;

            if(requestHasZip(req))
            {
                string zippedUnzipPath = await extractZippedService(req);
                Service service = loadService(zippedUnzipPath);

                /*
                    The deployment script is responsible for:
                        1. Connecting to the cluster
                        2. Copying the application to the cluster's image store
                        3. Test and register the application type with the cluster
                        4. Clean up any existing application data
                        5. Create an instance of the application type
                */
                res = deployService(service);
            }
            else
            {
                res = new HttpResponseMessage(HttpStatusCode.BadRequest);
                res.ReasonPhrase = "No file was provided";
            }
            
            return res;
        }

        private HttpResponseMessage deployService(Service service)
        {
            HttpResponseMessage res;
            using (Runspace runspace = RunspaceFactory.CreateRunspace())
            {
                // Prepare Powershell enviroment
                runspace.Open();
                PowerShell ps = PowerShell.Create();
                ps.Runspace = runspace;

                // Run deployment script
                ps.AddScript($@".\{DEPLOY_SCRIPT_PATH} -verbose -appPackagePath '{service.AppPackagePath}' -appName '{service.AppName}' -appType '{service.AppType}' -appTypeVersion '{service.AppTypeVersion}' -appImageStoreName '{service.AppImageStoreName}'");
                bool success;
                string result = invokeDeploymentScript(ps, out success);

                // Set appropriate status code
                if (!success)
                {
                    res = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
                else
                {
                    res = new HttpResponseMessage(HttpStatusCode.OK);
                }
                res.ReasonPhrase = result;
            }

            return res;
        }

        private static Service loadService(string zippedUnzipPath)
        {
            // Recursively search the FS for the application manifest file
            var appManifestFilesSearchResults = Directory.GetFiles(zippedUnzipPath, "ApplicationManifest.xml", SearchOption.AllDirectories);
            var appManifestFilePath = appManifestFilesSearchResults.Single();

            ApplicationManifest appManifest = getAppManifest(appManifestFilePath);

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
            return service;
        }

        private static ApplicationManifest getAppManifest(string appManifestFilePath)
        {
            // Load the application manifest and find the required information
            XmlSerializer serialiser = new XmlSerializer(typeof(ApplicationManifest));
            FileStream filestream = new FileStream(appManifestFilePath, FileMode.Open);
            var appManifest = (ApplicationManifest)serialiser.Deserialize(filestream);
            return appManifest;
        }

        private static async Task<string> extractZippedService(HttpRequest req)
        {
            IFormFile zippedFile = req.Form.Files.First();

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

            return zippedUnzipPath;
        }

        private bool requestHasZip(HttpRequest req)
        {
            bool isValid = true;

            if (req == null)
            {
                isValid = false;
            }

            if (req.Form.Files.First() == null)
            {
                isValid = false;
            }

            return isValid;
        }

        private string invokeDeploymentScript(PowerShell ps, out bool success)
        {
            StringBuilder scriptOutput = new StringBuilder();
            success = false;

            // Invoke the deployment script, compile ouput and return to caller
            // If an exception or an error is returned, success will be set to false
            // All output from script should be verbose

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
