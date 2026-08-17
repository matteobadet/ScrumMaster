using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ScrumMaster.Api.AzureDevOps;

/// <summary>
/// Client HTTP typé vers l'API REST Azure DevOps — voir specs/005-azure-devops-boards/research.md#1.
/// Authentification par PAT (Basic Auth, utilisateur vide) fournie à chaque appel : cette classe
/// ne stocke jamais de PAT en mémoire au-delà de la durée d'un appel.
/// </summary>
public class AzureDevOpsClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Valide qu'un PAT donne accès à l'organisation/projet indiqués (US1, FR-003).</summary>
    public async Task<bool> ValiderAccesAsync(string organisation, string projet, string pat, CancellationToken cancellationToken = default)
    {
        var request = CreerRequete(HttpMethod.Get, organisation, $"_apis/projects/{Uri.EscapeDataString(projet)}?api-version=7.1", pat);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Liste les Iterations du projet avec l'indicateur "en cours" (US2, FR-005a, research.md#4).</summary>
    public async Task<IReadOnlyList<AzureDevOpsIterationSummary>> ListerIterationsAsync(
        string organisation,
        string projet,
        string pat,
        CancellationToken cancellationToken = default
    )
    {
        var request = CreerRequete(
            HttpMethod.Get,
            organisation,
            $"{Uri.EscapeDataString(projet)}/_apis/wit/classificationnodes/iterations?$depth=1&api-version=7.1",
            pat
        );
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var arbre = await response.Content.ReadFromJsonAsync<AzureDevOpsIterationTreeDto>(JsonOptions, cancellationToken);

        var aujourdhui = DateOnly.FromDateTime(DateTime.UtcNow);
        return (arbre?.Children ?? [])
            .Select(n => new AzureDevOpsIterationSummary($"{projet}\\{n.Name}", EstEnCours(n.Attributes, aujourdhui)))
            .ToList();
    }

    /// <summary>Liste les work items assignés à une Iteration (US3, FR-008).</summary>
    public async Task<IReadOnlyList<AzureDevOpsWorkItemSummary>> ListerWorkItemsAsync(
        string organisation,
        string projet,
        string pat,
        string cheminIteration,
        CancellationToken cancellationToken = default
    )
    {
        var wiql = new { query = $"SELECT [System.Id] FROM WorkItems WHERE [System.IterationPath] = '{cheminIteration.Replace("'", "''")}'" };
        var wiqlRequest = CreerRequete(HttpMethod.Post, organisation, $"{Uri.EscapeDataString(projet)}/_apis/wit/wiql?api-version=7.1", pat);
        wiqlRequest.Content = JsonContent.Create(wiql);
        var wiqlResponse = await httpClient.SendAsync(wiqlRequest, cancellationToken);
        wiqlResponse.EnsureSuccessStatusCode();
        var resultat = await wiqlResponse.Content.ReadFromJsonAsync<AzureDevOpsWiqlResultDto>(JsonOptions, cancellationToken);

        var ids = resultat?.WorkItems.Select(w => w.Id).ToList() ?? [];
        if (ids.Count == 0)
        {
            return [];
        }

        var detailsRequest = CreerRequete(
            HttpMethod.Get,
            organisation,
            $"_apis/wit/workitems?ids={string.Join(',', ids)}&fields=System.Title,System.WorkItemType,System.State&api-version=7.1",
            pat
        );
        var detailsResponse = await httpClient.SendAsync(detailsRequest, cancellationToken);
        detailsResponse.EnsureSuccessStatusCode();
        var lot = await detailsResponse.Content.ReadFromJsonAsync<AzureDevOpsWorkItemsBatchDto>(JsonOptions, cancellationToken);

        return (lot?.Value ?? [])
            .Select(w => new AzureDevOpsWorkItemSummary(
                w.Id,
                ObtenirChamp(w.Fields, "System.Title"),
                ObtenirChamp(w.Fields, "System.WorkItemType"),
                ObtenirChamp(w.Fields, "System.State")
            ))
            .ToList();
    }

    /// <summary>
    /// Mapping état→catégorie normalisée pour un type de work item donné (specs/009-sprint-review-
    /// stats, research.md#1) — indépendant du modèle de processus de l'équipe.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, AzureDevOpsEtatCategorie>> ObtenirEtatsAsync(
        string organisation,
        string projet,
        string pat,
        string type,
        CancellationToken cancellationToken = default
    )
    {
        var request = CreerRequete(
            HttpMethod.Get,
            organisation,
            $"{Uri.EscapeDataString(projet)}/_apis/wit/workitemtypes/{Uri.EscapeDataString(type)}/states?api-version=7.1",
            pat
        );
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var etats = await response.Content.ReadFromJsonAsync<AzureDevOpsWorkItemStatesResponseDto>(JsonOptions, cancellationToken);

        return (etats?.Value ?? [])
            .Where(e => Enum.TryParse<AzureDevOpsEtatCategorie>(e.Category, ignoreCase: true, out _))
            .ToDictionary(e => e.Name, e => Enum.Parse<AzureDevOpsEtatCategorie>(e.Category, ignoreCase: true));
    }

    private static string ObtenirChamp(Dictionary<string, object> champs, string nom) =>
        champs.TryGetValue(nom, out var valeur) ? valeur?.ToString() ?? string.Empty : string.Empty;

    /// <summary>Crée un work item de type "Task" avec le titre donné (US4, FR-009).</summary>
    public async Task<int> CreerWorkItemAsync(
        string organisation,
        string projet,
        string pat,
        string titre,
        CancellationToken cancellationToken = default
    )
    {
        var request = CreerRequete(HttpMethod.Post, organisation, $"{Uri.EscapeDataString(projet)}/_apis/wit/workitems/$Task?api-version=7.1", pat);
        var patch = new[] { new { op = "add", path = "/fields/System.Title", value = titre } };
        request.Content = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8, "application/json-patch+json");
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var cree = await response.Content.ReadFromJsonAsync<AzureDevOpsCreatedWorkItemDto>(JsonOptions, cancellationToken);
        return cree!.Id;
    }

    private static bool EstEnCours(AzureDevOpsIterationAttributesDto? attributs, DateOnly aujourdhui)
    {
        if (attributs?.StartDate is not { } debut || attributs.FinishDate is not { } fin)
        {
            return false;
        }

        var debutDate = DateOnly.FromDateTime(debut.UtcDateTime);
        var finDate = DateOnly.FromDateTime(fin.UtcDateTime);
        return aujourdhui >= debutDate && aujourdhui <= finDate;
    }

    private static HttpRequestMessage CreerRequete(HttpMethod methode, string organisation, string chemin, string pat)
    {
        var request = new HttpRequestMessage(methode, $"https://dev.azure.com/{Uri.EscapeDataString(organisation)}/{chemin}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}")));
        return request;
    }
}
