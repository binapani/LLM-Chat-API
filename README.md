# LLM Chat API — Enterprise RAG

A .NET 8 ASP.NET Core Web API demonstrating an enterprise-style retrieval-augmented generation workflow using local AI services, embeddings, SQLite persistence, and structured observability.

This project is designed as a practical learning portfolio and prototype for understanding how a local RAG pipeline operates in practice. It combines C#, ASP.NET Core, Ollama, embeddings, semantic search, relevance validation, and performance logging in a single codebase.

## Project Overview

This project demonstrates a complete local RAG pipeline using:

- C#
- ASP.NET Core
- Ollama / Qwen
- Embeddings
- SQLite
- Entity Framework Core
- Vector similarity search
- Relevance validation
- Structured performance logging

The API ingests document content, embeds it, stores the vectors in SQLite, retrieves the closest matches for a user question, validates candidate relevance, and sends only grounded context to Qwen for final answer generation.

# AI Learning & Implementation Roadmap

This project follows a practical learning path:

Learn → Build → Test → Debug → Explain → Interview → Commit

The goal is not to create a collection of disconnected AI demos. This is a progressive implementation of an enterprise AI system, where each concept is learned, built in code, validated with real prompts, and then explained from an engineering and interview perspective.

## Phase 1 — LLM Fundamentals
Status: COMPLETED

Topics learned:
- LLM fundamentals
- Prompt engineering
- System prompts
- User prompts
- Context
- Ollama
- Qwen 2.5:3b
- Ollama /api/generate
- Ollama /api/chat
- Structured responses
- Tool-call concepts

## Phase 2 — RAG Fundamentals
Status: COMPLETED — AGENTIC RAG FOUNDATION

Implemented/learned:
- Document ingestion
- Text extraction
- Chunking
- Embeddings
- nomic-embed-text
- Vector storage
- Query embeddings
- Vector search
- Similarity scoring
- Top-K retrieval
- Similarity threshold filtering
- Reranking
- Context augmentation
- Grounded answer generation
- Hallucination mitigation
- RAG vs fine-tuning
- Agentic RAG using search_knowledge_base

Conceptual pipeline:

```text
Documents
  ↓
Text extraction
  ↓
Chunking
  ↓
Embeddings
  ↓
Vector storage
  ↓
User question
  ↓
Query embedding
  ↓
Vector search
  ↓
Similarity filtering
  ↓
Reranking
  ↓
Context
  ↓
LLM
  ↓
Answer
```

## Phase 3 — Retrieval Engineering
Status: NEXT / IN PROGRESS

Topics to learn and implement in this order:

1. Keyword/BM25 search
2. Dense/vector search vs sparse/keyword search
3. Hybrid search
4. Fixed-size chunking
5. Semantic chunking
6. Chunk overlap and chunk-size trade-offs
7. Parent-child retrieval
8. Metadata filtering
9. Query rewriting
10. Multi-query retrieval
11. Query decomposition
12. Context compression
13. Lost-in-the-middle problem

Principle:

Dense retrieval → semantic meaning
Sparse/BM25 → exact keyword matching
Hybrid retrieval → combines both

## Phase 4 — Agentic AI
Status: COMPLETED — AGENT TOOLING AND ORCHESTRATION

Implemented:
- AgentService
- OllamaAgentService
- Agent tool definitions
- Tool calling
- Tool execution
- search_knowledge_base
- calculate
- Agent loop
- Maximum iteration guard
- Tool orchestration
- Parallel tool execution
- Task.WhenAll
- Tool validation
- Unknown-tool handling
- Error handling
- Cancellation support
- Logging/observability

Architecture:

```text
User
 ↓
Agent / Qwen
 ↓
Tool selection
 ├── search_knowledge_base
 │       ↓
 │      RAG
 │
 └── calculate
         ↓
      Calculator
 ↓
Tool results
 ↓
Qwen
 ↓
Final answer
```

Orchestration model:

- Agent = decision maker
- Tools = capabilities
- Orchestration = coordinating the capabilities

Important distinction:

- Independent tools → can execute in parallel
- Dependent tools → should execute sequentially

## Phase 5 — Agent Memory
Status: COMPLETED — SHORT-TERM + PERSISTENT CONVERSATION MEMORY

Implemented:
- IConversationMemoryService abstraction
- ConversationMemoryService
- EfConversationMemoryService
- ConversationSessionEntity
- ConversationMessageEntity
- EF Core persistence
- SQLite persistence
- ConversationSessions table
- ConversationMessages table
- One-to-many session/message relationship
- SessionId unique constraint/index
- SequenceNumber for deterministic message ordering
- ToolCallsJson persistence with System.Text.Json
- Session isolation
- GetMessagesAsync
- AddMessagesAsync
- ReplaceMessagesAsync
- ClearAsync
- Per-session conversation history
- Database-backed conversation history
- Conversation persistence across API restart
- Memory logging

