'use strict';
var yeoman = require('yeoman-generator');
var chalk = require('chalk');
var yosay = require('yosay');
var util = require('util');
var Glob = require("glob").Glob;
var ncp = require('ncp').ncp;
var mkdirp = require('mkdirp');

module.exports = yeoman.generators.Base.extend({
    prompting: function () {
        var done = this.async();

        // Have Yeoman greet the user.
        this.log(yosay(
            'Ahoy there! welcome to ' + chalk.red('blanky') + ' generator!'
            ));

        var prompts = [
            {
                type: 'list',
                name: 'platformType',
                message: 'What type of application would you like to generator?',
                default: "node",
                choices: [{
                    name: "node"
                },
                    {
                        name: "c#"
                    },
                    {
                        name: "java"
                    },
                    {
                        name: "go"
                    }]
            },
            {
                type: 'input',
                name: 'pkgName',
                message: 'What is your app name?',
                default: "BlankyService"
            },
            {
                type: 'input',
                name: 'pkgVer',
                message: 'What is your app version?',
                default: "1.0.0"
            }];

        this.prompt(prompts, function (props) {
            this.props = props;
            done();
        }.bind(this));
    },

    writing: function () {
        var pkgName = this.props.pkgName;
        var pkgVer = this.props.pkgVer;

        console.log("Creating application " + pkgName + ":v" + pkgVer + "...");
        
        var that = this;
        
        mkdirp(this.destinationPath(pkgName), function (err) {

            mkdirp(that.destinationPath(pkgName + "/Code"), function (err) {

                mkdirp(that.destinationPath(pkgName + "/Config"), function (err) {
                    that.fs.copyTpl(
                        that.templatePath('node/ApplicationManifest.xml'),
                        that.destinationPath('ApplicationManifest.xml'),
                        {
                            Name: pkgName,
                            Version: pkgVer
                        }
                        );

                    that.fs.copyTpl(
                        that.templatePath('node/ServiceManifest.xml'),
                        that.destinationPath(pkgName + '/ServiceManifest.xml'),
                        {
                            Name: pkgName,
                            Version: pkgVer
                        }
                        );

                    ncp(that.templatePath("node/Code/"), that.destinationPath(pkgName + "/Code"), function (err) {
                        if (err) {
                            return console.error(err);
                        }
                        console.log('Copied code files');
                    });

                    ncp(that.templatePath("node/Config/"), that.destinationPath(pkgName + "/Config"), function (err) {
                        if (err) {
                            return console.error(err);
                        }
                        console.log('Copied config files');
                    });

                });
            });
        });

    },
    install: function () {
        this.installDependencies();
    },
    end: function () {
        console.log("Created application");
    }
});
