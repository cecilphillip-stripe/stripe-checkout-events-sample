# Events Storefront sample integration using Stripe Checkout

## What's in the box
This sample is split into a few  projects
* [StripeEventsCheckout.WebHost](src/StripeEventsCheckout.WebHost) - ASP.NET Core HTTP API backend that hosts endpoints for managing the checkout session and wehbooks
* [StripeEventsCheckout.BlazorUI/](src/StripeEventsCheckout.BlazorUI) - Frontend UI built with Blazor and Tailwind CSS.
* [StripeEventsCheckout.Tools](src/StripeEventsCheckout.Tools) - .NET CLI tool for seeding Stripe product data. (**Unneeded**. Kept just for reference)

## Requirements
* .NET SDK 7.0+ 
* [Stripe Account](https://dashboard.stripe.com/register)
* [Stripe CLI](https://stripe.com/docs/stripe-cli)


## Running the demo
### Step 1: Obtain your Stripe Keys 🕵🏽‍♂️
Before running the code, you'll need to retrieve your Stripe Secret Key from your account dashboard.
* Log in to your [Stripe Dashboard](https://dashboard.stripe.com/)
* Make sure you're in test mode. The toggle is located at the top right corner of the page.
* Click on the `Developers` button, then select `API Keys` in the left menu
* Under `Standard Keys`, reveal and copy your `Secret key`.

> You can learn more about API Keys and Modes at this link => https://stripe.com/docs/keys
* Update the `appsettings.json` file located in the root directory of [StripeEventsCheckout.WebHost](src/StripeEventsCheckout.WebHost) with your Stripe Publishable Key and Secret Key

```json
"Stripe": {
    "PublishableKey": "yourKey",
    "SecretKey": "yourKey",
    "WebhookSecrety": "yourSecret"
  }

```
or copy and rename the [.env.sample](./src/StripeEventsCheckout.WebHost/.env.example) to `.env` and fill out the fields

```dotenv
# Stripe keys
Stripe__PUublishableKey=yourKey
Stripe__SecretKey=yourKey
Stripe__WebhookSecret=yourSecret

```

### Step 2: Obtain your Stripe Webhook Key 🕵🏽‍♂️
Use the `stripe listen` command with the `--forward-to` flag to stream stripe events from your account in test mode to your local webhook endpoint.

```bash
stripe listen --forward-to localhost:5276/webhook
```

This command will return the webhook secret that you'll add to the `WebhookSecret` configuration key in your `appsettings.json` file.


### Step 3: Seeding data into the Stripe Dashboard
The `appsettings.json` file has a configuration property called `SeedProductData`. Set it to `true` to initiate seeding product and pricing data.

### Step 4: Run the code 👨🏽‍💻
Navigate into the src/ directory
```shell
cd  src/
```

Run the project
```shell
> dotnet build
> dotnet run --project StripeEventsCheckout.WebHost
```

By default, the application should start running on http://localhost:5276

#### [**Optional**] Run the application in Docker 👨🏽‍💻
The repo contains Dockerfiles and docker-compose files to quickly spin up the project running in containers on your local Docker instance.

> To run the containers locally, you'll need to have [Docker](https://www.docker.com/products/personal/) installed on your machine.

**File Listing**
* [Dockerfile.WebHost](./src/Dockerfile.WebHost) - The Dockerfile definition to build the Blazor frontend and API Backend.
* [docker-compose-app.yml](./docker-compose-app.yml) - Docker compose file to spin up the application.
* [docker-compose-infra.yml](./docker-compose-infra.yml) - Docker compose file to spin up some additional services.

| Service                                                     | Local Port(s) |
|:------------------------------------------------------------|:-------------:|
| [Stripe Events Web App](src/StripeEventsCheckout.WebHost) |     5276      |
| [Mongo DB](https://www.mongodb.com/try/download/community)  |     27017     |
| [Seq](https://datalust.co/seq)                              |     8191      |

**Run the containers**

Run the following command from the root directory of the project
```bash
> docker compose -f docker-compose-app.yml -f docker-compose-infra.yml up
```
## TODO
Replace Mongo with Cosmos DB