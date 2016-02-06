// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode'; 
var fs = require('fs');
var path = require('path');
var request = require('request');

// Using bauer-zip npm package for creating the zip file (https://www.npmjs.com/package/bauer-zip)
var z = require('bauer-zip');


// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {

	// Use the console to output diagnostic information (console.log) and errors (console.error)
	// This line of code will only be executed once when your extension is activated
	console.log('Congratulations, your extension "blanky" is now active!'); 

    let blanky = new Blanky();

	// The command has been defined in the package.json file
	// Now provide the implementation of the command with  registerCommand
	// The commandId parameter must match the command field in package.json
	var disposable = vscode.commands.registerCommand('extension.blankyDeploy', () => {
		// The code you place here will be executed every time your command is executed

        blanky.Deploy();
                        
	});
	context.subscriptions.push(blanky);
	context.subscriptions.push(disposable);
}


class Blanky {
    private _folderToZip: string = vscode.workspace.rootPath;
    private _zipFilename: string = '.blanky.zip';
    private _zipFilepath: string; 
    private _keepZip: boolean = false;
    private _deploymentEndpoint: string;
    private _zipExists: boolean = false;
    private _simulateDeploy: boolean = false;

    
    public constructor() {
        this._zipFilepath = path.join(vscode.workspace.rootPath, this._zipFilename);        
    }
    
    public Deploy()
    {
        vscode.window.showInformationMessage("Getting things started, checking some stuff locally");
        
        this.ValidateWorkspace();
        // this.GetConfigSettings();
        // this.TryRemoveZipFile();        
        // this.CreateZipFile();
        // this.InvokeDeploymentService();
        // this.Cleanup();
    }   
    
    // Validate that we have a microservice project   
    private ValidateWorkspace() {
        
        var that = this;
        
        // Check we have a manifest file
        var manifestPath: string = path.join(vscode.workspace.rootPath, 'ApplicationManifest.xml');
        
        if (!fs.existsSync(manifestPath)) {
            vscode.window.showErrorMessage('Not a valid project: ApplicationManifest.xml file is missing.');
            console.error('ApplicationManifest file is missing.');
        } else {
            vscode.workspace.findFiles("**/ServiceManifest.xml", null).then(function (results) {
                if (results.length===0) {
                    vscode.window.showErrorMessage('Not a valid project: ServiceManifest.xml file is missing.');
                    console.error('ServiceManifest file is missing.');
                } else {
                    that.GetConfigSettings();
                    that.TryRemoveZipFile();        
                    that.CreateZipFile();
                }
            });
        }                
    }
    
    // Zipping up all files in the current folder        
    private CreateZipFile() {
        var that = this;
        
        z.zip(this._folderToZip, this._zipFilepath, function (err){
            if(err)
            {
                console.log('Zip file creation failed');
                console.log(err);
                throw err;
            }
            else
            {
                console.log('Zip file created succesfully');
                that.InvokeDeploymentService();
            }
        });
        
    } 
    
    private TryRemoveZipFile() {
        if (fs.existsSync(this._zipFilepath)) {
           console.log ('Cleaning up existing zip file.');
           fs.unlinkSync(this._zipFilepath);
        };
    }
    
    private GetConfigSettings() {
        let config = vscode.workspace.getConfiguration("blanky");             

        this._keepZip = config.get("keepzip") as boolean;        
        this._simulateDeploy = config.get("simulatedeploy") as boolean;
        
        console.log ('Keepzip config setting: ' + this._keepZip);
        console.log ('SimulateDeploy config setting: ' + this._simulateDeploy);
        
        let endpoint = config.get("deploymentendpoint") as string;
        
        if(!endpoint) {
            console.log ('Deployment endpoint not specified in config.');
            vscode.window.showErrorMessage("You have no Blanky deployment endpoint specified.");
            throw "You have no Blanky deployment endpoint specified.";
        } else {
            this._deploymentEndpoint = endpoint;
            console.log ('Deployment endpoint config setting: ' + endpoint);
        }   
    }
    
    // Invoke the Blanky deployment endpoint to deploy the micro service
    private InvokeDeploymentService() {
        if (!this._simulateDeploy) {            
            var that = this;
                    
            console.log('Starting deployment to ' + this._deploymentEndpoint);
            vscode.window.showInformationMessage("Deploying to " + this._deploymentEndpoint);
            
            // Invoke the deployment endpoint rest service
            var req = request({
                uri: this._deploymentEndpoint,
                method: "POST",
                timeout: 900000},
                function (err, resp, body) {
                    if (err) {
                        console.log(err);
                        vscode.window.showErrorMessage('Deploying to Blanky failed:' + err);
                    } else {
                        console.log('Server response: ' + body);
                        vscode.window.showInformationMessage('Deploying to Blanky completed succesfully!');
                    }

                    that.Cleanup();
                    
                    console.log('Blanky deployment completed');
                }
                );

            var form = req.form();
            form.append('file', fs.createReadStream(this._zipFilepath));        
        } else {
            console.log('Simulating deployment to ' + this._deploymentEndpoint);
            that.Cleanup();

            vscode.window.showInformationMessage('Deploying to Blanky completed succesfully!');

            console.log('Blanky deployment completed');
        }
    }
    
    private Cleanup() {
        // Remove zipfile
        if(!this._keepZip){
            this.TryRemoveZipFile();
        }     
    }
    
    dispose() {
    }
}

// this method is called when your extension is deactivated
export function deactivate() {
}