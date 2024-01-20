FROM mcr.microsoft.com/dotnet/sdk:8.0 as builder
ARG TARGETPLATFORM
ARG BUILDPLATFORM

# Copy the source to docker container
COPY ./src /usr/src
RUN dotnet publish -c Release /usr/src/netdaemonbot.csproj -o "/publish"

FROM mcr.microsoft.com/dotnet/aspnet:8.0
# RUN mkdir /app
# Set the working directory to /app
WORKDIR /app

COPY --from=builder /publish .
# Start the application
ENTRYPOINT ["dotnet", "netdaemonbot.dll"]