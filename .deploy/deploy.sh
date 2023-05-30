RESOURCE_GROUP="stripe-events-rg"
LOCATION="eastus"

az group create --name ${RESOURCE_GROUP} --location $LOCATION
az deployment group create --resource-group ${RESOURCE_GROUP} --template-file main.bicep
# az deployment group what-if --resource-group ${RESOURCE_GROUP} --template-file stripe-events-demo.bicep

# https://www.pluralsight.com/guides/how-to-use-managed-identity-with-azure-service-bus
APP_IDENTITY=$(az functionapp identity show -g $RESOURCE_GROUP -n stripeevents-webhook --query "{principalId:principalId}" -o tsv)
SERVICEBUS_ID=$(az servicebus namespace show  -n stripeevents -g $RESOURCE_GROUP -o tsv --query "{id:id}")

# assign roles to listen and send messages to the queue
az role assignment create --role "Azure Service Bus Data Owner" --assignee $APP_IDENTITY --scope $SERVICEBUS_ID