Demonstrated:

Session A:
"My name is Alex."
→ later:
"What is my name?"
→ "Your name is Alex."

Session B:
"What is my name?"
→ does not know Alex from Session A.

Persistence validation:
- Same-session memory recall — PASSED
- Different-session isolation — PASSED
- Persistence after API restart — PASSED
- RAG question — PASSED
- Calculator question — PASSED
- Multi-tool request using knowledge-base search + calculator — PASSED

Key distinction:

RAG:
"What does the company know?"

Conversation memory:
"What happened earlier in this conversation?"

```text
User
 ↓
AgentService
 ↓
IConversationMemoryService
 ↓
EfConversationMemoryService
 ↓
VectorDbContext
 ↓
SQLite
 ↓
ConversationSessions
 ↓
ConversationMessages
```

Important: Long-term semantic memory, summarization, and persistent semantic memory remain future work and are not implemented here.

## Phase 6 — Advanced RAG
Status: PLANNED

Topics:
- CRAG
- Self-RAG
- Graph RAG
- Multimodal RAG

Do not claim these are implemented.

## Phase 7 — Advanced Agentic AI
Status: PLANNED

Topics:
- Agent planning
- Reflection
- Retry and recovery
- Tool failure handling
- Human-in-the-loop
- Multi-agent systems
- Agent evaluation
- Agent guardrails

Principle:
Use multi-agent architecture only when it provides a real architectural benefit; do not introduce it just because it is a current AI trend.

## Phase 8 — LLM Engineering
Status: PLANNED

Topics:
- Structured output
- JSON schema
- Function calling
- Streaming
- Token management
- Context windows
- Prompt optimization
- Temperature/top-p concepts
- Caching
- Semantic caching
- Batch inference
- Latency optimization

## Phase 9 — AI / RAG Evaluation
Status: PLANNED

Topics:
- RAG evaluation
- Faithfulness
- Answer relevance
- Context relevance
- Retrieval quality
- RAGAS
- TruLens
- Ground-truth evaluation
- LLM-as-judge
- Retrieval metrics
- Regression testing

## Phase 10 — Enterprise AI Architecture
Status: PLANNED

Topics:
- API gateway
- Authentication
- Authorization
- RBAC
- Multi-tenancy
- Data isolation
- Secrets management
- PII/security considerations
- Rate limiting
- Observability
- Cost management
- Reliability
- Disaster recovery

Target architecture conceptually:

```text
Client
 ↓
API Gateway
 ↓
AI Application
 ├── Agent
 ├── RAG
 ├── Memory
 └── Tools
 ↓
LLM / Model layer
 ↓
Enterprise data/services
```

## Phase 11 — Azure AI
Status: PLANNED

Map the local implementation to Azure concepts:

Ollama → Azure OpenAI
Local vector store → Azure AI Search
ASP.NET Core local API → Azure App Service
Local secrets → Azure Key Vault
Application logs → Application Insights
Authentication → Microsoft Entra ID

Also cover:
- Enterprise Azure architecture
- Security
- Scalability
- Monitoring
- Cost

Do not claim Azure migration has already been implemented.

## Phase 12 — LLMOps / Production AI
Status: PLANNED

Topics:
- Model/version management
- Prompt versioning
- Evaluation pipelines
- CI/CD
- Monitoring
- Cost monitoring
- Latency monitoring
- Model drift
- Feedback loops
- Production incident handling
- Continuous improvement

# Current Implementation Status

A concise implementation checklist:

- LLM integration — COMPLETED
- Ollama integration — COMPLETED
- Qwen 2.5:3b — COMPLETED
- nomic-embed-text — COMPLETED
- LLM generation — COMPLETED
- Embedding generation — COMPLETED
- Document ingestion — COMPLETED
- Chunking — COMPLETED
- Vector storage — COMPLETED
- Vector search — COMPLETED
- Similarity filtering — COMPLETED
- Relevance validation — COMPLETED
- Hybrid reranking — COMPLETED
- File upload — COMPLETED
- TXT/MD/CSV extraction — COMPLETED
- PDF extraction — COMPLETED
- DOCX extraction — COMPLETED
- Document metadata persistence — COMPLETED
- RAG pipeline — COMPLETED
- AgentService — COMPLETED
- OllamaAgentService — COMPLETED
- Tool definitions — COMPLETED
- Tool calling — COMPLETED
- search_knowledge_base — COMPLETED
- calculate — COMPLETED
- Agent loop — COMPLETED
- Maximum iteration guard — COMPLETED
- Tool validation — COMPLETED
- Unknown-tool handling — COMPLETED
- Error handling — COMPLETED
- Cancellation — COMPLETED
- Logging/observability — COMPLETED
- Multi-tool orchestration — COMPLETED
- Parallel tool execution — COMPLETED
- Session-based conversation memory — COMPLETED
- Session isolation — COMPLETED
- Per-session synchronization — COMPLETED
- Conversation history passed to the agent — COMPLETED
- Persistent Conversation Memory — COMPLETED
- Database-backed conversation history — COMPLETED
- Conversation persistence across API restart — COMPLETED
- BM25 — NEXT
- Hybrid search — NEXT
- Persistent semantic/long-term memory — PLANNED
- Conversation summarization — PLANNED
- Advanced RAG — PLANNED
- Multi-agent systems — PLANNED
- RAGAS — PLANNED
- LLM evaluation — PLANNED
- Azure OpenAI — PLANNED
- Azure AI Search — PLANNED
- LLMOps — PLANNED

