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
RESOURCE_GROUP=StripeEventsDemo
REGION_LOCATION=eastus
ACR_NAME=stripeeventsdemo
LA_WORKSPACE=stripeeventsdemo
CONTAINER_APP_ENV=stripeeventsapp

if ! command -v az &> /dev/null && command -v docker &> /dev/null; then
    printf "%sBoth Docker and Azure CLI are needed to provision these resources%s" "$RED" "$RESET"
    [ "$PS1" ] && return || exit; #exit script without closing shell
fi

if [ -f .env ]; then
  printf "%s.env file found. Loading environment variables%s" "$GREEN" "$RESET"
  export "$(echo $(cat .env | sed 's/#.*//g'| xargs) | envsubst)"
fi

# register containerapp extension
az extension add --name containerapp --upgrade
az extension add --name application-insights --upgrade
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights

deploy () {
    # create resource group
    printf "%sCreating resource group %s in location %s...%s \n" "$BLUE" $RESOURCE_GROUP $REGION_LOCATION "$RESET"
    az group create  --name $RESOURCE_GROUP  --location $REGION_LOCATION
    
    # create container registry
    printf "%sCreating Azure Container Registry %s in resource group %s...%s \n" "$BLUE" $ACR_NAME $RESOURCE_GROUP "$RESET"
    az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME  --sku Basic  --admin-enabled true
    az acr login --name $ACR_NAME
    
    # build and publish images
    printf "%sBuild and push container image => %s.azurecr.io/eventsapp:latest\n%s" "$BLUE" $ACR_NAME "$RESET"
    docker build --tag $ACR_NAME.azurecr.io/eventsapp:latest -f ../src/ApiServer.Dockerfile ../src/
    docker push $ACR_NAME.azurecr.io/eventsapp:latest
    
    printf "%sBuild and push container image => %s.azurecr.io/eventsorderprocessor:latest\n%s" "$BLUE" $ACR_NAME "$RESET"
    docker build --tag $ACR_NAME.azurecr.io/eventsorderprocessor:latest -f ../src/OrderProcessor.Dockerfile ../src/
    docker push $ACR_NAME.azurecr.io/eventsorderprocessor:latest
    
    # create log analytics workspace
    printf "%sCreating log-analytics workspace => %s \n%s" "$BLUE" $LA_WORKSPACE "$RESET"
    az monitor log-analytics workspace create --resource-group $RESOURCE_GROUP --location $REGION_LOCATION --workspace-name $LA_WORKSPACE
    LA_SHARED_KEY=$(az monitor log-analytics workspace get-shared-keys --resource-group $RESOURCE_GROUP --workspace-name $LA_WORKSPACE --query primarySharedKey --output tsv)
    LA_WORKSPACE_ID=$(az monitor log-analytics workspace show --resource-group $RESOURCE_GROUP --workspace-name $LA_WORKSPACE  -o tsv --query customerId)
    LA_WORKSPACE_RESOURCE_ID=$(az monitor log-analytics workspace show --resource-group $RESOURCE_GROUP --workspace-name $LA_WORKSPACE  -o tsv --query id)
    
    # create applications insights resource
    printf "%sCreating applications insights resource => %s \n%s" "$BLUE" $LA_WORKSPACE "$RESET"
    az monitor app-insights component create --app $LA_WORKSPACE --location $REGION_LOCATION --kind web --resource-group $RESOURCE_GROUP \
    --application-type web --workspace "$LA_WORKSPACE_RESOURCE_ID"
     AI_INSTRUMENTATION_KEY=$(az monitor app-insights component show --app $LA_WORKSPACE --resource-group $RESOURCE_GROUP -o tsv --query instrumentationKey)
    
    # create container apps environment
    printf "%sCreate Container App Environment %s\n%s" "$BLUE" $CONTAINER_APP_ENV "$RESET"
    az containerapp env create --name $CONTAINER_APP_ENV --resource-group $RESOURCE_GROUP --location $REGION_LOCATION \
    --logs-workspace-id "$LA_WORKSPACE_ID" --logs-workspace-key "$LA_SHARED_KEY"
    
    printf "%sAdding Rabbitmq DAPR component \n%s" "$BLUE" "$RESET"
    az containerapp env dapr-component set \
        --name $CONTAINER_APP_ENV --resource-group $RESOURCE_GROUP \
        --dapr-component-name rabbitmqbus --yaml rabbitpubsub.yaml
    
    ACR_PASSWORD=$(az acr credential show --name $ACR_NAME  --output tsv --query "passwords[0].value")
    
    printf "%sDeploy container app %s to %s\n%s" "$BLUE" "eventsorderprocessor" $CONTAINER_APP_ENV "$RESET"
    az containerapp create --name eventsorderprocessor --resource-group $RESOURCE_GROUP \
      --environment $CONTAINER_APP_ENV --image $ACR_NAME.azurecr.io/eventsorderprocessor \
      --target-port 5180 --ingress 'internal' --registry-server $ACR_NAME.azurecr.io \
      --registry-username $ACR_NAME --registry-password "$ACR_PASSWORD" \
      --min-replicas 1 --max-replicas 5  \
      --env-vars ASPNETCORE_ENVIRONMENT=Container DOTNET_ENVIRONMENT=Container APPINSIGHTS_INSTRUMENTATIONKEY="$AI_INSTRUMENTATION_KEY" \
      --enable-dapr true --dapr-app-id orderproessor --dapr-app-port 5180 --dapr-app-protocol http
    
    printf "%sDeploy container app %s to %s\n%s" "$BLUE" "eventsapp" $CONTAINER_APP_ENV "$RESET"
    az containerapp create --name eventsapp --resource-group $RESOURCE_GROUP \
      --environment $CONTAINER_APP_ENV --image $ACR_NAME.azurecr.io/eventsapp \
      --target-port 5276 --ingress 'external' --registry-server $ACR_NAME.azurecr.io \
      --registry-username $ACR_NAME --registry-password "$ACR_PASSWORD" \
      --min-replicas 1 --max-replicas 3 \
      --env-vars ASPNETCORE_ENVIRONMENT=Container DOTNET_ENVIRONMENT=Container APPINSIGHTS_INSTRUMENTATIONKEY="$AI_INSTRUMENTATION_KEY" \
      --enable-dapr true --dapr-app-id website --dapr-app-port 5276 --dapr-app-protocol http
}

