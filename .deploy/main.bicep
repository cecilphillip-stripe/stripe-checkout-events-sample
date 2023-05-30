@description('main resource location')
param location string = 'eastus'

@allowed([
  'Standard_LRS'
  'Standard_GRS'
])
param storageAccountType string = 'Standard_LRS'

var storageAccountName = 'stripeeventsstorage'
var functionAppName = 'stripeevents-webhook'

// Service Bus
@description('Stripe Events Service Bus')
resource stripeevents 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: 'stripeevents'
  location: location
  tags: {
    demo: 'stripe'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

@description('namespace listern access rule')
resource stripeevents_listener_rule 'Microsoft.ServiceBus/namespaces/authorizationrules@2022-10-01-preview' = {
  parent: stripeevents
  name: 'CheckoutEventsListener'
  properties: {
    rights: [
      'Listen'
    ]
  }
}

@description('namespace sender access rule')
resource stripeevents_sender_rule 'Microsoft.ServiceBus/namespaces/authorizationrules@2022-10-01-preview' = {
  parent: stripeevents
  name: 'CheckoutEventsSender'
  properties: {
    rights: [
      'Send'
    ]
  }
}

@description('service bus topic for stripe events')
resource checkoutevents_topic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: stripeevents
  name: 'stripe-checkout-events'
  properties: {
    maxMessageSizeInKilobytes: 256
    requiresDuplicateDetection: true
    supportOrdering: true
  }
}

//TODO: I could probably loop these subscriptions
@description('topic subscription for paid events')
resource checkoutevents_topic_paid_sub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: checkoutevents_topic
  name: 'checkout-complete-paid'
  properties: {
    requiresSession: false
    maxDeliveryCount: 10
  }
}

@description('topic subscription filter for paid events')
resource checkoutevents_topic_paid_sub_filter 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2022-10-01-preview' = {
  parent: checkoutevents_topic_paid_sub
  name: 'CheckoutCompletePaid'
  properties: {
    action: {
    }
    filterType: 'CorrelationFilter'
    correlationFilter: {
      properties: {
        'stripe-event': 'checkout.session.completed'
        'payment-status': 'paid'
      }
    }
  }
}

@description('topic subscription for expired events')
resource checkoutevents_topic_expired_sub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: checkoutevents_topic
  name: 'checkout-session-expired'
  properties: {
    requiresSession: false
    maxDeliveryCount: 10
  }
}

@description('topic subscription filter for expired events')
resource checkoutevents_topic_expired_sub_filter 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2022-10-01-preview' = {
  parent: checkoutevents_topic_expired_sub
  name: 'CheckoutSessionExpired'
  properties: {
    action: {
    }
    filterType: 'CorrelationFilter'
    correlationFilter: {
      properties: {
        'stripe-event': 'checkout.session.expired'
        status: 'open'
      }
    }
  }
}

@description('storage account for function app')
resource storageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: storageAccountName
  location: location
  kind: 'Storage'
  sku: {
    name: storageAccountType
  }
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
  tags: {
    demo: 'stripe'
    platform: 'dotnet'
  }
}

@description('App service plan for function app')
resource hostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: 'stripeevent-serverplan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
  tags: {
    demo: 'stripe'
    platform: 'dotnet'
  }
}

var stripeevents_endpoint = '${stripeevents.id}/AuthorizationRules/RootManageSharedAccessKey'
var stripeevents_connection = 'Endpoint=sb://${stripeevents.name}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=${listKeys(stripeevents_endpoint, stripeevents.apiVersion).primaryKey}'

@description('Function app to process stripe backend')
resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: '${toLower(functionAppName)}share'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~14'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'ServicebusConnection'
          value: stripeevents_connection
        }
      ]
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
        ]
      }
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
  tags: {
    demo: 'stripe'
    platform: 'dotnet'
  }
}

@description('Log Analytics workspace for resource group')
resource analyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: 'stripeevents-analytics-workspace'
  location: location
  properties: {
    retentionInDays: 30
  }
  tags: {
    demo: 'stripe'
    platform: 'dotnet'
  }
}

@description('application insights instance')
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'stripeevents-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    WorkspaceResourceId: analyticsWorkspace.id
  }
  tags: {
    demo: 'stripe'
    platform: 'dotnet'
  }
}

output functionHostname string = functionApp.properties.defaultHostName
