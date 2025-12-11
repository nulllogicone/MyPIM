@description('The location to deploy the resources to')
param location string

@description('The name of the event grid topic')
param topicName string

@description('The URL of the Event Grid Viewer (optional)')
@secure()
param viewerUrl string = ''

resource topic 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: topicName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
}

resource eventSubscription 'Microsoft.EventGrid/eventSubscriptions@2022-06-15' = if (!empty(viewerUrl)) {
  scope: topic
  name: 'viewerSubscription'
  properties: {
    destination: {
      endpointType: 'WebHook'
      properties: {
        endpointUrl: viewerUrl
      }
    }
    filter: {
      isSubjectCaseSensitive: false
      enableAdvancedFilteringOnArrays: true
    }
  }
}

output topicEndpoint string = topic.properties.endpoint
output topicId string = topic.id
