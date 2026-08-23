# LLM Chat API

A .NET 8 Web API built for modern large language model (LLM) applications using clean architecture, dependency injection, secure configuration, and Azure-first deployment.

This project is designed to support conversational AI, prompt-driven workflows, document-aware retrieval, and agent-style orchestration with production-ready concerns such as logging, error handling, and configuration management.

## Features

- .NET 8 Web API
- Clean architecture
- Dependency Injection
- Configuration & secrets management
- LLM integration
- Prompt management
- Error handling
- Logging
- Conversation history
- Embeddings
- Vector search
- RAG (Retrieval-Augmented Generation)
- Tool calling
- AI agent orchestration
- Azure deployment

## Architecture Overview

The solution is organized around a clean separation of concerns:

- API layer for HTTP endpoints and controllers
- Application layer for orchestration, business logic, and use cases
- Domain layer for core entities, contracts, and shared models
- Infrastructure layer for LLM providers, storage, vector search, and external integrations
- Shared configuration and security components

## Core Capabilities

### 1. LLM Integration
Connect to model providers and orchestrate requests for chat, summarization, classification, tool execution, and retrieval tasks.

### 2. Prompt Management
Centralize prompt templates, versioning, and system/user message composition for reliable and maintainable AI workflows.

### 3. Conversation History
Track chat sessions and previous messages to enable context-aware conversations and multi-turn interactions.

### 4. Embeddings and Vector Search
Generate embeddings for text and use vector search to find the most relevant context for RAG-based responses.

### 5. RAG Pipeline
Combine retrieval with generative responses to ground answers in domain-specific knowledge and supporting documents.

### 6. Tool Calling and AI Agents
Support structured tool execution and multi-step AI agent flows where the model can decide to call functions or services.

### 7. Production Readiness
Include robust error handling, structured logging, secure configuration, and Azure deployment patterns.

## Suggested Project Structure

```text
LLM-Chat-API/
├── src/
│   ├── LLMChat.Api/
│   ├── LLMChat.Application/
│   ├── LLMChat.Domain/
│   ├── LLMChat.Infrastructure/
│   └── LLMChat.Shared/
├── tests/
│   ├── LLMChat.Api.Tests/
│   ├── LLMChat.Application.Tests/
│   └── LLMChat.Infrastructure.Tests/
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Production.json
├── .env.example
├── Dockerfile
├── docker-compose.yml
├── README.md
├── .gitignore
└── LLMChat.sln
```

## Configuration and Secrets

Use environment variables or managed secret stores for sensitive settings such as:

- Azure OpenAI endpoint and keys
- API keys for model providers
- Database and storage connection strings
- Vector store configuration
- Authentication and authorization settings

Best practice is to keep secrets out of source control and load them via configuration providers or Azure Key Vault.

## Azure Deployment

The API is intended to be deployable to Azure using services such as:

- Azure App Service
- Azure Container Apps
- Azure Kubernetes Service (AKS)
- Azure OpenAI
- Azure AI Search
- Azure Key Vault
- Azure Application Insights

## Getting Started

1. Install the .NET 8 SDK.
2. Restore dependencies.
3. Configure your app settings and secrets.
4. Run the API locally.
5. Test endpoints and validate model integrations.

Example commands:

```bash
dotnet restore
dotnet build
dotnet run --project src/LLMChat.Api
```

## License

This project is intended for learning, experimentation, and production extension. Add an appropriate license before deployment or distribution.

## Notes

This README reflects the intended architecture and feature set for the LLM chat API. As the solution evolves, update the documentation to match the concrete implementation and deployment configuration.
