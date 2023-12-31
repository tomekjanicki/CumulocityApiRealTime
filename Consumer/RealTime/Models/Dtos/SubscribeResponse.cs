﻿namespace Consumer.RealTime.Models.Dtos;

public sealed class SubscribeResponse : Response
{
    public Error? ToResult() => GetNullableError();

    public static SubscribeResponse GetRequiredResponse(byte[] message)
        => GetRequiredResponse<SubscribeResponse>(message);
}