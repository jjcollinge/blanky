// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode'; 
var fs = require('fs');
var path = require('path');
var request = require('request');
var progress = require('request-progress');

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
    private _outputChannel: vscode.OutputChannel = vscode.window.createOutputChannel("Blanky Deployment");
    private _zipFilename: string = '.blanky.zip';
    private _zipFilepath: string; 
    private _keepZip: boolean = false;
    private _deploymentEndpoint: string;
    private _zipExists: boolean = false;
    private _simulateDeploy: boolean = false;
    private _showVerboseLogging = true;

    
    public constructor() {
        this._zipFilepath = path.join(vscode.workspace.rootPath, this._zipFilename);        
    }
    
    public Deploy()
    {
        this._outputChannel.clear();
        this._outputChannel.show();
        this._outputChannel.appendLine("Getting things started, checking some stuff locally");
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
            this.ShowMessageInUI('Not a valid project: ApplicationManifest.xml file is missing.', UIMessageType.Error);
            console.error('ApplicationManifest file is missing.');
        } else {
            vscode.workspace.findFiles("**/ServiceManifest.xml", null).then(function (results) {
                if (results.length===0) {
                    this.ShowMessageInUI('Not a valid project: ServiceManifest.xml file is missing.', UIMessageType.Error);
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
        that.ShowMessageInUI('Creating Zip for upload', UIMessageType.Info);
        z.zip(this._folderToZip, this._zipFilepath, function (err){
            if(err)
            {
                console.log('Zip file creation failed');
                console.log(err);
                that.ShowMessageInUI('Zip file creation failed' + err, UIMessageType.Error);
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
           this.ShowMessageInUI('Cleaning up existing zip file.', UIMessageType.Verbose);
           fs.unlinkSync(this._zipFilepath);
        };
    }
    
    private GetConfigSettings() {
        let config = vscode.workspace.getConfiguration("blanky");             

        this._keepZip = config.get("keepzip") as boolean;        
        this._simulateDeploy = config.get("simulatedeploy") as boolean;
        
        this.ShowMessageInUI('Keepzip config setting: ' + this._keepZip, UIMessageType.Verbose);
        this.ShowMessageInUI('SimulateDeploy config setting: ' + this._simulateDeploy, UIMessageType.Verbose);
        
        let endpoint = config.get("deploymentendpoint") as string;
        
        if(!endpoint) {
            console.log ('Deployment endpoint not specified in config.');
            this.ShowMessageInUI("You have no Blanky deployment endpoint specified.", UIMessageType.Error);
            throw "You have no Blanky deployment endpoint specified.";
        } else {
            this._deploymentEndpoint = endpoint;
            this.ShowMessageInUI('Deployment endpoint config setting: ' + endpoint, UIMessageType.Verbose);
        }   
    }
    
    // Invoke the Blanky deployment endpoint to deploy the micro service
    private InvokeDeploymentService() {
        if (!this._simulateDeploy) {            
            var that = this;
                    
            console.log('Starting deployment to ' + this._deploymentEndpoint);
            this.ShowMessageInUI("Deploying to " + this._deploymentEndpoint);
            
            // Invoke the deployment endpoint rest service
            var formDataToSend = {
                deplymentZip: fs.createReadStream(this._zipFilepath)
            };
           
            // Attempted to use request-progress to show upload progress when deploying. https://www.npmjs.com/package/request-progress
            progress(request({
                    uri: this._deploymentEndpoint,
                    method: "POST",
                    timeout: 900000,
                    formData: formDataToSend,
                },
                function (err, resp, body) {
                    if (err) {
                        console.log(err);
                        that.ShowMessageInUI('Deploying to Blanky failed:' + err, UIMessageType.Error);
                    } else {
                        that.ShowMessageInUI('Server response: ' + body, UIMessageType.Info);
                        that.ShowMessageInUI('Deploying to Blanky completed');
                    }

                    that.Cleanup();
                    console.log('Blanky deployment completed');
                }), {
                throttle: 10,                    // Throttle the progress event to 2000ms, defaults to 1000ms 
                delay: 0
            })
            .on('progress', function (state) {
                that.ShowMessageInUI("Completed: " + state.percentage * 100 + " Estimated time remaining: " + state.time.remaining)
                if (state.percentage === 1)
                {
                    that.ShowMessageInUI("Upload completed in: " + state.time.elapsed)
                }
            });

            // var form = req.form();
            // form.append('file', fs.createReadStream(this._zipFilepath));        
        } else {
            console.log('Simulating deployment to ' + this._deploymentEndpoint);
            that.Cleanup();

            that.ShowMessageInUI('Deploying to Blanky completed succesfully!');

            console.log('Blanky deployment completed');
        }
    }
    
    private Cleanup() {
        // Remove zipfile
        if(!this._keepZip){
            this.TryRemoveZipFile();
        }     
    }
    
    private ShowMessageInUI(message: string, messageType?: UIMessageType)
    {
        //If message type not set default to information
        if (messageType === null){
            messageType = UIMessageType.Info;
        }
        
        //If the message is verbose and verbose logging is disabled ignore it. 
        if (messageType === UIMessageType.Verbose  && !this._showVerboseLogging){
            return;
        }
        
        if (messageType === UIMessageType.Error)
        {
            vscode.window.showErrorMessage(message);
            this._outputChannel.appendLine('[Error]::' + message);
            return;
        }
        
        this._outputChannel.appendLine(message);
    }
    

    
    dispose() {
    }
}

enum UIMessageType {
    Verbose = 0,
    Info = 2,
    Error = 4
}

// this method is called when your extension is deactivated
export function deactivate() {
}