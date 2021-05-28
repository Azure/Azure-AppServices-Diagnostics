# Parameters defined
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Subscription,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$TrafficManager,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$TrafficManagerResourceGroup,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Endpoint,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$EndpointType,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [bool]$TrafficEnabled
)


Write-Host "Setting subscription context: " $Subscription
#set the subscripton context
Set-AzContext -SubscriptionId $Subscription

Write-Host "Get TrafficManager Details for: " $TrafficManager
$trafficmanagerDetails = Get-AzTrafficManagerProfile -ResourceGroupName $TrafficManagerResourceGroup -Name $TrafficManager

if($trafficmanagerDetails) {

    if ($TrafficEnabled){
        Enable-AzTrafficManagerEndpoint -Name $Endpoint -Type $EndpointType -ProfileName $TrafficManager -ResourceGroupName $TrafficManagerResourceGroup 
    }
    else {
        Disable-AzTrafficManagerEndpoint -Name $Endpoint -Type $EndpointType -ProfileName $TrafficManager -ResourceGroupName $TrafficManagerResourceGroup -Force
    }
    Write-Host "Updated frontdoor settings for " $TrafficManager

} else {
    Write-Host "Could not retrieve frontdoor details for: " $TrafficManager
}