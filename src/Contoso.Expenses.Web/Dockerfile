FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY ./Contoso.Common ./Contoso.Common/
COPY ./Contoso.Expenses.Web ./Contoso.Expenses.Web/

WORKDIR /app/Contoso.Expenses.Web

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/Contoso.Expenses.Web/out .

ENTRYPOINT ["dotnet", "Contoso.Expenses.Web.dll"]