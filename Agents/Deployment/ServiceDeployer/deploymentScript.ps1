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

##Update this command to deploy to remote cluster as needed. 
Connect-ServiceFabricCluster localhost:19000

Write-Host 'Copying application package...'
##When deploying to azure cluster remove the imagestoreconnectionstring parameter
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $appPackagePath -ImageStoreConnectionString 'file:C:\SfDevCluster\Data\ImageStoreShare' -ApplicationPackagePathInImageStore $appImageStoreName

Write-Host 'Registering application type...'
Test-ServiceFabricApplicationPackage $appPackagePath

##For testing - clean up any existing deployments
$service = get-ServiceFabricApplication -ApplicationName $appName
if ($service)
{
    remove-servicefabricapplication $service.ApplicationName -force
}

$types = get-servicefabricapplicationtype $appType
if ($types)
{
    foreach ($type in $types)
    {
        Unregister-ServiceFabricApplicationType $type.ApplicationTypeName -ApplicationTypeVersion  $type.ApplicationTypeVersion -force
    }
}

##Now lets register it again and deploy
Register-ServiceFabricApplicationType -ApplicationPathInImageStore $appImageStoreName

New-ServiceFabricApplication -ApplicationName $appName -ApplicationTypeName $appType -ApplicationTypeVersion 1.0.0.3

