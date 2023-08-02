﻿using System.Text.Json.Serialization;

namespace Consumer.RealTime.Models;

public sealed class Ext
{
    private const string PropertyName = "com.cumulocity.authn";

    [JsonPropertyName(PropertyName)]
    public Auth Auth { get; init; } = new();
}