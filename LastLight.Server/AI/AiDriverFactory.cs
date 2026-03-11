using System;
using System.Collections.Generic;
using System.Text.Json;

namespace LastLight.Server.AI;

public static class AiDriverFactory
{
    public static IAiDriver Create(string aiDriverName)
    {
        return aiDriverName.ToLower() switch
        {
            "phased" => new PhasedAiDriver(),
            "standard" => new StandardAiDriver(),
            _ => new StandardAiDriver() // Fallback
        };
    }
}
