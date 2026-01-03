using System.Net;
using System.Text;

namespace Tests;

/// <summary>
/// Fake HTTP handler der returnerer en foruddefineret response.
/// </summary>
public sealed class FakeHandler : HttpMessageHandler
{
    private readonly string _response;
    private readonly HttpStatusCode _statusCode;

    public FakeHandler(string response, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _response = response;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_response, Encoding.UTF8, "text/html")
        };
        return Task.FromResult(response);
    }
}
