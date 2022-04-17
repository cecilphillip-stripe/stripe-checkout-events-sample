FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

# install packages
RUN apt-get update && apt-get install -y curl jq

ENV ASPNETCORE_URLS=http://*:5276
EXPOSE 5276

# Build Projects
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Blazor Frontend
WORKDIR "/src/StripeEventsCheckout.BlazorUI"
COPY ["./StripeEventsCheckout.BlazorUI/StripeEventsCheckout.BlazorUI.csproj", "./"]
RUN dotnet restore "StripeEventsCheckout.BlazorUI.csproj"
COPY ["./StripeEventsCheckout.BlazorUI/.", "./"]

# API Server Backend
WORKDIR "/src/StripeEventsCheckout.ApiServer"
COPY ["./StripeEventsCheckout.ApiServer/StripeEventsCheckout.ApiServer.csproj", "./"]
RUN dotnet restore "StripeEventsCheckout.ApiServer.csproj"
COPY ["./StripeEventsCheckout.ApiServer/.", "./"]

# Build Single Project
RUN dotnet build "StripeEventsCheckout.ApiServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StripeEventsCheckout.ApiServer.csproj" -c Release -o /app/publish

# copy build to final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StripeEventsCheckout.ApiServer.dll"]