FROM mcr.microsoft.com/dotnet/aspnet:7.0 as base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
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

ENTRYPOINT ["dotnet", "Contoso.Expenses.Function.dll"]