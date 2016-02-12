

[cmdletbinding()]
Param (
    [string]$appPackagePath,
    [string]$appName,
    [string]$appType,
    [string]$appTypeVersion,
    [string]$appImageStoreName
)


Write-Verbose "appPackagePath:$appPackagePath"
Write-Verbose "appName:$appName"
Write-Verbose "appType:$appType"
Write-Verbose "appTypeVersion:$appTypeVersion"
Write-Verbose "appImageStoreName:$appImageStoreName"

$ErrorActionPreference = "Stop"

function Get-ImageStoreConnectionStringFromClusterManifest
{
    <#
    .SYNOPSIS 
    Returns the value of the image store connection string from the cluster manifest.

    .PARAMETER ClusterManifest
    Contents of cluster manifest file.
    #>

    [CmdletBinding()]
    Param
    (
        [xml]
        $ClusterManifest
    )

    $managementSection = $ClusterManifest.ClusterManifest.FabricSettings.Section | ? { $_.Name -eq "Management" }
    return $managementSection.ChildNodes | ? { $_.Name -eq "ImageStoreConnectionString" } | Select-Object -Expand Value
}

#Store\redishost

##Update this command to deploy to remote cluster as needed. 
Write-Verbose "Connecting to cluster..."
Connect-ServiceFabricCluster

Write-Verbose "Successfully connected!"

Write-Verbose "Copying application package..."
$clusterManifestText = Get-ServiceFabricClusterManifest
$imageStoreConnectionString = Get-ImageStoreConnectionStringFromClusterManifest ([xml] $clusterManifestText)
##When deploying to azure cluster remove the imagestoreconnectionstring parameter
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $appPackagePath -ImageStoreConnectionString $imageStoreConnectionString -ApplicationPackagePathInImageStore $appImageStoreName
Write-Verbose "Successfully copied app!"

Write-Verbose "Test the application package..."
Test-ServiceFabricApplicationPackage $appPackagePath -ImageStoreConnectionString $imageStoreConnectionString -Verbose
#Write-Verbose "Successfully registered app!"

##For testing - clean up any existing deployments
Write-Verbose "Cleaning up existing deployments of $appName"
$service = Get-ServiceFabricApplication -ApplicationName "fabric:/$appName"
if ($service)
{
    Write-Verbose "-- Removing app $($service.ApplicationName)"
    remove-servicefabricapplication $service.ApplicationName -force
}

$types = get-servicefabricapplicationtype $appType
if ($types)
{
    foreach ($type in $types)
    {
        Write-Verbose "-- Removing type $($service.ApplicationName)"
        Unregister-ServiceFabricApplicationType $type.ApplicationTypeName -ApplicationTypeVersion  $type.ApplicationTypeVersion -force
    }
}

##Now lets register it again and deploy
Write-Verbose "Registering the application"
Register-ServiceFabricApplicationType -ApplicationPathInImageStore $appImageStoreName
Write-Verbose "Successfully registered!"

Write-Verbose "creating new app instance!"
New-ServiceFabricApplication -ApplicationName $appName -ApplicationTypeName $appType -ApplicationTypeVersion 1.0.0.3

