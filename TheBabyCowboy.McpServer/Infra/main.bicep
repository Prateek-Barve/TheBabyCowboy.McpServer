// Set location default to Central India (Pune)
param location string = 'centralindia'
param appName string = 'thebabycowboy-mcp'

// 1. App Service Plan - Windows Free Tier (F1)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${appName}-plan'
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  kind: 'app'
  properties: {
    reserved: false // false = Windows OS (100% free indefinitely)
  }
}

// 2. Web App Service
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      netFrameworkVersion: 'v9.0' // Set to your target .NET version (.NET 9)
    }
  }
}

output webAppName string = appService.name