## Persistent Conversation Memory — COMPLETED

Implemented:
- IConversationMemoryService abstraction
- EfConversationMemoryService
- ConversationSessionEntity
- ConversationMessageEntity
- EF Core persistence
- SQLite persistence
- ConversationSessions table
- ConversationMessages table
- One-to-many session/message relationship
- SessionId unique constraint/index
- SequenceNumber for deterministic message ordering
- ToolCallsJson persistence using System.Text.Json
- Session isolation
- GetMessagesAsync
- AddMessagesAsync
- ReplaceMessagesAsync
- ClearAsync
- Database-backed conversation history
- Conversation persistence across API restart

Architecture:

```text
User
 ↓
AgentService
 ↓
IConversationMemoryService
 ↓
EfConversationMemoryService
 ↓
VectorDbContext
 ↓
SQLite
 ↓
ConversationSessions
 ↓
ConversationMessages
```

Validated tests:
1. Same-session memory recall — PASSED
2. Different-session isolation — PASSED
3. Persistence after API restart — PASSED
4. RAG question — PASSED
5. Calculator question — PASSED
6. Multi-tool request using knowledge-base search + calculator — PASSED

# Learning Philosophy

This project is intentionally implementation-driven. Each AI concept is first understood conceptually, then implemented in C#, tested with real requests, debugged using logs, reviewed from an architecture perspective, and converted into an interview-ready explanation.

The objective is not only to use AI frameworks but to understand:
- why a component exists
- when to use it
- when not to use it
- trade-offs
- failure modes
- scalability
- latency
- security
- production considerations

# Interview Preparation

Every major implementation in this project is also used to prepare for Senior GenAI / AI Architect interviews.

Example questions:

- Explain the end-to-end RAG workflow.
- RAG vs fine-tuning?
- Dense vs sparse vs hybrid retrieval?
- How do you choose chunk size?
- What is reranking?
- How do you reduce RAG latency?
- What is agent orchestration?
- How does tool calling work?
- How do you safely execute multiple tools in parallel?
- How do you implement conversation memory?
- How do you isolate memory between users?
- How would you make memory persistent?
- How do you prevent hallucinations?
- How do you evaluate RAG quality?
- How would you design this system for Azure?

# Current Next Step

Retrieval Engineering — BM25 and Hybrid Search

We currently rely primarily on dense/vector retrieval. The next milestone is to understand sparse keyword retrieval using BM25 and then combine dense + sparse retrieval into hybrid search.

```text
Dense Retrieval
      ↓
BM25 / Sparse Retrieval
      ↓
Hybrid Retrieval
      ↓
Reranking
      ↓
High-quality enterprise retrieval
```

This is the next learning step for improving retrieval quality before moving further into more advanced RAG and agent capabilities.

## Current Architecture

```text
User Question
    ↓
Embedding Generation
    ↓
SQLite Vector Search
    ↓
Top-K Retrieval
    ↓
Similarity Threshold
    ↓
Relevance Validation
    ↓
Relevant Context
    ↓
Grounded Prompt
    ↓
Qwen
    ↓
Answer
```

### Stage responsibilities

1. Embedding Generation
   - Converts the user question into an embedding using the configured embedding model.
   - This produces a vector representation suitable for semantic similarity search.

2. SQLite Vector Search
   - Compares the query embedding to previously stored document embeddings.
   - Uses cosine similarity to rank chunks by semantic proximity.

3. Top-K Retrieval
   - Fetches a bounded set of candidate chunks based on the configured retrieval size.
   - This keeps the retrieval stage tractable before applying further filtering.

4. Similarity Threshold
   - Applies `Rag:MinimumSimilarity` to eliminate weak matches before deeper validation.
   - This reduces noise and avoids sending irrelevant chunks downstream.

5. Relevance Validation
   - Invokes the Qwen-based relevance classifier for the strongest candidate chunks.
   - Keeps only candidates that can directly answer the user's question.

6. Relevant Context
   - Builds the final context from only those document chunks classified as relevant.

