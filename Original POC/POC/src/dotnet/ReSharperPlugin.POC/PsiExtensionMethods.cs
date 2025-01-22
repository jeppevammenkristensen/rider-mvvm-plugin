using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.POC;

public static class PsiExtensionMethods
{
    [CanBeNull]
    public static IProject GetProjectByName(this ISolution solution, string projectName)
    {
        var projects = solution.GetTopLevelProjects();
        return projects.FirstOrDefault(project => project.Name == projectName);
    }



    public static ITreeNode GetTypeTreeNodeByNamespaceAndShortName(this ICSharpFile file, string nameSpace, string shortName)
    {
        var namespaceDecls = file.NamespaceDeclarationsEnumerable;
        var namespaceDecl = (from decl in namespaceDecls
            where decl.DeclaredName == nameSpace
            select decl).FirstOrDefault();

        if (namespaceDecl == null) return null;
        var typeDecls = namespaceDecl.TypeDeclarationsEnumerable;

        var resultList = (from node in typeDecls
            where node.DeclaredName == shortName
            select node).ToList();

        return resultList.FirstOrDefault();
    }
        
    [CanBeNull]
    public static ITreeNode GetTypeTreeNodeByFqn(this ICSharpFile file, string typeName)
    {
        var namespaceName = GetLongNameFromFqn(typeName);
        var shortName = GetShortNameFromFqn(typeName);            

        var namespaceDecls = file.NamespaceDeclarationsEnumerable;
        var namespaceDecl = (from decl in namespaceDecls
            where decl.DeclaredName == namespaceName
            select decl).FirstOrDefault();

        if (namespaceDecl == null) return null;
        var typeDecls = namespaceDecl.TypeDeclarationsEnumerable;

        var resultList = (from node in typeDecls
            where node.DeclaredName == shortName
            select node).ToList();

        return resultList.FirstOrDefault();
    }


    private static string GetShortNameFromFqn(string fqn)
    {
        var pos = fqn.LastIndexOf(".", StringComparison.Ordinal) + 1;
        return pos > 0 ? fqn.Substring(pos) : fqn;
    }


    private static string GetLongNameFromFqn(string fqn)
    {
        var pos = fqn.LastIndexOf(".", StringComparison.Ordinal) + 1;
        return pos > 0 ? fqn.Substring(0, pos - 1) : fqn;
    }


    [CanBeNull]
    public static IEnumerable<IDeclaredElement> GetReferencedElements(this ITreeNode node)
    {
        var result = new List<IDeclaredElement>();
        var parentExpression = node.GetParentOfType<IReferenceExpression>();
        if (parentExpression == null) return null;

        var references = parentExpression.GetReferences();

        foreach (var reference in references)
        {
            var declaredElement = reference.Resolve().DeclaredElement;
            if (declaredElement != null)
                result.Add(declaredElement);
        }

        return result.Count == 0 ? null : result;
    }


    [CanBeNull]
    public static T GetParentOfType<T>(this ITreeNode node) where T : class, ITreeNode
    {
        while (node != null)
        {
            var typedNode = node as T;
            if (typedNode != null)
                return typedNode;

            node = node.Parent;
        }
        return null;
    }
}