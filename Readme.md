# Events Storefront sample integration using Stripe Checkout

## What's in the box
This sample is split into a few  projects
* [StripeEventsCheckout.ApiServer](src/StripeEventsCheckout.ApiServer/) - ASP.NET Core HTTP API backend that hosts endpoints for managing the checkout session and wehbooks
* [StripeEventsCheckout.BlazorUI/](src/StripeEventsCheckout.BlazorUI/) - Frontend UI built with Blazor and Tailwind CSS.
* [StripeEventsCheckout.Tools](src/StripeEventsCheckout.Tools/) - .NET CLI tool for seeding Stripe product data

## Requirements
* .NET SDK 6.0+ 
* A FREE [Stripe Account](https://dashboard.stripe.com/register)
* [Stripe CLI](https://stripe.com/docs/stripe-cli)


## Running the demo
### Step1: Obtain your Stripe Keys 🕵🏽‍♂️
Before running the code, you'll need to retrieve your Stripe Secret Key from your account dashboard.
* Log in to your [Stripe Dashboard](https://dashboard.stripe.com/)
* Make sure you're in test mode. The toggle is located at the top right corner of the page.
* Click on the `Developers` button, then select `API Keys` in the left menu
* Under `Standard Keys`, reveal and copy your `Secret key`.

> You can learn more about API Keys and Modes at this link => https://stripe.com/docs/keys
* Copy and rename the `.env.sample` file to a `.env` in the root directory of [StripeEventsCheckout.ApiServer](src/StripeEventsCheckout.ApiServer/).
* Add your keys to the `.env` configuration file in the [SimpleCheckoutServer](src/SimpleCheckoutServer) project.

```bash
STRIPE__PUBLISHABLE_KEY=<YOUR_KEY>
STRIPE__SECRET_KEY=<YOUR_KEY>
STRIPE__WEBHOOK_SECRET=<YOUR_SECRET>
```

### Step2: Obtain your Stripe Webhook Key 🕵🏽‍♂️
Use the `stripe listen` command with the `--forward-to` flag to stream stripe events from your account in test mode to your local webhook endpoint.

```bash
stripe listen --forward-to localhost:5276/webhook
```

This command will return the webhook secret that you'll add to the `STRIPE__WEBHOOK_SECRET` configuration key in your `.env` file.

### Step3: Seeding data into the Stripe Dashboard
The [StripeEventsCheckout.Tools](src/StripeEventsCheckout.Tools/) project contains the source for a .NET ClI tool (`dotnet-striped`) that you can use to load product data into your Stripe account. There are a few way you can run this tool

The [nupkg](src/StripeEventsCheckout.Tools/nupkg/) contains the tool already bundled as a nuget package. To install and execute `dotnet-striped` as a [local tool](https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use), run the follow commands from the `src/` directory.

```shell
> dotnet tool install --add-source ./StripeEventsCheckout.Tools/nupkg dotnet-striped-tool
> dotnet striped status
```

If the tool is installed correctly and the Stripe API is accessible, you should see "**All services are online.**" printed to the terminal.

Next, populate the `STRIPE__SECRET_KEY` environment variable with your Stripe secret key. If you're using bash or zsh, the follow command will export the variables in your `.env` file to the current terminal session.

```shell
> set -o allexport; source .env; set +o allexport
```

To load the test data into your Stripe account, execute the following
```shell
> dotnet striped setup
```

### Step4: Run the code 👨🏽‍💻
Navigate into the src/ directory
```shell
cd  src/
```

Run the build. This will restore both the node and .NET packages.
```shell
dotnet build
```
Run the project
```shell
dotnet run --project StripeEventsCheckout.ApiServer
```

By default, the application should start running on http://localhost:5276

### [**Optional**] Run the application in Docker 👨🏽‍💻
The repo contains a Dockerfile and docker-compose files to quickly spin up the project running in containers on your local Docker instance.

> To run the containers locally, you'll need to have [Docker](https://www.docker.com/products/personal/) installed on your machine.

**File Listing**
* [Dockerfile.ApiServer](./src/Dockerfile.ApiServer) - The Dockerfile definition to build the Blazor frontend and API Backend.
* [docker-compose-app.yml](./docker-compose-app.yml) - Docker compose file to spin up the application.
* [docker-compose-infra.yml](./docker-compose-infra.yml) - Docker compose file to spin up some additional services.

| Service | Local Port(s) | 
| :-------------- | :---------: | 
| [Stripe Events Web App](src/StripeEventsCheckout.ApiServer/) |5276  |
| [Mongo DB](https://www.mongodb.com/try/download/community) | 27017 |
| [Seq](https://datalust.co/seq) | 8191 |

**Run the containers**

Run the following command from the root directory of the project
```bash
> docker compose -f docker-compose-app.yml -f docker-compose-infra.yml up
```
