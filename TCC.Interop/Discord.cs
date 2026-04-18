using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Nostrum;
using TCC.Utils;

namespace TCC.Interop;

public static class Discord
{
    // Classic+ read-only fork: FireWebhook is a no-op.
    // The upstream implementation gated outbound Discord calls behind Firebase
    // (a Google Cloud function registered by the user). Firebase.cs was deleted
    // as part of the strip-everything-non-read-only pass, so this method can't
    // execute anyway. Kept as a stub so existing call sites (notifier settings,
    // Discord-integration menu items) still compile.
    public static void FireWebhook(string webhook, string message, string usernameOverride, string accountHash)
    {
        // intentional no-op
        _ = webhook; _ = message; _ = usernameOverride; _ = accountHash;
    }
}