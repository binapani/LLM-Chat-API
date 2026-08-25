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

## Next Milestone — Reranking

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

This is the next milestone. The concepts are:

- Retriever = finds possible candidates
- Reranker = determines which candidates are most relevant
- Generator = produces the final answer

This will be investigated as a future improvement, but it is not implemented in the current codebase and is clearly marked as the next milestone.

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

The following capabilities are planned learning milestones and are not implemented as production-grade architecture in the current codebase:

- Managed production vector database integration
- Native database vector indexing
- Advanced metadata filtering and ranking
- Source citations in generated answers
- Retrieval and answer evaluation frameworks
- Streaming responses
- Tool and function calling
- AI agent orchestration
- Azure OpenAI integration
- Azure AI Search integration

These remain future improvements rather than current implementation claims.
