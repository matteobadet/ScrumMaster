using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class ThemeChangeTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ThemeChangeTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record ThemeChangedEnvelope(ThemeRefDto Theme, List<ColonneDto> Colonnes);

    [Fact]
    public async Task ChangeTheme_ParLeFacilitateur_AvecColonnesPersonnalisees_EstApplique()
    {
        var (boardId, facilitateurId) = await CreateBoardAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var tcs = new TaskCompletionSource<ThemeChangedEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<ThemeChangedEnvelope>("ThemeChanged", e => tcs.TrySetResult(e));

        var themePersonnalise = new ThemePersonnaliseDto("Mon thème", null, null, ["Continuer", "Arrêter", "Essayer"]);
        await connection.InvokeAsync("ChangeTheme", boardId, null, themePersonnalise);

        var changed = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(3, changed.Colonnes.Count);
        Assert.Contains(changed.Colonnes, c => c.Intitule == "Continuer");
    }

    [Fact]
    public async Task ChangeTheme_ParUnNonFacilitateur_EstRefuse()
    {
        var (boardId, _) = await CreateBoardAsync();
        var autreParticipantId = await JoinBoardAsync(boardId, "Sam");

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, autreParticipantId);

        var themePersonnalise = new ThemePersonnaliseDto("Mon thème", null, null, ["A", "B"]);
        var ex = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync("ChangeTheme", boardId, null, themePersonnalise)
        );

        Assert.Contains("facilitateur", ex.Message);
    }

    [Fact]
    public async Task ChangeTheme_ReaffecteLesPostItsExistantsALaPremiereColonneDuNouveauTheme()
    {
        var (boardId, colonnes, facilitateurId) = await CreateBoardWithColonnesAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var addedTcs = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var addedSub = connection.On<PostItAddedEnvelope>("PostItAdded", e => addedTcs.TrySetResult(e.PostIt.Id)))
        {
            await connection.InvokeAsync("AddPostIt", boardId, colonnes[0], "Ne doit pas disparaître");
            await addedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        var themePersonnalise = new ThemePersonnaliseDto("Nouveau", null, null, ["Alpha", "Beta"]);
        await connection.InvokeAsync("ChangeTheme", boardId, null, themePersonnalise);

        var state = await GetBoardStateAsync(boardId);
        Assert.Single(state.PostIts);
        Assert.Equal("Ne doit pas disparaître", state.PostIts[0].Texte);
        Assert.Equal(state.Colonnes[0].Id, state.PostIts[0].ColonneId);
    }

    private record PostItAddedEnvelope(PostItPayload PostIt);

    private record PostItPayload(Guid Id);

    private async Task<BoardStateDto> GetBoardStateAsync(Guid boardId)
    {
        var response = await _client.GetAsync($"/api/boards/{boardId}");
        return (await response.Content.ReadFromJsonAsync<BoardStateDto>())!;
    }

    private async Task<(Guid BoardId, List<Guid> Colonnes, Guid FacilitateurId)> CreateBoardWithColonnesAsync()
    {
        var (boardId, facilitateurId) = await CreateBoardAsync();
        var state = await GetBoardStateAsync(boardId);
        return (boardId, state.Colonnes.Select(c => c.Id).ToList(), facilitateurId);
    }

    [Fact]
    public async Task ChangeTheme_AvecColonnesVides_EstRefuse()
    {
        var (boardId, facilitateurId) = await CreateBoardAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var themePersonnalise = new ThemePersonnaliseDto("Vide", null, null, []);
        var ex = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync("ChangeTheme", boardId, null, themePersonnalise)
        );

        Assert.Contains("colonne", ex.Message);
    }

    private async Task<Guid> JoinBoardAsync(Guid boardId, string nomAffiche)
    {
        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/participants", new JoinBoardRequest(nomAffiche));
        var joined = await response.Content.ReadFromJsonAsync<JoinBoardResponse>();
        return joined!.ParticipantId;
    }

    private async Task<(Guid BoardId, Guid FacilitateurId)> CreateBoardAsync()
    {
        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex");
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();
        return (created!.BoardId, created.ParticipantId);
    }

    private HubConnection CreateConnection() =>
        new HubConnectionBuilder()
            .WithUrl(
                "http://localhost/hubs/retro-board",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                }
            )
            .Build();
}
