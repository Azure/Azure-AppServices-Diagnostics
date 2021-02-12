
var testCheck = {
    title: "testCheck",
    func: async function testCheck(siteInfo, appSettings, diagProvider){
        
        //debugger;
        return {level: 2, markdown: "hello"};
    }
}

var connectionStringCheck = {
    title: "Check the connection string resource connectivity",
    func: async function connectionStringCheck(siteInfo, appSettings, diagProvider){
        var connectionStringSettings = (await diagProvider.postArmResourceAsync(siteInfo.resourceUri+"/config/connectionstrings/list")).properties;
        var connMap = Object.fromEntries(Object.keys(connectionStringSettings).map(key => [key, parseConnString(connectionStringSettings[key].value)]));
        var connTests = Object.keys(connMap).flatMap(key1 => Object.keys(connMap[key1]).map(key2=>{
            var pair = connMap[key1][key2];
            return [key1, `${key2}`, `${pair.hostname}:${pair.port}`, diagProvider.checkConnectionAsync(pair.hostname, pair.port)];
        }));
        connTests = await Promise.all(connTests.map(async t=> [t[0], t[1], t[2], await t[3]]));
        var markdown = 
        `|connection string| type | hostname:port | ip | aliases | check result|
         |-----------------|------|---------------|----|---------|-------------|`;
        markdown+="\r\n" + connTests.map(t=> `[${t[0]}](${connectionStringSettings[t[0]].value} "${connectionStringSettings[t[0]].value}")|${t[1]}|${t[2]}|${t[3].ip}|${t[3].aliases}|${t[3].status == 0 ? "🟢Ok": "🚫Fail"}|`).join("\r\n");
        return {level: 3, markdown: markdown};
    }
}

var environmentVarCheck = {
    title: "Check environment variable",
    func: async function environmentVarCheck(siteInfo, appSettings, diagProvider){
        var vars = ["WEBSITE_PRIVATE_IP", "WEBSITE_PRIVATE_IPx"];
        var result = await diagProvider.getEnvironmentVariablesAsync(vars);
        var markdown = "vars:" + vars.join(";") + "\r\n" + result.join(";");
        return {level: 3, markdown: "```\r\n"+markdown+"\r\n```"};
    }
}

