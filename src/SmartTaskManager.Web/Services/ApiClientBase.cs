using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartTaskManager.Web.Models;

namespace SmartTaskManager.Web.Services;

public abstract class ApiClientBase
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private readonly HttpClient _httpClient;
    private readonly SmartTaskManagerApiAccessTokenProvider _accessTokenProvider;

    protected ApiClientBase(HttpClient httpClient, SmartTaskManagerApiAccessTokenProvider accessTokenProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _accessTokenProvider = accessTokenProvider ?? throw new ArgumentNullException(nameof(accessTokenProvider));
    }

    protected Task<TResponse> GetAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return SendForJsonAsync<TResponse>(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
    }

    protected Task<TResponse> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(request, options: SerializerOptions)
        };

        return SendForJsonAsync<TResponse>(message, cancellationToken);
    }

    protected Task<TResponse> PatchAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return SendForJsonAsync<TResponse>(
            new HttpRequestMessage(HttpMethod.Patch, requestUri),
            cancellationToken);
    }

    protected Task<TResponse> PatchAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new(HttpMethod.Patch, requestUri);
        message.Content = JsonContent.Create(request, options: SerializerOptions);

        return SendForJsonAsync<TResponse>(message, cancellationToken);
    }

    private async Task<TResponse> SendForJsonAsync<TResponse>(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage message = request;
        await AttachAccessTokenAsync(message);
        using HttpResponseMessage response = await _httpClient.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response, cancellationToken);
        }

        TResponse? result = await response.Content.ReadFromJsonAsync<TResponse>(SerializerOptions, cancellationToken);
        if (result is null)
        {
            throw new InvalidOperationException("The API returned an empty response.");
        }

        return result;
    }

    private async Task AttachAccessTokenAsync(HttpRequestMessage request)
    {
        if (request.Headers.Authorization is not null)
        {
            return;
        }

        string accessToken = await _accessTokenProvider.GetAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task<SmartTaskManagerApiException> CreateApiExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        ApiErrorDetails? error = null;

        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiErrorDetails>(SerializerOptions, cancellationToken);
        }
        catch (JsonException)
        {
        }

        string message = error is null
            ? $"The API returned {(int)response.StatusCode} ({response.ReasonPhrase})."
            : string.Join(
                " ",
                new[] { error.Title, error.Detail }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

        return new SmartTaskManagerApiException((int)response.StatusCode, message, error);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
