# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the entire solution
dotnet build BetfairNG.sln

# Build specific projects
dotnet build BetfairNG.csproj                    # Main library (netstandard2.1)
dotnet build ConsoleExample/ConsoleExample.csproj # Example app (net6.0)

# Restore dependencies
dotnet restore BetfairNG.sln
```

## Project Structure

This is a C# client library for the Betfair API-NG and Exchange Streaming API.

### Core Projects

- **BetfairNG** (`BetfairNG.csproj`) - Main library containing:
  - `BetfairClient` - Async API client using Task Parallel Library
  - `BetfairClientSync` - Synchronous wrapper around BetfairClient
  - `MarketListener` - Reactive Extensions-based market polling and subscription
  - `Network` - HTTP JSON-RPC communication layer
  - `Data/` - API data types (MarketBook, Runner, Order, etc.)

- **BetfairNG.ESAClient** - Exchange Streaming API client:
  - `Client` - SSL socket client with auto-reconnect
  - `ClientCache` - Caches streaming market/order data
  - `Cache/` - Market and order snapshot types (MarketSnap, OrderMarketSnap)
  - `Protocol/` - Connection status and message handling

- **BetfairNG.ESASwagger** - Generated Swagger models for ESA streaming protocol

- **ConsoleExample** - Usage examples (OriginalExample, StreamingExample, PeriodicExample)

### Key Patterns

**API Client Usage:**
```csharp
BetfairClient client = new BetfairClient("APP_KEY");
client.Login("cert.p12", "certpass", "username", "password");
var markets = await client.ListMarketCatalogue(filter, projections, sort, maxResults);
```

**Reactive Market Subscriptions:**
```csharp
var listener = MarketListener.Create(client, priceProjection, connectionCount);
listener.SubscribeMarketBook(marketId)
    .Subscribe(tick => { /* handle market update */ });
```

**Streaming API:**
```csharp
var esaClient = new Client(hostname, port, sessionProvider);
esaClient.ChangeHandler = new ClientCache();
esaClient.Start();
esaClient.MarketSubscription(subscriptionMessage);
```

### Helper Classes

- `BFHelpers` - Market filter/projection builders, console formatting, market efficiency calculations
- `PriceHelpers` - Betfair price ladder operations (AddPip, SubtractPip, RoundToNearestBetfairPrice, SnapToLadder)
  - `PriceHelpers.Table` - Immutable array of all valid Betfair prices (1.01 to 1000)

### Dependencies

- Newtonsoft.Json - JSON serialization
- System.Reactive - Reactive Extensions for market streaming
- System.Collections.Immutable - Immutable collections for price ladder
