# Build the NetDaemon with build container
FROM mcr.microsoft.com/dotnet/core/sdk:3.1.200-alpine as build

# From buildx
ARG TARGETPLATFORM
ARG BUILDPLATFORM

# Add bash
RUN apk add  \
    bash

# Copy the source to docker container
COPY ./src/bot /tmp/bot

# Copy the build script for minimal single binary build
COPY docker/build.sh /tmp/build.sh
RUN chmod +x /tmp/build.sh

# Build the minimal single binary
RUN /bin/bash /tmp/build.sh

# Build the target container
FROM netdaemon/base

# Copy the built binaries and set execute permissions
COPY --from=build /bot/bin/publish /bot
RUN chmod +x /bot/bot

# Copy the S6 service scripts
COPY docker/rootfs/etc /etc

# Set default values of NetDaemon env
ENV \
    ALGOLIA_APIKEY=key \
    ALGOLIA_APPID=app_id \
    DISCORD_TOKEN=NOT_SET

ENTRYPOINT ["/init"]