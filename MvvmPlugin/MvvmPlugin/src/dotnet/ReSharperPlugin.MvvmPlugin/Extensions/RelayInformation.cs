using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.MvvmPlugin.Extensions;

public class RelayInformation
{
    // (bool Async, bool HasParameters, IType? ParameterType)
    public bool Async { get; private set; }
    public bool HasParameters { get; private set; }
    public IType? ParameterType { get; private set; }
    
    public RelayInformation(bool async, bool hasParameters, IType? parameterType)
    {
        Async = async;
        HasParameters = hasParameters;
        ParameterType = parameterType;
    }
    
}