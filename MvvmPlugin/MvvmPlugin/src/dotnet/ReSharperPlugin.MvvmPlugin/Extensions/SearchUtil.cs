using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Logging;

namespace ReSharperPlugin.MvvmPlugin.Extensions;

public static  class SearchUtil
{
    public static IEnumerable<ReferenceOccurrence> FindUsagesInFile<T>(this ICSharpContextActionDataProvider provider,
        T treeNode, Func<T, IDeclaredElement> declaredElementRetriever) where T : ITreeNode
    {   
        var consumer = new SearchResultsConsumer();
        
        try
        {
            provider.PsiServices.SingleThreadedFinder.FindReferences(declaredElementRetriever(treeNode), domain: SearchDomainFactory.Instance.CreateSearchDomain(treeNode.GetSourceFile()), consumer: consumer, NullProgressIndicator.Create());
        }
        catch (Exception e)
        {
            Logger.LogException("Failed to find usages in file", e);
            yield break;
        }
        
        foreach (var occurrence in consumer.GetOccurrences().OfType<ReferenceOccurrence>())
        {
            yield return occurrence;
        }
    }
    
    public static IEnumerable<ReferenceOccurrence> FindUsagesInFile(this ICSharpContextActionDataProvider provider,
        ITreeNode treeNode,  params IDeclaredElement[] declaredElements)
    {   
        var consumer = new SearchResultsConsumer();
        
        try
        {
            provider.PsiServices.SingleThreadedFinder.FindReferences(declaredElements, domain: SearchDomainFactory.Instance.CreateSearchDomain(treeNode.GetSourceFile()), consumer: consumer, NullProgressIndicator.Create());
        }
        catch (Exception e)
        {
            Logger.LogException("Failed to find usages in file", e);
            yield break;
        }
        
        foreach (var occurrence in consumer.GetOccurrences().OfType<ReferenceOccurrence>())
        {
            yield return occurrence;
        }
    }
}
