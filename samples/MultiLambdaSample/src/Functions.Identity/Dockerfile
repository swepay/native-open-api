# ── Build stage ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level files
COPY Native.OpenApi.slnx .

# Copy project files for restore
COPY src/Native.OpenApi/Native.OpenApi.csproj src/Native.OpenApi/
COPY src/NativeLambdaRouter.SourceGenerator.OpenApi/NativeLambdaRouter.SourceGenerator.OpenApi.csproj src/NativeLambdaRouter.SourceGenerator.OpenApi/
COPY samples/MultiLambdaSample/src/Functions.Identity/Functions.Identity.csproj samples/MultiLambdaSample/src/Functions.Identity/

RUN dotnet restore samples/MultiLambdaSample/src/Functions.Identity/Functions.Identity.csproj

# Copy all source
COPY src/ src/
COPY samples/MultiLambdaSample/src/Functions.Identity/ samples/MultiLambdaSample/src/Functions.Identity/

RUN dotnet publish samples/MultiLambdaSample/src/Functions.Identity/Functions.Identity.csproj \
    -c Release \
    -o /app/publish \
    -p:PublishAot=false

# ── Runtime stage ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

# Install Lambda RIE
ADD https://github.com/aws/aws-lambda-runtime-interface-emulator/releases/latest/download/aws-lambda-rie /usr/local/bin/aws-lambda-rie
RUN chmod +x /usr/local/bin/aws-lambda-rie

WORKDIR /var/task
COPY --from=build /app/publish .

ENTRYPOINT ["/usr/local/bin/aws-lambda-rie", "dotnet", "exec", "/var/task/bootstrap.dll"]