function parseConnString(connStr){
    var createPair = (hostname, port) => new Object({hostname:hostname, port:parseInt(port)});
    var results = {};
    if(connStr.startsWith("Server=tcp:")){
        // sql server connection string
        var m = connStr.match(/Server=tcp:(.*?),(.*?);/);
        results["default"] = createPair(m[1], m[2]);
    }else if(connStr.startsWith("BlobEndpoint")){
        // storage account connection string
        var lines = connStr.split(";");
        for(var i in lines){
            var m = lines[i].match(/(.*?)Endpoint=https:\/\/(.*?)\//);
            if(m != null){
                results[m[1].toLowerCase()] = createPair(m[2], 443);
            }
        }
    }else if(connStr.startsWith("DefaultEndpointsProtocol")){
        // another kind of storage account connection string
        var services = ["blob", "queue", "file", "table"];
        var m = connStr.match(/DefaultEndpointsProtocol=(.*?);.*?AccountName=(.*?);.*?EndpointSuffix=(.*?)$/);
        var port = (m[1] == "https" ? 443:80);
        var accountName = m[2], suffix = m[3];
        var urls = services.map(s=> createPair(`${accountName}.${s}.${suffix}`, port));
        for(var i in services){
            results[services[i]] = urls[i];
        }
    }else if(connStr.startsWith("https://") || connStr.startsWith("http://")){
        var m = connStr.match(/(http.*?):\/\/(.*?)[\/\s]/);
        var port = (m[1] == "https" ? 443:80);
        results["default"] = createPair(m[2], port);
        // normal url
    }else{
        // unknown
        var e = new Error("unknown connection string type");
        e.data = {connStr: connStr};
        throw e;
    }

    return results;
}

var connectivityCheck = {
    title: "Check connectivities between app and selected targets",
    func: async function connectivityCheck(siteInfo, appSettings, diagProvider){
        // check if the environment variable WEBSITE_PRIVATE_IP is set or not, if not, app was not integrated to a VNet, check fails
        var markdown = "";
        var privateIp = (await diagProvider.getEnvironmentVariablesAsync(["WEBSITE_PRIVATE_IP"]))[0];
        if(privateIp == null){
            return {level: 2, markdown: "WEBSITE_PRIVATE_IP env is not set, app was not integrated to VNet successfully"};
        }
        markdown += `WEBSITE_PRIVATE_IP is set to ${privateIp}\r\n\r\n`;

        var dnsSettings = [appSettings["WEBSITE_DNS_SERVER"], appSettings["WEBSITE_DNS_ALT_SERVER"]].filter(i => i!=null);
        var customDns = undefined;
        if(dnsSettings.length>0){
            // verify if custom dns is reachable
            for(var i in dnsSettings){
                var result = await diagProvider.tcpPingAsync(dnsSettings[i], 53);
                if(result.status == 0){
                    customDns = dnsSettings[i];
                    break;
                }
            }
            if(customDns == undefined){
                return {level: 2, markdown: `Custom DNS server ${dnsSettings} is not reachable!`};
            }
            markdown += `custom DNS setting detected, verified ${customDns} is reachable.\r\n\r\n`;
        }
        else{
            markdown += "No custom DNS is set\r\n\r\n";
        }
        

        var connectionStringSettings = (await diagProvider.postArmResourceAsync(siteInfo.resourceUri+"/config/connectionstrings/list")).properties;
        var targets = Object.keys(connectionStringSettings).flatMap(connName => {
            var setting = connectionStringSettings[connName];
            var parsed = parseConnString(setting.value);
            return Object.keys(parsed).map(key => new Object({resource:`connection string setting [${connName}](${setting.value} "${setting.value}") ${key == "default" ? "" : key}`, endpoint: parsed[key]}));
        });

        

        targets = targets.map(target => new Object({...target, task: diagProvider.checkConnectionAsync(target.endpoint.hostname, target.endpoint.port)}));

        await Promise.all(targets.map(t=>t.task));
        var statusMap = [
            "🟢Ok",
            "🚫Timeout, target unreachable",
            "🚫Host not found",
            "🚫Blocked by worker"
        ];
        var table = 
        `|resource| endpoint | result |
         |--------|----------|--------|`;
        var failureCnt = 0;
        for(var i in targets){
            var target = targets[i];
            var result = await target.task;
            table += `\r\n|${target.resource}|${target.endpoint.hostname}:${target.endpoint.port}|${statusMap[result.status]}`;
            if(result.status!=0){
                ++failureCnt;
            }
        }
        markdown += table+"\r\n\r\n";
        if(failureCnt > 0){
            if(failureCnt == targets.length){
                markdown += `All ${targets.length} resource endpoint connectivity test failed!`;
                return {level: 2, markdown};
            }else{
                markdown += `${failureCnt}/${targets.length} resource endpoint connectivity test failed!`;
                return {level: 1, markdown};
            }
        }else{
            markdown += `All ${targets.length} resource endpoint connectivity test succeeded(at tcp level only)!`;
        }
        return {level: 0, markdown};
    }
}


var AspVnetConfigVerification={
    title : "Checking App Service Plan Vnet Configuration",
    func:async function AspVnetConfigVerification(siteInfo, appSettings, armService){
        var level, msg;
        var siteArmId = siteInfo["resourceUri"];
        var siteResource = await armService.getArmResourceAsync(siteArmId);
        var serverFarmId = siteResource["properties"]["serverFarmId"];
        //console.log("serverFarmId: "+ serverFarmId);
        var swiftUrl = siteArmId + "/config/virtualNetwork";;
        var curSiteVnetInfo = await armService.getArmResourceAsync(swiftUrl);
        console.log(curSiteVnetInfo);
        var curSiteSubnetResourceId ;
        if(curSiteVnetInfo!= null)
        {
            curSiteSubnetResourceId = curSiteVnetInfo["properties"]["subnetResourceId"];
        }
        
        msg = "App Service Plan Resource URI: "+ serverFarmId;
        var aspSites = await armService.getArmResourceAsync(serverFarmId+"/sites");
        console.log(aspSites);
        var siteArray = aspSites["value"];
        
        var siteCount = aspSites.length;
        msg = "App Service Plan has **"+ siteCount + "** sites";
        var aspSubnetArray=[];
        // Get individual sites vnet integration setting
        var aspSubnetDict={};
        if(aspSites!=null){
            for(var site in aspSites){
                var siteResourceUri = site["id"];
                var siteSwiftUrl = siteResourceUri + "/config/virtualNetwork";
                var siteVnetInfo = await armService.getArmResourceAsync(siteSwiftUrl);
                if(siteVnetInfo != null){                
                    console.log(siteVnetInfo);                                            
                    var subnetResourceId = siteVnetInfo["properties"]["subnetResourceId"];
                    if(subnetResourceId!=null){
                        // create an array with subnetUri
                        aspSubnetArray.push(subnetResourceId);
                        //create a dictionary using subnet uri as key
                        if(aspSubnetDict[subnetResourceId]){
                            aspSubnetDict[subnetResourceId].push(site["name"]);                        
                        }
                        else{
                            aspSubnetDict[subnetResourceId] = [site["name"]];
                        }
                    }
                }            
            }
        }
        // check if the unique subnet count is more than one and generate appropriate instructions
        var uniqueAspSubnetCount = Object.keys(aspSubnetDict).length;
        if(uniqueAspSubnetCount > 1)
        {
            var s = `
            App Service Plan is connected to multiple(${uniqueAspSubnetCount}) subnets. This might affect outbound connectivity to VNet hosted resources!
            ## Recommendation
            - Please ensure that all of the Web App in the App Service Plan are integrated to the same subnet.
            - Review the below table to review the configuration:            
            `
            var aspVnetTable = `
            | Subnet URI | Web App(s)|
            | -----------|-----------|
            `
            for(var subnet in aspSubnetDict){
                aspVnetTable = aspVnetTable.concat(`            |${subnet}|${aspSubnetDict[subnet]}|\r\n`);
            }
            msg = s;
            msg += aspVnetTable;
            level = 1;
        }
        else{        
            msg = "App Service Plan is connected to one subnet. ";           
        }
        //console.log(aspSitesVnetInfoArray);
        return {level: level, markdown: msg};
    }
}

var jsDynamicImportChecks = [testCheck, connectionStringCheck, environmentVarCheck, connectivityCheck, AspVnetConfigVerification];
