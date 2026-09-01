namespace Scp035.ApiFeatures.Net;

internal readonly struct HttpResponse
{
    internal HttpResponse(long code, string body, string error)
    {
        Code = code;
        Body = body;
        Error = error;
    }

    internal long Code { get; }

    internal string Body { get; }

    internal string Error { get; }

    internal bool IsSuccessful => Error is null && Code is >= 200 and < 300;
}