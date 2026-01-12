# Dependencies & Packages

**Project File:** [Api.csproj](../Api.csproj)
**Framework:** .NET 9.0

## NuGet Packages

### Web & API
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
<PackageReference Include="Scalar.AspNetCore" Version="2.6.9" />
```
- **OpenAPI/Swagger** - API documentation
- **Scalar** - Modern API documentation UI (replaces Swagger UI)

### Database
```xml
<PackageReference Include="Npgsql" Version="9.0.3" />
<PackageReference Include="Dapper" Version="2.1.66" />
```
- **Npgsql** - PostgreSQL driver
- **Dapper** - Micro-ORM for fast database access

### Authentication & Security
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.3.0" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
```
- **JwtBearer** - JWT authentication middleware
- **System.IdentityModel.Tokens.Jwt** - JWT token handling
- **BCrypt.Net-Next** - Password hashing

### Data Processing
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="CsvHelper" Version="33.1.0" />
```
- **Newtonsoft.Json** - JSON serialization (legacy, System.Text.Json also used)
- **CsvHelper** - CSV export functionality

### Web Scraping & Automation
```xml
<PackageReference Include="Selenium.WebDriver" Version="4.33.0" />
<PackageReference Include="DotNetSeleniumExtras.WaitHelpers" Version="3.11.0" />
<PackageReference Include="HtmlAgilityPack" Version="1.12.2" />
```
- **Selenium WebDriver** - Browser automation for Google OAuth
- **WaitHelpers** - Selenium wait conditions
- **HtmlAgilityPack** - HTML parsing

### Utilities
```xml
<PackageReference Include="UtilityPack" Version="1.0.3" />
<PackageReference Include="Costura.Fody" Version="6.0.0" />
```
- **UtilityPack** - Custom utility library
- **Costura.Fody** - Embeds dependencies into assembly

## Built-in .NET Libraries

### Core
- **System.Text.Json** - Primary JSON serialization
- **System.Security.Cryptography** - Hashing, random generation
- **System.IdentityModel** - Claims-based identity

### ASP.NET Core
- **Microsoft.AspNetCore.Authentication**
- **Microsoft.AspNetCore.Authorization**
- **Microsoft.AspNetCore.Cors**
- **Microsoft.Extensions.DependencyInjection**
- **Microsoft.Extensions.Logging**
- **Microsoft.Extensions.Configuration**

## Key Features by Package

### Npgsql (PostgreSQL)
**Used for:**
- Connection pooling (5-20 connections)
- Async database operations
- JSONB support
- Array type mapping

**Connection String:**
```
Host=localhost;Database=leadhype_db;Username=abhaybhosale;
Password=smartlead123;Port=5432;SearchPath=public;
Pooling=true;MinPoolSize=5;MaxPoolSize=20;
ConnectionIdleLifetime=300;ConnectionPruningInterval=10;CommandTimeout=30
```

### Dapper (ORM)
**Used for:**
- Raw SQL queries with parameter binding
- Object mapping
- Multi-mapping (joins)
- Async operations

**Example:**
```csharp
using var connection = await _connectionService.GetConnectionAsync();
var campaigns = await connection.QueryAsync<Campaign>(
    "SELECT * FROM campaigns WHERE client_id = @ClientId",
    new { ClientId = clientId }
);
```

### BCrypt.Net-Next
**Used for:**
- Password hashing with salts
- Secure password verification
- Work factor: 10 (default)

**Example:**
```csharp
var hash = BCrypt.Net.BCrypt.HashPassword(password);
var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
```

### Selenium WebDriver
**Used for:**
- Google OAuth automation (GoogleOAuthService)
- Multilogin integration (TaskManager)
- Remote debugging via Chrome DevTools

**Configuration:**
- Remote debugging port from Multilogin
- Headless browser automation
- Wait for element strategies

### Scalar
**Used for:**
- API documentation UI at `/scalar/v1`
- Alternative to Swagger UI
- Theme: BluePlanet
- Configured in [ScalarConfiguration.cs](../Core/Startup/ScalarConfiguration.cs)

### CsvHelper
**Used for:**
- Campaign export to CSV
- Email account export to CSV
- Custom field mapping
- Large dataset streaming

## Package Versions

| Package | Version | Purpose |
|---------|---------|---------|
| .NET | 9.0 | Runtime |
| Npgsql | 9.0.3 | PostgreSQL driver |
| Dapper | 2.1.66 | ORM |
| JWT Bearer | 9.0.0 | Authentication |
| BCrypt.Net | 4.0.3 | Password hashing |
| Selenium | 4.33.0 | Browser automation |
| Scalar | 2.6.9 | API docs |
| CsvHelper | 33.1.0 | CSV export |

## Assembly Info

**[Api.csproj](../Api.csproj):**
```xml
<PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ApplicationIcon>../favicon.ico</ApplicationIcon>
    <AssemblyName>LeadHype.Api</AssemblyName>
    <RootNamespace>LeadHype.Api</RootNamespace>
    <Product>LeadHype API</Product>
</PropertyGroup>
```

## Implicit Usings

Enabled in project - common namespaces auto-imported:
- System
- System.Collections.Generic
- System.Linq
- System.Threading.Tasks
- Microsoft.AspNetCore.*

## Migration File Handling

```xml
<ItemGroup>
  <Content Include="Core\Database\Migrations\*.sql">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

SQL migration files copied to output directory

## Related Docs

- [10-middleware-security.md](10-middleware-security.md) - Authentication packages
- [04-database-schema.md](04-database-schema.md) - Npgsql/Dapper usage
- [08-smartlead-integration.md](08-smartlead-integration.md) - Selenium usage
