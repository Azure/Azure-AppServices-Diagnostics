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

if ($exitCode -ne 0) {
    throw "setup script failed"
}