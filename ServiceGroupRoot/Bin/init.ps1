#Description: Simple setup script to initialize resources such as keyvault certificates, AAD apps, EasyAuth configuration
#init.ps1

$exitCode = 0
function Show-Error([string]$Message, [Object]$Details){
    $exitCode++
    Write-Error $Message
    Write-Host $Details
}

#Current Ev2 infrastructure: Test, Prod, Fairfax, Mooncake, Blackforest, USNat, USSec
if ($env:CloudEnvironment -ieq "USNat") {
    az cloud register --name "$env:CloudEnvironment" --endpoint-active-directory "https://login.microsoftonline.eaglex.ic.gov/" --endpoint-active-directory-graph-resource-id "https://graph.cloudapi.eaglex.ic.gov" --endpoint-active-directory-resource-id "https://management.azure.eaglex.ic.gov" --endpoint-management "https://management.core.eaglex.ic.gov" --endpoint-resource-manager "https://management.azure.eaglex.ic.gov" --suffix-acr-login-server-endpoint "https://acr.eaglex.ic.gov" --suffix-keyvault-dns '.vault.cloudapi.eaglex.ic.gov' --suffix-storage-endpoint "core.eaglex.ic.gov"
    az cloud set --name "$env:CloudEnvironment"
}

if ($env:COMPUTERNAME -inotlike "hawfor*" -and $env:NAME -inotlike "hawfor*") {
    Write-Host "Login with service principal"
    az login --service-principal -u $env:ServicePrincipalApplicationId -p $env:ServicePrincipalClientSecret --tenant $env:TenantId
    az account set --subscription $env:SubscriptionId   
}

Write-Host "Add $env:ServicePrincipalApplicationId to keyvault access policies"

#Add the service principal object itself as authorized keyvault user
az keyvault set-policy --resource-group $env:AzureResourceGroupName --name $env:KeyVaultName --spn "$env:ServicePrincipalApplicationId" --secret-permissions get list --certificate-permissions get getissuers list listissuers manageissuers setissuers update

#TODO iterate over a list of environment variables with a prefix of DiagnosticCertificate* and iterate over to add each to the web app
Write-Host "Add keyvault certificate $env:KeyVaultCertificate to $env:SiteName"
$response = az webapp config ssl import --key-vault $env:KeyVaultName --key-vault-certificate-name $env:KeyVaultCertificate --resource-group $env:AzureResourceGroupName --name $env:SiteName

if($response -ne $null){
    Write-Host "Added keyvault certificate Succesfully"
}else{
    Show-Error -Message "Failed to add keyvault certificate" -Details $response
}

#Add the AAD auth to this app. First check if the app was created
$response = az webapp show --resource-group $env:AzureResourceGroupName --name $env:SiteName | ConvertFrom-Json
$url = "https://$($response.defaultHostName)"

#TODO Find a way to give the MSI object authorization to the tenant so that it may create an AAD app in an automated fashion
#Write-Host "Create AAD app for $url"
#az ad app create --display-name $env:AzureResourceGroupName --homepage $url --identifier-uris $url --reply-urls "$url/.auth/login/aad/callback" --required-resource-accesses '@aad_app_create_manifest.json'

$accountJson = az cloud show | ConvertFrom-Json
$tenantJson = az account show | ConvertFrom-Json
$issuerUrl = "$($accountJson.endpoints.activeDirectory)/$($tenantJson.tenantId)"

Write-Host "Update EasyAuth settings"
$response = az webapp auth update --name $env:SiteName --resource-group $env:AzureResourceGroupName --action LoginWithAzureActiveDirectory --enabled true --aad-client-id $env:EasyAuthClientId --aad-token-issuer-url $issuerUrl
if ($response -ne $null) {
    Write-Host "Successfully updated EasyAuth settings"
}else{
    Show-Error "Failed to update EasyAuth" $response
}

if ($exitCode -ne 0) {
    throw "setup script failed"
}