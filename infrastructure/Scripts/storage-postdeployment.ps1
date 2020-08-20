param ($resourceGroup, $storageAccount)

$tableName = "diagentities"

Write-Host "Fetching storage account for" $resourceGroup $storageAccount

$storageAccount = Get-AzStorageAccount -ResourceGroupName $resourceGroup -Name $storageAccount

$ctx = $storageAccount.Context

Write-Host "Creating new table" $tableName

$tableCreation = New-AzStorageTable -Name $tableName -Context $ctx

Write-Host $tableCreation

$detectorConfigTable = "detectorconfig"

Write-Host "Create new table" $detectorConfigTable

$tableCreation = New-AzStorageTable -Name $detectorConfigTable -Context $ctx

Write-Host $tableCreation

$containerName = "detectors"

Write-Host "Creating new container" $containerName

$containerCreation = New-AzStorageContainer -Name $containerName -Context $ctx

Write-Host $containerCreation 
