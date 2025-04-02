using System;
using System.Linq.Expressions;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReSharperPlugin.MvvmPlugin.Tests.test.data.ConvertToRelayTest;

public partial class NotifyPropertyChangedTest : ObservableObject
{
    private string _someProperty;
    
    public string OtherProperty { get; set; }
    public string AnotherProperty { get; set; }
    
    public string ThirdProperty { get; set; }
    
    public string Some{caret}Property 
    { 
        get
        {
            return _someProperty;
        } 
        set
        {
            _someProperty = value;
            OnPropertyChanged(nameof("SomeProperty"));
            OnPropertyChanged(nameof(OtherProperty));
            OnPropertyChanged("AnotherProperty"));
            SetProperty(ref field, value);
            OnPropertyChanged(() => ThirdProperty);
            OnPropertyChanged<FirstTest>(t => t.ForthProperty);

        }
    }
    
    protected void OnPropertyChanged<T>(
        Expression<Func<T>> expression)
    {
        string propertyName = PropertyName.For(expression);
        OnPropertyChanged(propertyName);
    }
}

/// <summary>
/// Gets property name using lambda expressions.
/// </summary>
internal class PropertyName
{
    public static string For<t>(
        Expression<func<t, object>> expression)
    {
        Expression body = expression.Body;
        return GetMemberName(body);
    }
 
    public static string For(
        Expression<func<object>> expression)
    {
        Expression body = expression.Body;
        return GetMemberName(body);
    }
 
    public static string GetMemberName(
        Expression expression)
    {
        if (expression is MemberExpression)
        {
            var memberExpression = (MemberExpression)expression;
 
            if (memberExpression.Expression.NodeType ==
                ExpressionType.MemberAccess)
            {
                return GetMemberName(memberExpression.Expression)
                       + "."
                       + memberExpression.Member.Name;
            }
            return memberExpression.Member.Name;
        }
 
        if (expression is UnaryExpression)
        {
            var unaryExpression = (UnaryExpression)expression;
 
            if (unaryExpression.NodeType != ExpressionType.Convert)
                throw new Exception(string.Format(
                    "Cannot interpret member from {0}",
                    expression));
 
            return GetMemberName(unaryExpression.Operand);
        }
 
        throw new Exception(string.Format(
            "Could not determine member from {0}",
            expression));
    }
}