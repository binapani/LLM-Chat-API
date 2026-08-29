using LLMChat.Api.Models;
using LLMChat.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LLMChat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILLMService _llmService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentIngestionService _documentIngestionService;
    private readonly IVectorStore _vectorStore;
    private readonly IRAGService _ragService;
    private readonly IRAGEvaluationService _ragEvaluationService;

    public ChatController(
        IConfiguration configuration,
        ILLMService llmService,
        IEmbeddingService embeddingService,
        IDocumentIngestionService documentIngestionService,
        IVectorStore vectorStore,
        IRAGService ragService,
        IRAGEvaluationService ragEvaluationService)
    {
        _configuration = configuration;
        _llmService = llmService;
        _embeddingService = embeddingService;
        _documentIngestionService = documentIngestionService;
        _vectorStore = vectorStore;
        _ragService = ragService;
        _ragEvaluationService = ragEvaluationService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var answer = await _llmService.GenerateAnswerAsync(request.Message);

        return Ok(new ChatResponse
        {
            Answer = answer
        });
    }

    [HttpPost("embedding")]
    public async Task<ActionResult<float[]>> GenerateEmbedding([FromBody] string text)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
        return Ok(embedding);
    }

    [HttpPost("ingest")]
public async Task<IActionResult> IngestDocuments()
{
    var documents = new[]
    {
        (
            Source: "password-policy",
            Content: "Password reset policy: All employees, contractors, and privileged users must use the secure password reset workflow managed by the Identity and Access Management team. Users may initiate a password reset only through the official company portal or approved self-service portal after confirming their identity with a verified email address, mobile device, or security question response. Passwords must be at least 12 characters in length and include at least one uppercase letter, one lowercase letter, one number, and one special character from the approved character set. Reused passwords from previous company accounts are prohibited, and passwords cannot contain the user’s first name, last name, username, or common dictionary words. The system will reject weak passwords and display specific guidance if the chosen password does not meet organizational requirements. Users who forget their password may request a reset link, which expires within 30 minutes, and the reset must be completed within the same secure session. If a user is locked out after multiple failed login attempts, they must wait for the lockout window to expire or contact the help desk to verify identity before reactivation. For all administrative and privileged accounts, MFA is required for each login and password reset, and recovery codes must be stored in the approved password manager. Accounts must not be shared, and users are responsible for protecting their password from disclosure, phishing attempts, and shoulder-surfing. Password expiration occurs every 90 days for standard users and every 60 days for elevated accounts. Before expiration, users receive a notification prompting them to update their password. Users who do not change their password before the deadline will be required to reset at next login. The organization enforces a maximum of 5 failed login attempts for standard users and 3 for privileged users before an account is temporarily locked. Locked accounts remain inaccessible until they are manually reviewed and unlocked by IT security or until the configured lockout timer resets. Security teams monitor suspicious login patterns and may require a security reset if unusual access is detected. All users are responsible for reporting account compromise, password sharing, or suspected phishing immediately to the Information Security team. Managers are expected to reinforce the policy during onboarding and annual security awareness training, and employees must acknowledge the policy before access is granted. This policy supports secure authentication, protects confidential data, and reduces the chance of credential theft or unauthorized access across corporate systems."
        ),
        (
            Source: "cafeteria-policy",
            Content: "Cafeteria opening hours: The cafeteria is open Monday through Friday from 8:00 AM to 5:00 PM. It is closed on weekends and public holidays."
        )
    };

    await _documentIngestionService.IngestAsync(documents);

    return Ok("Documents ingested successfully.");
}
    [HttpPost("search")]
    public async Task<ActionResult<IReadOnlyList<VectorSearchResult>>> Search([FromBody] ChatRequest request)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Message);
        var results = await _vectorStore.SearchAsync(queryEmbedding, 2);

        return Ok(results);
    }

    [HttpPost("accurate-search")]
    public async Task<ActionResult<IReadOnlyList<AccurateSearchResult>>> AccurateSearch([FromBody] ChatRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var minimumSimilarity = _configuration.GetValue<float>("Rag:MinimumSimilarity");
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Message);
        var results = await _vectorStore.SearchAsync(queryEmbedding, 5);

        var filteredResults = results
            .Where(result => result.Similarity >= minimumSimilarity)
            .Select(result => new AccurateSearchResult
            {
                DocumentId = result.Document.DocumentId,
                ChunkId = result.Document.ChunkId,
                Source = result.Document.Source,
                Similarity = result.Similarity,
                Content = result.Document.Content
            })
            .ToList();

        return Ok(filteredResults);
    }

    [HttpPost("rag")]
    public async Task<ActionResult<ChatResponse>> GenerateRagAnswer([FromBody] ChatRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var answer = await _ragService.GenerateAnswerAsync(request.Message);

        return Ok(new ChatResponse
        {
            Answer = answer
        });
    }
[HttpPost("rag-context")]
public async Task<ActionResult<string>> RetrieveRagContext(
    [FromBody] ChatRequest request)
{
    if (!ModelState.IsValid)
    {
        return ValidationProblem(ModelState);
    }

    var context = await _ragService.RetrieveContextAsync(request.Message);

    return Ok(context);
}
    [HttpPost("evaluate-rag")]
    public async Task<ActionResult<IReadOnlyList<RAGEvaluationResult>>> EvaluateRag(
        [FromBody] IReadOnlyList<RAGEvaluationCase> cases)
    {
        if (cases == null || cases.Count == 0)
        {
            return BadRequest("Evaluation cases are required and cannot be empty.");
        }

        var results = await _ragEvaluationService.EvaluateAsync(cases);

        return Ok(results);
    }
    [HttpPost("agent")]
public async Task<ActionResult<ChatResponse>> Agent(
    [FromBody] ChatRequest request,
    [FromServices] IAgentService agentService,
    CancellationToken cancellationToken)
{
    if (!ModelState.IsValid)
    {
        return ValidationProblem(ModelState);
    }

    var answer = await agentService.RunAsync(
        request.Message,
        cancellationToken);

    return Ok(new ChatResponse
    {
        Answer = answer
    });
}
}
