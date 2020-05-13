## Intent
The goal of this deployment folder is to fully automate the infrastructure of the App Service Diagnostics application.
This will make it trivial to create the resources needed to deploy ASD into new clouds.

ARM templates also control Geneva configuration upgrades.

#### Note
Currently the best way to run these deployments is to run them through azure dev ops
Commit your changes, get them approved then run the release pipeline: `ARM Template Deployment Liberation`


#### Architecture Overview.

The aim is to use the `DeployAll.parameters.json`
and then run it against `DeployAllAppServices.json`
to create new app services or update their configurations.

After set up of new app services and deployments have been tested and verified you can then run
  `DeployAll.parameters.json`
with 
  `DeployAllFrontDoors.json`
to add the apps to the correct front doors.

Many of the variables are configured inside of `DeployAllAppServices.json` and `DeployAllFrontDoors.json` **Note** not all configuration is done inside the `DeployAll.parameters.json` file.

The parameters file is primarily used as a way to get key vault secrets used for deployment.