7. Grounded Prompt
   - Sends the question and filtered context to the generation model.
   - The model is instructed to answer only from the supplied context.

8. Qwen Answer Generation
   - Produces the final answer when grounded context exists.

## Document Ingestion

Documents are ingested as:

```text
(Source, Content)
```

The current ingestion pipeline performs the following steps:

- Generates a `DocumentId`
- Splits content into chunks using a size of 500 characters and an overlap of 50 characters
- Generates embeddings for each chunk
- Stores `DocumentId`, `ChunkId`, `Source`, `Content`, and `Embedding` in SQLite

The current sample sources are:

- `password-policy`
- `annual-leave-policy`
- `cafeteria-policy`

Example ingestion behavior:

- each source gets a unique document identifier
- each document is chunked deterministically
- each chunk is embedded and stored as a searchable vector

## Vector Search

The vector search layer uses SQLite as the persistence layer and stores embeddings alongside chunk metadata.

### Behavior

- Embeddings are stored in SQLite
- Search calculates cosine similarity between the query embedding and the stored embeddings
- Results are ordered by descending similarity
- Top-K candidates are retrieved
- `Rag:MinimumSimilarity` is used as a configurable threshold

Current configuration:

```json
"Rag": {
  "MinimumSimilarity": 0.50,
  "RetrievalTopK": 5,
  "RelevanceTopK": 3
}
```

This configuration reflects the current lightweight prototype approach and is meant to be tuned with evaluation data instead of assumed to be universally correct.

## Important Retrieval Lesson

A false-positive retrieval test highlighted why similarity alone is not enough.

### Example false-positive case

Question:

```text
What is the maternity leave policy?
```

The vector search returned:

- `annual-leave-policy`
- similarity = `0.6246655`

This demonstrated that cosine similarity can surface a content item that is related to the broader domain but still does not directly answer the question. In other words, the vector search identified a candidate with a strong similarity score, but the document was not actually relevant to the question being asked.

Therefore, a similarity threshold alone is not sufficient for reliable RAG. Retrieval quality must be checked again before building the final context.

## Relevance Validation

The project includes a dedicated `IRelevanceService` and `RelevanceService`.

After vector retrieval, the strongest candidates are checked with Qwen using a strict YES/NO relevance classification. The sequence is:

```text
Vector Search
    ↓
Top candidates
    ↓
RelevanceService
    ↓
YES → keep
NO → discard
```

The relevance validation prompt asks whether the supplied context contains information that can directly help answer the question, and the model is constrained to respond with only:

- `YES`
- `NO`

If no candidate is relevant, the API returns:

```text
The information is not available in the provided documents.
```

## RAG Test Results

These tests were verified during the project’s retrieval validation work.

### Test 1

Question:

```text
How many days of annual leave do full-time employees receive?
```

Result:

```text
Full-time employees receive twenty-five days of annual leave per year.
```

### Test 2

Question:

```text
What is the maternity leave policy?
```

Result:

```text
The information is not available in the provided documents.
```

These results demonstrate both:

- successful grounded retrieval when the answer is present
- rejection of an irrelevant retrieved document when it does not actually answer the question

## Performance Monitoring

The current `RAGService` uses:

- `ILogger<RAGService>`
- `Stopwatch`

The pipeline tracks these metrics at Information level:

- `TotalMs`
- `EmbeddingMs`
- `VectorSearchMs`
- `RelevanceValidationMs`
- `FinalLlmMs`
- `RetrievalTopK`
- `RelevanceTopK`
- `CandidatesRetrieved`
- `CandidatesAfterSimilarityFilter`
- `CandidatesValidated`
- `RelevantCandidates`

These metrics are intended to support performance measurement and retrieval analysis, not to imply that a pipeline is optimal without comparative measurement.

## Performance Experiment

The project recorded a measured baseline and a concurrent relevance-validation experiment.

### Before concurrent relevance validation

```text
TotalMs = 51519
EmbeddingMs = 3508
VectorSearchMs = 1145
RelevanceValidationMs = 35476
FinalLlmMs = 11388
CandidatesValidated = 3
```

### After Task.WhenAll concurrent relevance validation

```text
TotalMs = 48154
EmbeddingMs = 3473
VectorSearchMs = 874
RelevanceValidationMs = 33387
FinalLlmMs = 10417
CandidatesValidated = 3
```

This run showed approximately a 6.5% reduction in total latency:

```text
(51519 - 48154) / 51519 ≈ 0.065
```

This means the total request latency dropped by roughly 6.5% in this measured run.

### Important note on concurrency

This does not prove that `Task.WhenAll` guarantees parallel model inference. It only shows that application-level concurrency reduced the wall-clock time of the relevance-validation phase in the local environment. The local Ollama/runtime configuration may still impose a bottleneck on concurrent inference, so the LLM itself may remain the dominant limiting factor.

