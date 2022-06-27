FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

# install packages
RUN apt-get update && apt-get install -y curl jq

ENV ASPNETCORE_URLS=http://*:5180
EXPOSE 5180

# Build Projects
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Order Processor
WORKDIR "/src/StripeEventsCheckout.OrderProcessor"
COPY ["./StripeEventsCheckout.OrderProcessor/StripeEventsCheckout.OrderProcessor.csproj", "./"]
RUN dotnet restore "StripeEventsCheckout.OrderProcessor.csproj"
COPY ["./StripeEventsCheckout.OrderProcessor/.", "./"]

# Build Single Project
RUN dotnet build "StripeEventsCheckout.OrderProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StripeEventsCheckout.OrderProcessor.csproj" -c Release -o /app/publish

# copy build to final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StripeEventsCheckout.OrderProcessor.dll"]