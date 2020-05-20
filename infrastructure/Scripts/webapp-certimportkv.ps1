$subId = Read-Host "Please enter subscription id"
Set-AzureRmContext -SubscriptionId $subId

$location = Read-Host "Please enter location of the web app, eg: Central US "
$ResourceGroupName = Read-Host "Please enter resource group of web app"
$webappName = Read-Host "Please enter web app name"

$webAppResource = Get-AzureRmWebApp -ResourceGroupName $ResourceGroupName -Name $webappName
$AppServicePlanName = $webAppResource.ServerFarmId

$vaultName =  "AppServiceDiagnosticsKV"

$keyVaultResource = Get-AzureRmKeyVault -VaultName $vaultName

$keyVaultId = $keyVaultResource.ResourceId

$webAppRPServicePrincipal = "abfa0a7c-a6b6-4736-8310-5855508787cd"

Set-AzureRmKeyVaultAccessPolicy -VaultName $vaultName -ServicePrincipalName $webAppRPServicePrincipal -PermissionsToSecrets get 

$geoMasterCert = "DiagnosticToGeoMaster"

$PropertiesObject = @{
serverFarmId = $AppServicePlanName
keyVaultId = $keyVaultId
keyVaultSecretName = $geoMasterCert
}

New-AzureRmResource -Name $geoMasterCert -Location $location `
    -PropertyObject $PropertiesObject `
    -ResourceGroupName $ResourceGroupName `
    -ResourceType Microsoft.Web/certificates `
    -Force

$mdmCert = "DiagnosticsToMdmOneCert"

$PropertiesObject = @{
    serverFarmId = $AppServicePlanName
    keyVaultId = $keyVaultId
    keyVaultSecretName = $mdmCert
    }

New-AzureRmResource -Name $mdmCert -Location $location `
-PropertyObject $PropertiesObject `
-ResourceGroupName $ResourceGroupName `
-ResourceType Microsoft.Web/certificates `
-Force
