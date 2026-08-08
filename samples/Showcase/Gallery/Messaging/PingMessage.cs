namespace Samples.Showcase.Gallery.Messaging;

/// <summary>Demo message used by the Messaging page.</summary>
public sealed record PingMessage(string Text, int Sequence);
