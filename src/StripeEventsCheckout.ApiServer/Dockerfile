FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

# install packages
RUN apt-get update && apt-get install -y curl jq

ENV ASPNETCORE_URLS=http://*:5000
EXPOSE 5000

# run build 
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["StripeEventsCheckout.ApiServer.csproj", "./"]
RUN dotnet restore "./StripeEventsCheckout.ApiServer.csproj"
COPY . .
RUN dotnet build "StripeEventsCheckout.ApiServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StripeEventsCheckout.ApiServer.csproj" -c Release -o /app/publish

# copy build to final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StripeEventsCheckout.ApiServer.dll"]