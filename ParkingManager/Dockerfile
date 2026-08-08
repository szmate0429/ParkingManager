
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


COPY ["ParkingManager.csproj", "./"]
RUN dotnet restore "ParkingManager.csproj"


COPY . .
RUN dotnet publish "ParkingManager.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .


ENTRYPOINT ["dotnet", "ParkingManager.dll"]