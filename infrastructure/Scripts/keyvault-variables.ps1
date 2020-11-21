Write-Host "Setting subscription context" $Subscription
#set the subscripton context
Set-AzContext -SubscriptionId $Subscription

Write-Host "Fetching all secrets from keyvault" $VaultName
#get all secrets from kv 
$allsecrets = Get-AzKeyVaultSecret -VaultName $VaultName

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
        $appsettingName = $secretName.replace("--",".");
        Write-Host "setting app setting" $appSettingName
       Write-Host "##vso[task.setvariable variable=$($appSettingName)]$($secretValueText)"

    } catch {
        Write-Host "Error occured while processing secret "$secretName
        Write-Host "Error Message: " $_.Exception.Message
        Write-Host "Skipping to next secret"
    }
  
}
