param ($resourceGroup, $storageAccount)

$tableName = "diagentities"

Write-Host "Fetching storage account for" $resourceGroup $storageAccount

$storageAccount = Get-AzStorageAccount -ResourceGroupName $resourceGroup -Name $storageAccount

$ctx = $storageAccount.Context

Write-Host "Creating new table" $tableName

$tableCreation = New-AzStorageTable -Name $tableName -Context $ctx

Write-Host $tableCreation
