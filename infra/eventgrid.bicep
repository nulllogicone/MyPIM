@description('The location to deploy the resources to')
param location string

@description('The name of the event grid topic')
param topicName string

resource topic 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: topicName
  location: location
  sku: {
    name: 'Basic'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output topicEndpoint string = topic.properties.endpoint
output topicId string = topic.id