update_containers() {    
    # build and publish images
    printf "%sBuild and push container image => %s.azurecr.io/eventsapp:latest \n%s" "$BLUE" $ACR_NAME "$RESET"
    docker build --tag $ACR_NAME.azurecr.io/eventsapp:latest -f ../src/ApiServer.Dockerfile ../src/
    docker push $ACR_NAME.azurecr.io/eventsapp:latest
    
    printf "%sBuild and push container image => %s.azurecr.io/eventsorderprocessor:latest \n%s" "$BLUE" $ACR_NAME "$RESET"
    docker build --tag $ACR_NAME.azurecr.io/eventsorderprocessor:latest -f ../src/OrderProcessor.Dockerfile ../src/
    docker push $ACR_NAME.azurecr.io/eventsorderprocessor:latest
    
    # Uncomment below to update container apps
    printf "%sUpdating container app %s on %s\n%s" "$BLUE" "eventsorderprocessor" $CONTAINER_APP_ENV "$RESET"
    az containerapp update -n eventsorderprocessor -g $RESOURCE_GROUP --image $ACR_NAME.azurecr.io/eventsorderprocessor:latest
    printf "%sUpdating container app %s on %s\n%s" "$BLUE" "eventsapp" $CONTAINER_APP_ENV "$RESET"
    az containerapp update -n eventsapp -g $RESOURCE_GROUP --image $ACR_NAME.azurecr.io/eventsapp:latest  
}

deploy
