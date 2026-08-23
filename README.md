# LLM Chat API

A focused .NET 8 ASP.NET Core Web API for learning how local large language models, embeddings, semantic search, and Retrieval-Augmented Generation (RAG) fit together.

The API uses [Ollama](https://ollama.com/) locally:

- `qwen2.5:3b` generates text responses.
- `nomic-embed-text` generates embeddings.
- Prompt engineering gives the text model a consistent audience and response focus.
- An in-memory vector store ranks documents using cosine similarity.

## Completed Features

### Text generation and prompt engineering

`LLMService` sends requests to Ollama's `/api/generate` endpoint through an injected `HttpClient`. Each prompt includes a system instruction for experienced software engineers, followed by the user's message. Responses are returned through the `ChatResponse` model.

### Embeddings

`IEmbeddingService` defines the embedding contract, and `EmbeddingService` calls Ollama's `/api/embeddings` endpoint with the `nomic-embed-text` model. The generated vector is returned as `float[]`.

### In-memory semantic search

`IVectorStore` defines document storage and search operations. `InMemoryVectorStore` stores `DocumentVector` objects in memory and calculates cosine similarity between a query embedding and each stored embedding. Results are ordered by descending similarity.

### Document ingestion

`DocumentIngestionService` uses `IEmbeddingService` to embed each document and then stores the resulting `DocumentVector` through `IVectorStore`.

### End-to-end RAG

`RAGService` coordinates the complete pipeline:

```text
Question -> Embedding -> Vector Search -> Retrieved Context -> Qwen -> Answer
```

It retrieves the top two matching documents, includes their content in a prompt with the question, and sends that prompt to `ILLMService`. When no documents are available, it returns `No relevant information was found.`

## API Endpoints

The default HTTP development URL is `http://localhost:5166`. Swagger is available at `/swagger` when running in the Development environment.

### `POST /api/chat`

Generate a direct answer from Qwen.

Request:

```json
{
	"message": "Explain dependency injection in ASP.NET Core."
}
```

### `POST /api/chat/embedding`

Generate an embedding for a plain string request body.

```json
"Explain dependency injection in ASP.NET Core."
```

### `POST /api/chat/ingest`

Ingests three built-in sample documents covering password reset, annual leave, and cafeteria opening hours. This endpoint does not require a request body.

### `POST /api/chat/search`

Embeds the question and returns the two most similar stored `DocumentVector` results.

Request:

```json
{
	"message": "When is the cafeteria open?"
}
```

### `POST /api/chat/rag`

Runs the complete RAG pipeline and returns a `ChatResponse`.

Request:

```json
{
	"message": "How do I reset my password?"
}
```

The in-memory store starts empty each time the API process starts. Call `/api/chat/ingest` before using `/api/chat/search` or `/api/chat/rag` with the sample data.

## Project Structure

```text
LLM-Chat-API/
├── README.md
└── src/
		└── LLMChat.Api/
				├── Controllers/
				│   └── ChatController.cs
				├── Models/
				│   ├── ChatRequest.cs
				│   ├── ChatResponse.cs
				│   ├── DocumentVector.cs
				│   ├── OllamaEmbeddingRequest.cs
				│   ├── OllamaEmbeddingResponse.cs
				│   ├── OllamaRequest.cs
				│   └── OllamaResponse.cs
				├── Services/
				│   ├── DocumentIngestionService.cs
				│   ├── EmbeddingService.cs
				│   ├── IEmbeddingService.cs
				│   ├── IDocumentIngestionService.cs
				│   ├── ILLMService.cs
				│   ├── IRAGService.cs
				│   ├── IVectorStore.cs
				│   ├── InMemoryVectorStore.cs
				│   ├── LLMService.cs
				│   └── RAGService.cs
				├── Program.cs
				└── LLMChat.Api.csproj
```

## Local Setup

### Prerequisites

- .NET 8 SDK
- Ollama

### 1. Install and start Ollama

Install Ollama for your operating system, then make sure the Ollama service is running. Pull the models used by this project:

```bash
ollama pull qwen2.5:3b
ollama pull nomic-embed-text
```

Ollama should be reachable at `http://localhost:11434`.

### 2. Verify Ollama

Text generation:

```bash
curl http://localhost:11434/api/generate -d '{
	"model": "qwen2.5:3b",
	"prompt": "Explain RAG in one sentence.",
	"stream": false
}'
```

Embeddings:

```bash
curl http://localhost:11434/api/embeddings -d '{
	"model": "nomic-embed-text",
	"prompt": "Explain RAG in one sentence."
}'
```

### 3. Run the API

From the repository root:

```bash
dotnet restore src/LLMChat.Api/LLMChat.Api.csproj
dotnet run --project src/LLMChat.Api/LLMChat.Api.csproj
```

Then open `http://localhost:5166/swagger` or call the endpoints directly.

## Future Roadmap

The following capabilities are planned learning milestones and are not implemented in the current project:

- SQLite persistence for documents and vectors
- Document chunking
- Metadata filtering
- Source citations in generated answers
- Retrieval and answer evaluation
- Streaming responses
- Tool and function calling
- AI agent orchestration
- Azure OpenAI integration
- Azure AI Search integration

## Scope

This project intentionally keeps the current implementation small and local. It is a learning and portfolio project focused on understanding the mechanics of an Ollama-backed RAG API before introducing durable storage, production-scale retrieval, and hosted model services.