## Architectural Lessons

The project surfaced several practical lessons:

1. Vector similarity is retrieval, not proof of relevance.
2. RAG requires retrieval-quality evaluation.
3. Metadata improves observability and enables future filtering strategies.
4. Expensive LLM calls should be minimized.
5. Performance must be measured rather than assumed.
6. Application-level concurrency does not guarantee model-level parallelism.
7. Production RAG should balance accuracy, latency, cost, security, and scalability.

## Current RAG Pipeline

The current pipeline is:

1. Generate query embedding
2. Retrieve up to `RetrievalTopK` candidates
3. Apply `MinimumSimilarity`
4. Sort by similarity
5. Keep up to `RelevanceTopK` candidates
6. Validate relevance
7. Build grounded context
8. Generate final answer with Qwen

## Hybrid Reranking

A lightweight deterministic `HybridReranker` was introduced between vector retrieval and final generation.

The current scoring formula is:

```text
combinedScore =
    (semanticSimilarity * 0.70)
    +
    (keywordOverlap * 0.30)
```

This reranker is designed to improve the ordering of retrieved candidates before the final grounded prompt is assembled.

- Semantic similarity provides the embedding-based relevance signal.
- Keyword overlap provides a lexical relevance signal.
- The combined score is used to reorder retrieved candidates.
- The reranker does not call an LLM.
- The reranker is intended to be inexpensive compared with LLM-based relevance validation.

## RerankedResult

The reranking step now returns a richer result structure built from:

- `VectorSearchResult`
- `Score`

Each reranked item preserves the original vector-search result and adds a ranking heuristic score. This score is a ranking/confidence heuristic and is NOT a calibrated probability.

## Reranker Performance

The lightweight reranker itself is very fast compared with the LLM stages.

Measured reranking latency from the experiment:

- Annual leave test: approximately 6–7 ms
- Maternity leave test: approximately 0 ms in the recorded run

These timings show that the reranker is inexpensive relative to final generation and LLM-based validation, making it a practical candidate for a cheaper second-stage ranking step.

## Reranker Score Experiment

The following measured scores were observed during the reranker experiment:

| Question | Top Reranker Score | Expected |
|---|---:|---|
| How many days of annual leave do full-time employees receive? | 0.88342714 | Relevant |
| What is the maternity leave policy? | 0.63726586 | Not available |

These two results suggest a possible separation between relevant and irrelevant queries, but they are NOT sufficient to establish a production confidence threshold.

The important caveat is that reranker scores are heuristic ranking scores rather than calibrated probabilities. A high reranker score indicates that a candidate ranks strongly under the current lexical-plus-semantic formula, but it does not mean the score has been validated as a statistically meaningful confidence measure.

## Confidence Gate — Next Experiment

This is the next milestone for a future confidence threshold experiment:

```text
Question
   ↓
Embedding
   ↓
Vector Search
   ↓
Similarity Threshold
   ↓
Hybrid Reranker
   ↓
Confidence Threshold
      /        \
    LOW        HIGH
     ↓           ↓
No Context     Final Qwen
```

This future experiment would test whether a lightweight confidence gate can separate clearly answerable questions from unanswerable ones before the final generation stage. It is intentionally a future milestone and not a production claim.

## Historical Reranking Milestone

The current relevance-validation approach uses a generative LLM multiple times and is therefore expensive. This remains a valid prototype design, but it is not the most efficient production approach.

### Current flow

```text
Vector Search
    ↓
Qwen relevance check × up to 3
    ↓
Final Qwen
```

### Target flow

```text
Vector Search
    ↓
Lightweight Reranker
    ↓
Relevant candidates
    ↓
Final Qwen
```

This was an earlier milestone in the project learning path and helped validate the value of a lightweight reranking stage. It is not the current next milestone. The concepts are:

- Retriever = finds possible candidates
- Reranker = determines which candidates are most relevant
- Generator = produces the final answer

This was explored as a learning step and remains a useful historical reference, but the active next milestone is retrieval engineering focused on BM25 and hybrid search.

## API Endpoints

The default development URL is `http://localhost:5166`. Swagger is available at `/swagger` when running in the Development environment.

### `POST /api/chat`

Generates a direct answer from Qwen using a prompt-only request.

### `POST /api/chat/embedding`

Generates an embedding for the supplied text.

### `POST /api/chat/ingest`

Ingests the current sample policy documents, chunks and embeds them, and stores the metadata and vectors in SQLite.

### `POST /api/chat/search`

Embeds the question and returns the top matching `DocumentVector` results using cosine similarity.

### `POST /api/chat/accurate-search`

Embeds the question, retrieves up to the configured Top-K candidates, applies `Rag:MinimumSimilarity`, and returns the relevant metadata plus the content.

