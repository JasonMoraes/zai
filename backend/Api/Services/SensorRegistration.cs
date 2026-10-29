using Api.Entities;

namespace Api.Services;

/// <summary>A just-registered sensor together with its plain API key, which exists only in this result.</summary>
public record SensorRegistration(Sensor Sensor, string ApiKey);
