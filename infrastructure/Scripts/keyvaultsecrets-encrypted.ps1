param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Subscription,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$VaultName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$encryptionkey,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$initvector,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty]
    [string]$filePath
)
$json = @{}
Write-Host "Setting subscription context" $Subscription
#set the subscripton context
Set-AzContext -SubscriptionId $Subscription
Write-Host "Fetching all secrets from keyvault" $VaultName
#get all secrets from kv 
$allsecrets = Get-AzKeyVaultSecret -VaultName $VaultName
Write-Host "Total secrets fetched = " $allsecrets.Count

$keyBytes = [System.Convert]::FromBase64String($encryptionkey)
$ivBytes = [System.Convert]::FromBase64String($initvector)

#Create a AES Crypto Provider:
$AESCipher = New-Object System.Security.Cryptography.AesCryptoServiceProvider
#Add the Key and IV to the Cipher
$AESCipher.Key  = $keyBytes
$AESCipher.IV  = $ivBytes
$Encryptor = $AESCipher.CreateEncryptor()
try {
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
    
            $UnencryptedBytes = [System.Text.Encoding]::UTF8.GetBytes($secretValueText)
            $EncryptedBytes = $Encryptor.TransformFinalBlock($UnencryptedBytes, 0, $UnencryptedBytes.Length)
            #Save the IV information with the data:
            [byte[]] $FullData = $AESCipher.IV + $EncryptedBytes
            # Transforms the data to string format
            $CipherText  = [System.Convert]::ToBase64String($FullData)
            #set as app setting
            $appsettingName = $secretName.replace("--",":");
            Write-Host "setting encrypted app setting" $appSettingName
            $json[$appSettingName] = $CipherText
    
        } catch {
            Write-Host "Error occured while processing secret "$secretName
            Write-Host "Error Message: " $_.Exception.Message
            Write-Host "Skipping to next secret"
        }
      
    }
} catch {
    Write-Host "Error Message: " $_.Exception.Message
} finally {
    #Cleanup the Cipher
    $AESCipher.Dispose()
}

$jsonstring = ConvertTo-Json -InputObject $json

Write-Host "Writing encrypted settings to file" $filePath

Out-File -FilePath $filePath -InputObject $jsonstring