### `POST /api/chat/rag`

Runs the complete retrieval and generation pipeline using the embedded search, relevance validation, grounded prompt, and Qwen answer generation.

## Interview Talking Points

These are concise senior/architect-level talking points suitable for a portfolio discussion:

- "I identified a false-positive retrieval case where cosine similarity returned a semantically related but incorrect document."
- "I introduced second-stage relevance validation to prevent irrelevant context from reaching the final generation step."
- "I instrumented the RAG pipeline with structured latency metrics."
- "I measured the impact of concurrent relevance validation rather than assuming it improved performance."
- "The measurements showed that LLM relevance validation remained the dominant bottleneck, leading to the architectural decision to investigate dedicated reranking."

## Local Setup

### Prerequisites

- .NET 8 SDK
- Ollama
- SQLite tooling is optional for inspection; the project uses EF Core and the SQLite database file directly

### 1. Install and start Ollama

Install Ollama for your operating system, then start the service.

```bash
ollama pull qwen2.5:3b
ollama pull nomic-embed-text
```

### 2. Verify the models

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

Then open the Swagger UI or call the endpoints directly.

### 4. SQLite persistence

The application uses SQLite for vector persistence, with `VectorDbContext` configured against `vectors.db`.

The database stores chunk metadata and JSON-serialized embedding arrays. The current project uses EF Core for persistence and supports restartable local retrieval across application restarts.

## Technology Summary

This project demonstrates several core engineering patterns:

- Dependency injection and service abstractions
- `HttpClient` integration for Ollama endpoints
- Local LLM generation with Qwen
- Embedding generation with `nomic-embed-text`
- Semantic retrieval using cosine similarity
- Character-based chunking with overlap
- SQLite persistence with EF Core
- Retrieval filtering and relevance validation
- Structured observability and latency tracking
- Prototype RAG architecture for learning and portfolio demonstration

## Scope

This project intentionally keeps the implementation small, local, and educational. It is a learning-focused portfolio project designed to make the mechanics of a local Ollama-backed RAG API understandable before introducing more advanced retrieval, evaluation, reranking, and production-scale architecture.

## Real Document Upload & Ingestion

The project now supports uploading a real document through an HTTP API instead of relying only on hardcoded documents.

### New endpoint

`POST /api/Documents/upload`

The endpoint accepts an uploaded document and sends it through the ingestion pipeline.

### Architecture

```text
File Upload
    ↓
DocumentsController
    ↓
IDocumentTextExtractor
    ↓
PlainTextDocumentExtractor
    ↓
DocumentIngestionService
    ↓
DocumentChunker
    ↓
Embedding Service
    ↓
Vector Store
    ↓
RAG Retrieval
    ↓
Hybrid Reranker
    ↓
Grounded LLM Answer
```

### Document extraction abstraction

Introduced:

`IDocumentTextExtractor`

This abstraction separates document text extraction from the controller and ingestion pipeline.

Current implementation:

`PlainTextDocumentExtractor`

Supported formats:

- `.txt`
- `.md`
- `.csv`

The extractor:
- Uses UTF-8 encoding.
- Reads files asynchronously.
- Supports `CancellationToken`.
- Validates file names and extensions.
- Does not take ownership of the caller's stream.

### Upload API response

A real test document was uploaded successfully.

Example response:

```json
{
  "fileName": "annual-leave-test.txt",
  "message": "Document uploaded and ingested successfully.",
  "extractedCharacterCount": 280
}
```

### End-to-end validation

The uploaded annual-leave document was successfully:

1. Uploaded through the API.
2. Extracted into text.
3. Ingested through `DocumentIngestionService`.
4. Chunked.
5. Embedded.
6. Stored in the vector store.
7. Retrieved through the RAG pipeline.
8. Used to generate a grounded answer.

The positive query successfully returned the annual-leave information.

A negative query for maternity leave correctly returned:

"The information is not available in the provided documents."

### Architectural improvement

Previously, test documents were hardcoded inside `ChatController`.

The new architecture separates:

- HTTP/API concerns
- Document extraction
- Document ingestion
- Chunking
- Embedding
- Vector storage
- Retrieval
- Reranking
- Generation

This makes the system easier to extend with additional document formats later.

### Milestone Status

Completed:

- File upload API
- Document extraction abstraction
- Plain-text extraction
- Document ingestion
- Chunking
- Embedding
- Vector storage
- RAG retrieval
- Hybrid reranking
- End-to-end file-to-answer validation
- PDF extraction
- DOCX extraction
- Document metadata persistence
- Document metadata retrieval

Next planned milestone:

- Better document lifecycle management

## Multi-Format Document Ingestion

The document ingestion pipeline now supports multiple real-world document formats through a format-aware extractor architecture.

### Supported Formats

