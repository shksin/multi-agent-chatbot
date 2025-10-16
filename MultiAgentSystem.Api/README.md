# Multi-Agent System API

## ?? Overview
A sophisticated AI-powered multi-agent system providing intelligent chat capabilities with user context awareness and RAG (Retrieval-Augmented Generation) functionality. The system includes authentication, mock banking APIs for testing, and comprehensive Swagger documentation.

## ?? Quick Start - Running Locally

### Prerequisites
Before running the application locally, ensure you have the following installed:

- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (recommended) or **Visual Studio Code**
- **Git** (for cloning the repository)

### Step 1: Clone and Navigate
```bash
git clone <repository-url>
cd MultiAgentSystem/MultiAgentSystem.Api
```

### Step 2: Restore Dependencies
```bash
dotnet restore
```

### Step 3: Build the Project
```bash
dotnet build
```

### Step 4: Run the Application
```bash
dotnet run
```

Or use Visual Studio:
1. Open `MultiAgentSystem.Api.sln` in Visual Studio
2. Set `MultiAgentSystem.Api` as the startup project
3. Press `F5` or click "Start Debugging"

### Step 5: Access the Application
Once running, the application will be available at:
- **API Base URL**: `https://localhost:58550`
- **Swagger UI**: `https://localhost:58550/api-docs`
- **Alternative HTTP URL**: `http://localhost:58551`

The browser should automatically open to the Swagger documentation page.

## ?? Configuration

### Default Configuration
The application comes with default configuration that works out-of-the-box for local development:

#### JWT Authentication
- **Secret Key**: Pre-configured for development
- **Token Expiry**: 60 minutes
- **Algorithm**: HS256

#### CORS Settings
- Allows requests from `http://localhost:3000` (React default)
- Allows requests from `http://localhost:5173` (Vite default)
- Configured for credentials and all headers/methods

#### Ports
- **HTTPS**: 58550
- **HTTP**: 58551

### Custom Configuration (Optional)
To customize settings, modify `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "YourCustomSecretKey",
    "ExpiryMinutes": 120
  },
  "AzureSearch": {
    "Endpoint": "https://your-search-service.search.windows.net",
    "IndexName": "your-index-name",
    "ApiKey": "your-api-key"
  }
}
```

## ?? API Documentation & Testing

### Swagger UI Features
The integrated Swagger UI provides:
- **Interactive Testing**: Test all endpoints directly from the browser
- **Authentication**: Built-in JWT token management
- **Request/Response Examples**: Complete API documentation
- **Model Validation**: Real-time validation feedback

### Accessing Documentation
1. Start the application (see Quick Start above)
2. Navigate to `https://localhost:58550/api-docs`
3. Explore and test the API endpoints

## ?? Authentication & Demo Users

### Demo User Accounts
The application includes pre-configured demo users for testing:

| Username | Password | Description | Use Case |
|----------|----------|-------------|----------|
| `admin` | `password123` | Administrator account | Full system access, premium features |
| `user1` | `pass123` | Standard user account | Regular banking customer |
| `demo` | `demo123` | Demo/trial account | Limited demo data |

### Authentication Flow
1. **Get Token**: POST to `/api/auth/login` with credentials
2. **Use Token**: Include `Authorization: Bearer <token>` in subsequent requests
3. **Swagger Integration**: Click "Authorize" button in Swagger UI

### Example Login Request
```bash
curl -X POST "https://localhost:58550/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "password123"
  }'
```

## ??? System Architecture

### Multi-Agent Components
- **Orchestrator Agent**: Coordinates between different AI agents
- **User Agent**: Handles user-specific data and context
- **RAG Agent**: Provides retrieval-augmented generation capabilities

### API Endpoints Overview

#### Authentication (`/api/auth`)
- `POST /login` - User authentication
- `POST /validate` - Token validation
- `GET /demo-users` - Available demo accounts

#### Chat System (`/api/chat`)
- `POST /query` - AI-powered chat queries
- `GET /health` - Service health check

#### Mock Banking API (`/api/mock-user`)
- `GET /profile` - User profile data
- `GET /accounts` - Bank account information
- `GET /transactions` - Transaction history
- `GET /cards` - Credit/debit card details
- `GET /loans` - Loan information
- `GET /investments` - Investment portfolios

## ?? Testing the Application

### 1. Health Check
```bash
curl https://localhost:58550/api/chat/health
```

### 2. Authentication Test
```bash
curl -X POST "https://localhost:58550/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "demo", "password": "demo123"}'
```

### 3. Chat Query Test
```bash
curl -X POST "https://localhost:58550/api/chat/query" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{"message": "What is my account balance?"}'
```

### 4. User Data Test
```bash
curl "https://localhost:58550/api/mock-user/accounts" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## ??? Development

### Project Structure
```
MultiAgentSystem.Api/
??? Agents/               # AI agent implementations
??? Controllers/          # API controllers
??? Services/            # Business logic services
??? Properties/          # Launch settings
??? appsettings.json     # Configuration
??? Program.cs          # Application setup
```

### Key Dependencies
- **Microsoft.SemanticKernel** - AI agent framework
- **Swashbuckle.AspNetCore** - Swagger/OpenAPI
- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT authentication
- **Azure.Search.Documents** - Azure Cognitive Search integration

### Building for Production
```bash
dotnet publish -c Release -o ./publish
```

## ?? Troubleshooting

### Common Issues

#### Port Already in Use
If ports 58550/58551 are in use:
1. Edit `launchSettings.json`
2. Change `applicationUrl` to different ports
3. Restart the application

#### Missing Dependencies
```bash
dotnet clean
dotnet restore
dotnet build
```

#### SSL Certificate Issues
For HTTPS development certificate:
```bash
dotnet dev-certs https --trust
```

#### Configuration Issues
- Ensure `appsettings.json` is properly formatted
- Check that all required configuration sections exist
- Verify JWT secret key is set

### Logs and Debugging
- Application logs are written to the console
- Use Visual Studio debugger for step-through debugging
- Check browser developer tools for API response details

## ?? Security Considerations

### Development vs Production
- **JWT Secret**: Change the default secret key for production
- **HTTPS**: Enable HTTPS redirect in production
- **CORS**: Restrict CORS origins for production use
- **API Keys**: Use secure key management for external services

### Best Practices
- Always use HTTPS in production
- Implement proper error handling
- Use secure token storage in client applications
- Regularly update dependencies

## ?? Support

### Getting Help
- Check the Swagger documentation at `/api-docs`
- Review application logs for error details
- Ensure all prerequisites are installed
- Verify network connectivity and firewall settings

### Development Tips
- Use the Swagger UI for interactive testing
- Monitor console output for detailed logging
- Test with different demo user accounts
- Use browser developer tools for debugging client issues

---

*Happy coding! ??*