## Intent
The goal of this deployment folder is to fully automate the infrastructure deployment of the App Service Diagnostics application, as well as other services in the application's cluster.
This will make it easier to repeat this infrastructure in other environments.

ARM templates also control Geneva configuration upgrades.

#### Note
Currently these deployments only work in the Azure Portal Powershell terminal

## Commands
To deploy an ARM template from Powershell:
- Get portal access for the Diag Production WebApp subscription
- Find the subscription on Azure Portal
- Upload the template and parameter files from this directory to the Portal terminal
- `cd` to the uploaded directory
- Enter the following commands in the Portal Powershell terminal

```PS
Select-AzureRmSubscription `
  -SubscriptionId "..."

New-AzureRmResourceGroupDeployment `
  -ResourceGroupName "..." `
  -TemplateFile ".\antareskvtemplate.json" `
  -TemplateParameterFile ".\{appname}-antareskvparameters.json"
```

Useful command to get current settings:
```PS
Get-AzureRmResourceGroupDeployment -ResourceGroupName "..."
```
