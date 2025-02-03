using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit;

[ContextAction(
    Name = "Make Class Observable",
    Description = "Lets the class inherit from ObservableObject. If required the containing class will be made partial.",
    GroupType = typeof(CSharpContextActions))]
public class MakeObservableContextAction : ContextActionBase
{
    private readonly ICSharpContextActionDataProvider _provider;

    public MakeObservableContextAction(ICSharpContextActionDataProvider provider)
    {
        _provider = provider;
    }
    
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (_provider.GetSelectedTreeNode<IClassDeclaration>() is not { } classLikeDeclaration)
            return null;
        
        using (WriteLockCookie.Create())
        {

            if (!ObservableInstalled)
            {
                // I haven't figured out how to install a nuget package. But 
                // here should be code that will install the CommunityToolkit.Mvvm
                // if it isn't installed.
                // For now if it isn't installed the generated code will not be compilable
                // but the ObservableObject will be generated so you can use resharper/rider to 
                // right click and install it. 
                
                // var project = classLikeDeclaration.GetProject();
                // const string packageName = "CommunityToolkit.Mvvm";    
            }
            
            
            classLikeDeclaration.EnsurePartialAndInheritsObservableObject(observableObject: null, supressObservableObjectNotFound: true);
            return null;
        }
    }

    public override string Text => "Make Class ObservableObject";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        FieldDeclaration = null;
        
        if (_provider.GetSelectedTreeNode<IClassDeclaration>() is { } classLikeDeclaration)
        {
            // Check if the containing class implements the ObservableObject in some way or another

            if (PluginUtil.GetObservableObject(classLikeDeclaration).ShouldBeKnown() is { } observableObject)
            {
                ObservableInstalled = true;
                
                
               var declaredElement = classLikeDeclaration.DeclaredElement;
               if (declaredElement == null)
                   return false;

               if (declaredElement.IsDescendantOf(observableObject.GetTypeElement()))
               {
                   return false;
               }
            }
            else
            {
                // We land here if we could not get the ObservableObject (the package is not installed)
                // For now (to not present the context action all the time) we check if the name of the given
                // class ends with ViewModel if not we return false
                if (!classLikeDeclaration.DeclaredName.EndsWith("ViewModel", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            if (classLikeDeclaration.SuperTypes.Any(x => x.IsClassType()))
            {
                return false;
            }

            if (!classLikeDeclaration.IsPartial)
                return true;

            
        }
        
        
        

        return false;
    }

    public bool ObservableInstalled { get; set; }

    public IFieldDeclaration? FieldDeclaration { get; private set; }
}