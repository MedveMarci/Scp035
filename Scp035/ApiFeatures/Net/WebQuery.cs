using System;
using System.Collections.Generic;
using MEC;
using UnityEngine.Networking;

namespace Scp035.ApiFeatures.Net;

internal static class WebQuery
{
    private const int TimeoutSeconds = 10;

    internal static CoroutineHandle Get(string url, Action<HttpResponse> callback)
    {
        try
        {
            return Timing.RunCoroutine(Send(UnityWebRequest.Get(url), callback), "RoleSwap_Http");
        }
        catch (Exception exception)
        {
            LogManager.Debug($"Failed to start the request to {url}: {exception}");
            Answer(callback, new HttpResponse(0, null, exception.Message));

            return default;
        }
    }

    private static IEnumerator<float> Send(UnityWebRequest request, Action<HttpResponse> callback)
    {
        using (request)
        {
            request.timeout = TimeoutSeconds;

            if (!TrySend(request, out string error))
            {
                Answer(callback, new HttpResponse(0, null, error));
                yield break;
            }

            while (!request.isDone)
                yield return Timing.WaitForOneFrame;

            Answer(callback, Read(request));
        }
    }

    private static bool TrySend(UnityWebRequest request, out string error)
    {
        try
        {
            request.SendWebRequest();
            error = null;

            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            LogManager.Debug($"Failed to send the request to {request.url}: {exception}");

            return false;
        }
    }

    private static HttpResponse Read(UnityWebRequest request)
    {
        try
        {
            return new HttpResponse(request.responseCode, request.downloadHandler?.text,
                string.IsNullOrEmpty(request.error) ? null : request.error);
        }
        catch (Exception exception)
        {
            return new HttpResponse(0, null, exception.Message);
        }
    }

    private static void Answer(Action<HttpResponse> callback, HttpResponse response)
    {
        if (callback is null)
            return;

        try
        {
            callback(response);
        }
        catch (Exception exception)
        {
            LogManager.Error($"A web request callback failed.\n{exception}");
        }
    }
}