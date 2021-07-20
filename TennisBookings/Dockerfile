FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app

COPY src/TennisBookings.ScoreProcessor/*.csproj ./src/TennisBookings.ScoreProcessor/
COPY src/TennisBookings.ResultsProcessing/*.csproj ./src/TennisBookings.ResultsProcessing/

RUN dotnet restore src/TennisBookings.ScoreProcessor/TennisBookings.ScoreProcessor.csproj
RUN dotnet restore src/TennisBookings.ResultsProcessing/TennisBookings.ResultsProcessing.csproj

COPY . .
RUN dotnet publish src/TennisBookings.ScoreProcessor/TennisBookings.ScoreProcessor.csproj -c Release -o out

#Runtime
FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "TennisBookings.ScoreProcessor.dll"]