- `.txt`
- `.md`
- `.csv`
- `.pdf`
- `.docx`

### Architecture

```text
File Upload
    ↓
DocumentsController
    ↓
DocumentTextExtractorResolver
    ↓
┌───────────────────────────────┐
│ PlainTextDocumentExtractor    │ → .txt / .md / .csv
│ PdfDocumentTextExtractor      │ → .pdf
│ DocxDocumentTextExtractor     │ → .docx
└───────────────────────────────┘
    ↓
DocumentIngestionService
    ↓
DocumentChunker
    ↓
Embedding Service
    ↓
Vector Store
    ↓
Hybrid Reranker
    ↓
RAG
    ↓
Grounded LLM Answer
```

### Extractor Abstraction

The system uses:

`IDocumentTextExtractor`

This keeps document-format-specific extraction separate from the upload controller and ingestion pipeline.

A `DocumentTextExtractorResolver` selects the appropriate extractor based on the uploaded file extension.

Current implementations:

- `PlainTextDocumentExtractor`
- `PdfDocumentTextExtractor`
- `DocxDocumentTextExtractor`

### PDF Support

PDF text extraction is implemented using PdfPig.

The PDF extractor:
- Reads text from PDF pages.
- Preserves page order.
- Produces plain text suitable for chunking and embedding.
- Does not require Microsoft Word or another desktop application.

### DOCX Support

DOCX extraction is implemented using the Microsoft Open XML SDK.

The DOCX extractor:
- Reads DOCX documents directly from streams.
- Does not require Microsoft Word to be installed.
- Extracts paragraph/run text.
- Extracts table-cell content.
- Converts the document into plain text suitable for RAG chunking.

### End-to-End Validation

The same annual-leave test document was successfully processed through:

1. TXT upload and RAG retrieval.
2. PDF upload and RAG retrieval.
3. DOCX upload and RAG retrieval.

The DOCX test successfully returned:

"Full-time employees receive twenty-five days of annual leave per year."

This confirms that different document formats can enter the same downstream ingestion and RAG pipeline.

### Architectural Benefit

The controller does not contain format-specific extraction logic.

Instead:

```text
File extension
    ↓
DocumentTextExtractorResolver
    ↓
Format-specific extractor
    ↓
Common ingestion pipeline
```

This makes adding another format, such as a future document type, possible without redesigning the upload or RAG pipeline.

## Current Progress

Completed:

- File upload API
- Extractor abstraction
- Format-aware extractor resolver
- TXT/MD/CSV extraction
- PDF extraction
- DOCX extraction
- Document ingestion
- Chunking
- Embedding
- Vector storage
- Hybrid reranking
- RAG retrieval
- End-to-end multi-format testing
- Document metadata entity
- SQLite document repository
- Document metadata persistence
- Document metadata retrieval

### Next Milestones

- Document listing
- Document deletion
- Deleting associated vector chunks
- Re-indexing
- Duplicate detection
- Document versioning

## Document Metadata Persistence

Document metadata is persisted separately from document content and vector data.

### Metadata model

The upload pipeline creates a `DocumentMetadata` record containing:

- `Id`
- `FileName`
- `ContentType`
- `UploadedAtUtc`
- `Source`

The metadata is mapped to `DocumentEntity` and persisted through `IDocumentRepository`, implemented by `SQLiteDocumentRepository` using `VectorDbContext`.

### Metadata persistence flow

```text
DocumentsController
    ↓
IDocumentRepository
    ↓
SQLiteDocumentRepository
    ↓
VectorDbContext
    ↓
SQLite Documents table
```

Document content follows the existing ingestion path independently:

```text
Document Upload
    ↓
DocumentsController
    ↓
DocumentTextExtractorResolver
    ↓
Document Text Extraction
    ↓
DocumentIngestionService
    ↓
Vector Store / RAG
```

### Metadata retrieval

Persisted metadata can be retrieved by document ID:

`GET /api/Documents/{id}`

Metadata persistence was verified by uploading a PDF document and successfully retrieving its metadata by document ID using the document-management endpoints.

The retrieved metadata included the document ID, file name, content type, upload timestamp, and source.

### Current database state

SQLite is the current persistence database. The `vectors.db` database contains:

- `DocumentVectors` for vector data and chunk metadata
- `Documents` for document metadata
- `__EFMigrationsHistory` for EF Core migration history

The current migration history includes:

- `20260823180115_InitialVectorStore`
- `20260825043111_AddDocumentsTable`

Document metadata persistence is implemented and verified, but the application remains a local portfolio prototype rather than production-grade document management.

### Completed document management milestones

- Document metadata entity
- SQLite document repository
- Document metadata persistence
- Document metadata retrieval

### Future document management milestones

- Document listing
- Document deletion
- Deleting associated vector chunks
- Re-indexing
- Duplicate detection
- Document versioning

