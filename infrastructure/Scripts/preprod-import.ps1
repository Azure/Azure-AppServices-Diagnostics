
param(

[Parameter(Mandatory = $true)]
[ValidateNotNullOrEmpty()]
[string]$subscriptionId,

[Parameter(Mandatory = $true)]
[ValidateNotNullOrEmpty()]
[string]$resourceGroupName,

[Parameter(Mandatory = $true)]
[ValidateNotNullOrEmpty()]
[string]$storageAccountName,

[Parameter(Mandatory = $true)]
[ValidateNotNullOrEmpty()]
[string]$location,

[Parameter(Mandatory = $true)]
[ValidateNotNullOrEmpty()]
[string]$sourceTable,

[Parameter(Mandatory = $true)]
[ValidateNotNullOrEmpty()]
[string]$destinationTable,

[Parameter(Mandatory = $true)]
[ValidateNotNullOrEmpty()]
[string]$sourceContainer,

[Parameter(Mandatory = $true)]
[ValidateNotNullOrEmpty()]
[string]$destinationContainer,

[Parameter(Mandatory = $true)]
[ValidateNotNullOrEmpty()]
[string]$resourceProvider,

[Parameter(Mandatory = $true)]
[ValidateNotNullOrEmpty()]
[string]$resourceProviderType

)


# Log on to Azure and set the active subscription

Write-Host "Setting subscription as $subscriptionId"

Select-AzureRmSubscription -SubscriptionName $subscriptionId

# Get the storage key for the storage account
$storageAccountKey = (Get-AzureRmStorageAccountKey -ResourceGroupName $resourceGroupName -Name $storageAccountName).Value[0]

# Get a storage context
$ctx = New-AzureStorageContext -StorageAccountName $storageAccountName -StorageAccountKey $storageAccountKey

# Get a reference to the table
$cloudTable = (Get-AzureStorageTable -Name $sourceTable -Context $ctx ).CloudTable

$customFilter = "(ResourceProvider eq '$resourceProvider') and (ResourceType eq '$resourceProviderType')" 

Write-Host "Executing query with filter $customFilter"

$result = Get-AzTableRow -table $cloudTable -customFilter $customFilter

foreach ($tablerow in $result)
 {

  try {
  $partitionKey = $tableRow.PartitionKey
  $rowKey = $tableRow.RowKey
  $Entity = New-Object "Microsoft.Azure.Cosmos.Table.DynamicTableEntity" "$partitionKey", "$rowKey"
  #Create entity object with properties
  foreach($object_properties in $tablerow.PsObject.Properties)
  {
    $Entity.Properties.Add( $object_properties.Name,  $object_properties.Value)
  }

  $Entity.Timestamp = [DateTime]::UtcNow | get-date
  $destinationCloudTable = (Get-AzureStorageTable -Name $destinationTable -Context $ctx ).CloudTable
  [Microsoft.Azure.Cosmos.Table.TableOperation]$tableOperation=[Microsoft.Azure.Cosmos.Table.TableOperation]::InsertOrMerge($Entity)

  Write-Host "Inserting $tablerow.DetectorId into $destinationTable"
   
  $insertResult = $destinationCloudTable.Execute($tableOperation)
  } catch {
    $message = $_ 
    Write-Warning "Exception occured while importing rows $message"
  }

}

 Write-Host "Finished inserting entities into table $destinationTable"
 Write-Host "Starting blob copy..."

 foreach ($tablerow in $result) {
  try {
  $result = $tablerow.DetectorId;
  $filename = "$result\$result.dll".ToLower();
  Write-Host "File to import is $filename"
  az storage blob copy start --account-name $storageAccountName --destination-blob $filename --destination-container $destinationContainer --subscription $subscriptionId --source-blob $filename --source-container $sourceContainer 
  } catch {
      $message = $_ 
      Write-Warning "Exception occured while importing blob $message"
    }
 
}

Write-Host "Finished blob copy into $destinationContainer"