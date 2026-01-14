param name string
param location string

resource speech_resource 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: take('${name}-${uniqueString(resourceGroup().id)}', 64)
  location: 'westeurope'
  sku: {
    name: 'F0'
  }
  kind: 'SpeechServices'
  identity: {
    type: 'None'
  }
  properties: {
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    allowProjectManagement: false
    publicNetworkAccess: 'Enabled'
  }
  tags: {
    'aspire-resource-location': location
  }
}

output endpoint string = speech_resource.properties.endpoint
