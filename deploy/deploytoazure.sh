# Text Output Formatting
NONE=$(tput setaf 0)
RED=$(tput setaf 1)
GREEN=$(tput setaf 2)
YELLOW=$(tput setaf 3)
BLUE=$(tput setaf 4)
WHITE=$(tput setaf 7)
BLACK=$(tput setaf 16)
BOLD=$(tput bold)
RESET=$(tput sgr0)

# Resource Variables
RESOURCE_GROUP=stripeeventsdemo
REGION_LOCATION=eastus
ACR_NAME=stripeeventsdemo
CONTAINER_APP_ENV=stripeeventsapp

if ! command -v az &> /dev/null && command -v docker &> /dev/null; then
    printf "%sBoth Docker and Azure CLI are needed to provision these resources%s" "$RED" "$RESET"
    [ "$PS1" ] && return || exit; #exit script without closing shell
fi

# register containerapp extension
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights

# create resource group
printf "%sCreating resource group %s in location %s...%s \n" "$BLUE" $RESOURCE_GROUP $REGION_LOCATION "$RESET"
az group create  --name $RESOURCE_GROUP  --location $REGION_LOCATION

# create container registry
printf "%sCreating Azure Container Registry %s in resource group %s...%s \n" "$BLUE" $ACR_NAME $RESOURCE_GROUP "$RESET"
az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME  --sku Basic  --admin-enabled true
az acr login --name $ACR_NAME

# build and publish images
printf "%sBuild and push container image => %s.azurecr.io/eventsapp:latest\n%s" "$BLUE" $ACR_NAME "$RESET"
docker build --tag $ACR_NAME.azurecr.io/eventsapp:latest -f ApiServer.Dockerfile .
docker push $ACR_NAME.azurecr.io/eventsapp:latest

printf "%sBuild and push container image => %s.azurecr.io/eventsorderprocessor:latest\n%s" "$BLUE" $ACR_NAME "$RESET"
docker build --tag stripeeventsdemo.azurecr.io/eventsorderprocessor:latest -f OrderProcessor.Dockerfile .
docker push $ACR_NAME.azurecr.io/eventsorderprocessor:latest

# create containerapps environment
print "%sCreate Container App Environment %s\n%s" "$BLUE" $CONTAINER_APP_ENV "$RESET"
az containerapp env create --name $CONTAINER_APP_ENV --resource-group $RESOURCE_GROUP --location $REGION_LOCATION

az containerapp env dapr-component set \
    --name $CONTAINER_APP_ENV --resource-group $RESOURCE_GROUP \
    --dapr-component-name rabbitmqbus --yaml rabbitpubsub.yaml

printf "%sDeploy container app %s to %s\n%s" "$BLUE" "eventsorderprocessor" $CONTAINER_APP_ENV "$RESET"
az containerapp create --name eventsorderprocessor --resource-group $RESOURCE_GROUP \
  --environment $CONTAINER_APP_ENV --image $ACR_NAME.azurecr.io/eventsorderprocessor \
  --target-port 5180 --ingress 'internal' --registry-server $ACR_NAME.azurecr.io \
  --min-replicas 1 --max-replicas 5  --env-vars ASPNETCORE_ENVIRONMENT=Container DOTNET_ENVIRONMENT=Container \
  --enable-dapr true --dapr-app-id orderproessor --dapr-app-port 5180 --dapr-app-protocol http

printf "%sDeploy container app %s to %s\n%s" "$BLUE" "eventsapp" $CONTAINER_APP_ENV "$RESET"
az containerapp create --name eventsapp --resource-group $RESOURCE_GROUP \
  --environment $CONTAINER_APP_ENV --image $ACR_NAME.azurecr.io/eventsapp \
  --target-port 5276 --ingress 'external' --registry-server $ACR_NAME.azurecr.io \
  --min-replicas 1 --max-replicas 3 --env-vars ASPNETCORE_ENVIRONMENT=Container DOTNET_ENVIRONMENT=Container \
  --enable-dapr true --dapr-app-id website --dapr-app-port 5276 --dapr-app-protocol http


