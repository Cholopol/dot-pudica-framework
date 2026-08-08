namespace DotPudica.Core.Binding;

internal interface IBinding
{
    void Bind(object? source);
    void Unbind();
    void Dispose();
}