## Future Roadmap

The following capabilities remain future or planned work and are not yet implemented as production-grade architecture in the current codebase:

- Managed production vector database integration
- Native database vector indexing
- Advanced metadata filtering and ranking
- Source citations in generated answers
- Evaluation frameworks
- Streaming responses
- Azure OpenAI integration
- Long-term semantic memory
- Advanced RAG
- Advanced agentic AI
- LLMOps

These remain future improvements rather than current implementation claims.

## BM25 Retrieval — COMPLETED

The BM25 implementation is now in place and verified.

### Implemented components

- IBm25SearchService
- SQLiteBm25SearchService
- SQLite FTS5 virtual table support
- DocumentChunksFts table
- BM25 indexing initialization
- Backfill from existing DocumentVectors rows into the FTS5 table
- Chunk-level indexing during ingestion and document rebuilds
- Document reindexing support for replaced documents
- BM25 SearchAsync implementation
- Query normalization for FTS5 compatibility
- SQLite bm25() score ranking

### FTS5 and BM25 behavior

This implementation separates two distinct concepts:

- FTS5 full-text indexing: SQLite builds a searchable inverted index for text content.
- BM25 ranking: SQLite calculates a relevance score for matching rows using the bm25() function.

In other words, the FTS5 table makes keyword matching possible, while bm25() is what ranks the matching rows by relevance. The index is not the same thing as the ranking formula, and they were both validated during the BM25 debugging work.

### DocumentChunksFts

The BM25 pipeline indexes the chunk metadata and content in a dedicated SQLite FTS5 table named DocumentChunksFts.

The indexed columns include:

- ChunkId
- DocumentId
- Source
- Content

This allows the API to perform keyword-first retrieval over stored document chunks without depending only on dense vector similarity.

### BM25 indexing and maintenance

The current implementation includes:

- InitializeAsync() to create the FTS5 table when needed
- BackfillAsync() to populate the index from the existing DocumentVectors data
- IndexChunkAsync() for adding a single indexed chunk
- ReindexDocumentAsync() to delete and rebuild the index for a document's chunks

This was implemented to keep the BM25 search index aligned with the vector storage data and to support normal document reprocessing.

### BM25 SearchAsync and query normalization

The BM25 query path includes:

- validation for blank or invalid queries
- query normalization to reduce whitespace issues
- a SQLite MATCH query against DocumentChunksFts
- ORDER BY bm25(DocumentChunksFts) DESC
- LIMIT topK results

The normalization step was part of the debugging work because queries containing punctuation or hyphenated wording can behave differently in SQLite FTS5 than a human would expect. The implementation therefore trims and normalizes the incoming query before executing the search.

### API endpoint

The BM25 endpoint is:

- POST /api/chat/bm25-search

This endpoint accepts a search request, executes the BM25 query, and returns the ranked chunks with metadata and score information.

### Verified BM25 tests and debugging

The actual BM25 validation used the SQLite FTS5 table directly and through the API endpoint. The tested queries included:

- annual
- leave
- employees
- full
- time
- annual leave
- full employees
- full time employees
- full-time
- "annual leave"
- "full-time employees annual leave"
- full-time employees annual leave

The diagnostic checks confirmed:

- the DocumentChunksFts schema exists
- the table contains indexed rows
- vocabulary entries are being built as expected
- SQLite MATCH returns rows for policy-related terms
- bm25(DocumentChunksFts) produces ranking scores for returned results
- query behavior varies depending on tokenization and phrase structure

### PolicyFull-time indexing discovery

One important debugging discovery was the behavior of hyphenated terms in the FTS5 index. The index and query behavior for full-time did not always match a naive expectation that a hyphenated term would behave like a single literal string. This was validated by querying the FTS5 table directly and inspecting both the match count and the returned ranked rows.

That debugging step was important because it showed that FTS5 indexing and BM25 ranking must be validated against the real SQLite index behavior rather than assumed from standard keyword-search intuition.

### Dense/vector vs sparse/BM25 retrieval

This project now clearly distinguishes the two retrieval modes:

- Dense/vector retrieval: semantic similarity using embeddings and cosine similarity
- Sparse/BM25 retrieval: lexical keyword matching using SQLite FTS5 and bm25() ranking

These are different retrieval mechanisms with different strengths:

- Dense retrieval is good for semantic meaning and paraphrased intent.
- Sparse/BM25 retrieval is good for exact or near-exact keyword matching.

The BM25 implementation is therefore a real retrieval layer in its own right, not just a diagnostic pass.

### Current next implementation

Hybrid Search is the NEXT implementation:

Vector Search + BM25 → candidate fusion → reranking → RAG

This is the next step in the retrieval stack, where dense and sparse signals are combined before the final reranking and grounded generation stages.
