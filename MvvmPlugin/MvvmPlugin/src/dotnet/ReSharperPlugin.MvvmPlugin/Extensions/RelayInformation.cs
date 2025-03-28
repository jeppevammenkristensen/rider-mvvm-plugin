using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.MvvmPlugin.Extensions;

public record RelayInformation(bool Async, bool HasParameters, IType? ParameterType)
{
}