# Parameters defined
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Subscription,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$FrontDoor,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$FrontDoorResourceGroup,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$BackendPoolName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$BackendHostName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$TrafficEnabled
)
Write-Host "Setting subscription context: " $Subscription
#set the subscripton context
Set-AzContext -SubscriptionId $Subscription

Write-Host "Get FrontDoor Details for: " $FrontDoor
$frontdoorDetails = Get-AzFrontDoor -ResourceGroupName $FrontDoorResourceGroup -Name $FrontDoor

if($frontdoorDetails) {
    
    Write-Host "Retrieved FrontDoor details for: " $FrontDoor "Filtering backendpool for: " $BackendPoolName
    $FilteredbackendPool = $frontdoorDetails.BackendPools | Where-Object -FilterScript { $_.Name -eq $BackendPoolName }
    if($FilteredbackendPool) {
        
    $UpdateBackend = $FilteredbackendPool.Backends | Where-Object -FilterScript { $_.Address -eq $BackendHostName}
    Write-Host "Setting " $BackendHostName "to be " $TrafficEnabled

    $UpdateBackend.EnabledState = $TrafficEnabled

    Set-AzFrontDoor -ResourceGroupName $FrontDoorResourceGroup -Name $FrontDoor -BackendPool $frontdoorDetails.BackendPools

    Write-Host "Updated frontdoor settings for " $FrontDoor
    }
} else {
    Write-Host "Could not retrieve frontdoor details for: " $FrontDoor
}