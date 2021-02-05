FROM openfaas/of-watchdog:0.7.2 as watchdog

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Contoso.Expenses.OpenFaaS/Contoso.Expenses.OpenFaaS.csproj", "Contoso.Expenses.OpenFaaS/"]
COPY ["Contoso.Common/Contoso.Expenses.Common.csproj", "Contoso.Common/"]
RUN dotnet restore "Contoso.Expenses.OpenFaaS/Contoso.Expenses.OpenFaaS.csproj"
COPY . .
WORKDIR "/src/Contoso.Expenses.OpenFaaS"
RUN dotnet build "Contoso.Expenses.OpenFaaS.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Contoso.Expenses.OpenFaaS.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=watchdog /fwatchdog /usr/bin/fwatchdog
RUN chmod +x /usr/bin/fwatchdog

ENV fprocess="dotnet Contoso.Expenses.OpenFaaS.dll"
ENV cgi_headers="true"
ENV mode="http"
ENV upstream_url="http://127.0.0.1:80"

HEALTHCHECK --interval=3s CMD [ -e /tmp/.lock ] || exit 1

CMD ["fwatchdog"]