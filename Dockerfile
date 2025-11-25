FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY Theo/Theo.csproj .
RUN dotnet restore Theo.csproj
COPY Theo/. .
RUN dotnet publish Theo.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Theo.dll", "token.txt", "insults.txt"]