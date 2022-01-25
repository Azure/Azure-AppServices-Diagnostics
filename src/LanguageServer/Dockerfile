FROM node:8.16.0-stretch-slim

# Omnisharp
ENV OMNISHARP_VERSION 1.37.0
RUN curl -L -o omnisharp.tar.gz https://github.com/OmniSharp/omnisharp-roslyn/releases/download/v$OMNISHARP_VERSION/omnisharp-linux-x64.tar.gz
RUN curl -L -o dotnet.tar.gz https://download.visualstudio.microsoft.com/download/pr/d731f991-8e68-4c7c-8ea0-fad5605b077a/49497b5420eecbd905158d86d738af64/dotnet-sdk-3.1.100-linux-x64.tar.gz
RUN mkdir -p /opt/dotnet && tar -zxf dotnet.tar.gz -C /opt/dotnet
RUN mkdir -p /opt/omnisharp && tar -zxf omnisharp.tar.gz -C /opt/omnisharp

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libc6 \
        libgcc1 \
        libgssapi-krb5-2 \
        libicu57 \
        liblttng-ust0 \
        libssl1.0.2 \
        libstdc++6 \
        zlib1g \
        ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN ln -s /opt/dotnet/dotnet /usr/bin/dotnet
ENV DOTNET_RUNNING_IN_CONTAINER=true \
  NUGET_XMLDOC_MODE=skip \
  DOTNET_USE_POLLING_FILE_WATCHER=true
# Trigger first run experience by running arbitrary cmd to populate local package cache
RUN dotnet help

# Copy artifacts
RUN mkdir /workspace
RUN mkdir /workspace/customdlls
COPY workspace/customdlls /workspace/customdlls
COPY workspace/Solution.csproj /workspace/

WORKDIR /app
COPY package.json package.json
COPY src src
RUN npm install
EXPOSE 3000
# Entrypoint
CMD npm run start:ext
