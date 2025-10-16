# Multi-Agent Chatbot System

A sophisticated multi-agent system built with .NET 8 and React that features an intelligent orchestrator coordinating between a RAG (Retrieval-Augmented Generation) agent and a User agent.

## 🏗️ Architecture

### Backend (.NET 8)
- **Orchestrator Agent**: Intelligently routes queries to appropriate agents based on authentication status
- **RAG Agent**: Queries Azure AI Search with hybrid search (semantic + keyword)
- **User Agent**: Retrieves personalized user data when authenticated
- **Authentication Service**: JWT-based authentication with demo accounts

### Frontend (React)
- Modern chatbot interface with real-time messaging
- Login/logout functionality
- Visual indicators for authentication status and active agents
- Responsive design with smooth animations

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v16 or higher)
- [Azure AI Search](https://azure.microsoft.com/services/search/) (optional - uses dummy data if not configured)

## 🚀 Quick Start

### 1. Backend Setup

Navigate to the API directory:
```powershell
cd MultiAgentSystem\MultiAgentSystem.Api
```

Restore dependencies:
```powershell
dotnet restore
```

Update `appsettings.json` with your Azure Search credentials (optional):
```json
{
  "AzureSearch": {
    "Endpoint": "https://your-search-service.search.windows.net",
    "IndexName": "your-index-name",
    "ApiKey": "your-api-key"
  }
}
```

Run the API:
```powershell
dotnet run
```

The API will start at `http://localhost:58550`

### 2. Frontend Setup

Navigate to the frontend directory:
```powershell
cd ..\chatbot-frontend
```

Install dependencies:
```powershell
npm install
```

Run the development server:
```powershell
npm start
```

The app will open at `http://localhost:3000`

## 🔐 Demo Accounts

The system includes three demo accounts for testing:

| Username | Password    | Description                    |
|----------|-------------|--------------------------------|
| admin    | password123 | Administrator with full access |
| user1    | pass123     | Standard user account          |
| demo     | demo123     | Demo/trial account             |

## 🎯 Features

### Multi-Agent System
- **Intelligent Routing**: Orchestrator decides which agents to invoke based on context
- **Parallel Processing**: Agents run concurrently for optimal performance
- **Hybrid Search**: RAG agent uses both semantic and keyword search for better results
- **Personalized Responses**: User agent provides account-specific information when authenticated

### Authentication
- JWT token-based authentication
- Secure bearer token transmission
- Persistent login state (localStorage)
- Token validation

### User Experience
- Clean, modern UI with gradient design
- Real-time message streaming
- Typing indicators
- Suggested questions for new users
- Agent attribution for transparency
- Markdown rendering for rich content

## 📚 API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/validate` - Token validation
- `GET /api/auth/demo-users` - Get demo user list

### Chat
- `POST /api/chat/query` - Send message to orchestrator
- `GET /api/chat/health` - Health check

## 🧪 Testing the System

### Without Authentication (RAG Agent Only)
1. Open the app without logging in
2. Try questions like:
   - "Tell me about your products"
   - "What support options are available?"
   - "How do I get started?"

### With Authentication (Both Agents)
1. Click "Login" and use any demo account
2. Try personalized questions:
   - "Show me my account information"
   - "What's my recent activity?"
   - "Display my usage statistics"

## 🔧 Configuration

### Backend Configuration (appsettings.json)

```json
{
  "Jwt": {
    "Key": "your-secret-key",
    "ExpiryMinutes": 60
  },
  "AzureSearch": {
    "Endpoint": "your-azure-search-endpoint",
    "IndexName": "your-index-name",
    "ApiKey": "your-api-key"
  },
  "UserApi": {
    "BaseUrl": "https://api.example.com",
    "Timeout": 30
  }
}
```

### Frontend Configuration (.env)

```env
REACT_APP_API_URL=http://localhost:58550/api
```

## 🏗️ Project Structure

```
MultiAgentSystem/
├── MultiAgentSystem.Api/          # .NET 8 Backend
│   ├── Agents/                    # Agent implementations
│   │   ├── RagAgent.cs           # Azure Search integration
│   │   ├── UserAgent.cs          # User data retrieval
│   │   └── OrchestratorAgent.cs  # Agent coordination
│   ├── Controllers/               # API controllers
│   │   ├── AuthController.cs     # Authentication
│   │   └── ChatController.cs     # Chat interface
│   ├── Services/                  # Business services
│   │   └── AuthService.cs        # Auth logic
│   └── Program.cs                 # App configuration
│
└── chatbot-frontend/              # React Frontend
    ├── public/                    # Static assets
    ├── src/
    │   ├── components/            # React components
    │   │   ├── ChatInterface.js  # Main chat UI
    │   │   └── LoginModal.js     # Login dialog
    │   ├── services/              # API services
    │   │   ├── authService.js    # Auth API calls
    │   │   └── chatService.js    # Chat API calls
    │   └── App.js                 # Root component
    └── package.json               # Dependencies
```

## 🔍 How It Works

### Agent Orchestration Flow

1. **User sends a message** through the React frontend
2. **Frontend determines** if user is authenticated and includes bearer token if available
3. **Orchestrator receives** the query and checks for authentication token
4. **Decision logic**:
   - Always calls RAG Agent to search knowledge base
   - Calls User Agent only if bearer token is present
5. **Agents execute** in parallel for optimal performance
6. **Orchestrator synthesizes** responses from all agents
7. **Response returned** to frontend with agent attribution

### RAG Agent - Hybrid Search

The RAG Agent uses Azure AI Search with hybrid search capabilities:
- **Semantic Search**: Understands context and meaning
- **Keyword Search**: Finds exact matches
- **Combined Results**: Best of both approaches for accurate results

### User Agent - Personalized Data

The User Agent provides personalized information:
- Extracts user identity from JWT token
- Retrieves user-specific data (profile, activity, usage)
- Returns contextual information based on query

## � Git Configuration

This project includes comprehensive .gitignore files to exclude build artifacts, dependencies, and sensitive files from version control:

### Root .gitignore

- Operating system files (`.DS_Store`, `Thumbs.db`)
- IDE and editor files (`.vscode/`, `.idea/`)
- Log files and temporary files
- Environment variables (`.env` files)
- Azure configuration files

### Backend (.NET) .gitignore

- Build outputs (`bin/`, `obj/`, `Debug/`, `Release/`)
- Visual Studio files (`.vs/`, `*.user`, `*.suo`)
- NuGet packages and cache
- Test results and coverage reports
- Publishing profiles and Azure settings

### Frontend (React) .gitignore

- Node modules (`node_modules/`)
- Build outputs (`build/`, `dist/`)
- Package manager files (`package-lock.json`, `yarn.lock`)
- Environment variables (`.env*`)
- Test coverage reports
- Editor and IDE files

### Key Files Protected

- **Secrets**: `appsettings.*.json`, `secrets.json`, `*.pfx`, `*.key`
- **Dependencies**: `node_modules/`, NuGet packages
- **Build Artifacts**: All compiled outputs and temporary files
- **IDE Settings**: Editor-specific configuration files

## �🛠️ Development

### Adding a New Agent

1. Create agent interface and implementation in `Agents/` folder
2. Register service in `Program.cs`:
   ```csharp
   builder.Services.AddSingleton<IMyAgent, MyAgent>();
   ```
3. Inject into OrchestratorAgent and add to orchestration logic

### Customizing Responses

- **RAG Agent**: Modify `GetDummyResponseAsync()` or configure Azure Search
- **User Agent**: Update `GetDummyUserDataAsync()` with your data structure
- **Orchestrator**: Adjust `SynthesizeResponse()` for response formatting

## 🐛 Troubleshooting

### API not starting
- Ensure .NET 8 SDK is installed
- Check if port 58550 is available
- Review error messages in console

### Frontend not connecting to API
- Verify API is running at `http://localhost:58550`
- Check CORS settings in `Program.cs`
- Confirm `.env` file has correct API URL

### Azure Search errors
- System works with dummy data if Azure Search is not configured
- Verify credentials in `appsettings.json`
- Check Azure Search service is accessible

## 📝 License

This project is provided as-is for demonstration purposes.

## 🤝 Contributing

Feel free to submit issues and enhancement requests!

## 📧 Support

For questions and support, please open an issue in the repository.

---

Built with ❤️ using .NET 8, React, and Azure AI Services
