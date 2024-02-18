﻿using System.Text.Json.Serialization;

namespace GotikAniki69.Server.Models;

public class Coordinates
{
    [JsonPropertyName("x")]
    public double X
    {
        get; set;
    }

    [JsonPropertyName("y")]
    public double Y
    {
        get; set;
    }
}