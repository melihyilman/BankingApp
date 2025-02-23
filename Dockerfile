FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore BankingApp.sln
RUN dotnet publish CustomerService/CustomerService.csproj -c Release -o /app/customer-out
RUN dotnet publish AccountService/AccountService.csproj -c Release -o /app/account-out
RUN dotnet publish NotificationService/NotificationService.csproj -c Release -o /app/notification-out
RUN dotnet publish RetryService/RetryService.csproj -c Release -o /app/retry-out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Dapr CLI'ı kur
RUN apt-get update && apt-get install -y wget ca-certificates \
    && wget -q https://raw.githubusercontent.com/dapr/cli/master/install/install.sh -O - | /bin/bash \
    && apt-get clean

COPY --from=build /app/customer-out ./customer
COPY --from=build /app/account-out ./account
COPY --from=build /app/notification-out ./notification
COPY --from=build /app/retry-out ./retry
COPY components ./components

ENV PATH="/root/.dapr/bin:${PATH}"