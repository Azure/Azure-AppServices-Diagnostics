# Parameters defined
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Subscription,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$VaultName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$WebappResourceGroup,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$WebappName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$WebappSlot
)
Write-Host "Setting subscription context" $Subscrption
#set the subscripton context
Set-AzContext -SubscriptionId $Subscription

Write-Host "Fetching all secrets from keyvault" $VaultName
#get all secrets from kv 
$allsecrets = Get-AzKeyVaultSecret -VaultName $VaultName

Write-Host "Fetching current web app settings for Webapp:" $WebappName", Resource Group:" $WebappResourceGroup", Slot:" $WebappSlot
#Get current web app settings
$hash = @{}
$webApp = Get-AzWebAppSlot -ResourceGroupName $WebappResourceGroup -Name $WebappName -Slot $WebappSlot
$appSettingList = $webApp.SiteConfig.AppSettings

Write-Host "Total app settings fetched = " $appSettingList.Count
$hash = @{}
$stickySlotSettings = @()
 #setup the current app settings
ForEach ($kvp in $appSettingList) {
    $hash[$kvp.Name] = $kvp.Value
    Write-Host "Adding : " $kvp.Name "to sticky slot settings list"
    $stickySlotSettings += $kvp.Name
}
Write-Host "Total secrets fetched = " $allsecrets.Count
foreach ($secret in $allsecrets) {

    $secretName = $secret.Name;
    #Get plain text value of current version of secret
    Write-Host "Fetching plain text value of current version of secret" $secretName
    try {
        $secretdetails = Get-AzKeyVaultSecret -VaultName $VaultName -Name $secretName
        $secretValueText = '';
        $ssPtr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secretdetails.SecretValue)
        try {
            $secretValueText = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ssPtr)
        } finally {
            [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ssPtr)
        }
    
        #set as app setting
        $appsettingName = $secretName.replace("--",":");
        $hash[$appsettingName] = $secretValueText;
        $stickySlotSettings += $appsettingName
    } catch {
        Write-Host "Error occured while processing secret "$secretName
        Write-Host "Error Message: " $_.Exception.Message
        Write-Host "Skipping to next secret"
    }
  
}
$hash["Secrets:KeyVaultEnabled"] = "false"
Write-Host "Applying keyvault secrets to app settings for Webapp:" $WebappName ", Resource Group:" $WebappResourceGroup", Slot:" $WebappSlot
Set-AzWebAppSlot -ResourceGroupName $WebappResourceGroup -Name $WebappName -AppSettings $hash -Slot $WebappSlot

#set slot sticky properties
$stickSlotConfigObject = @{"appSettingNames" = $stickySlotSettings;}


Write-Host "Applying sticky slot app settings for Webapp:" $WebappName ", Resource Group:" $WebappResourceGroup", Slot:" $WebappSlot

$result = Set-AzResource -Properties $stickSlotConfigObject -ResourceGroupName $WebappResourceGroup  -ResourceType Microsoft.Web/sites/config -ResourceName $WebappName/slotConfigNames -ApiVersion 2018-02-01 -Force