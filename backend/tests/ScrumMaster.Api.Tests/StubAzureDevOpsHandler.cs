namespace ScrumMaster.Api.Tests;

/// <summary>
/// Remplace l'appel réseau réel vers Azure DevOps par une réponse configurée par le test — voir
/// specs/005-azure-devops-boards/plan.md (section Testing).
/// </summary>
public class StubAzureDevOpsHandler : DelegatingHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage> Repondre { get; set; } = _ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(Repondre(request));
}
