using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.FileSystem;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel;
using JetBrains.DataFlow;
using JetBrains.IDE.UI;
using JetBrains.IDE.UI.Extensions;
using JetBrains.IDE.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Daemon.OptionPages;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;
using JetBrains.ReSharper.UnitTestFramework.Resources;
using JetBrains.Rider.Model.UIAutomation;

namespace ReSharperPlugin.MvvmPlugin.Options;

[OptionsPage(PID, PageTitle, typeof(UnitTestingThemedIcons.Session),
    // Discover derived types of AEmptyOptionsPage
    ParentId = CodeEditingPage.PID)]
// Inline options page into another options page
// [OptionsPage(PID, PageTitle, typeof(OptionsThemedIcons.EnvironmentGeneral),
//     ParentId = CodeInspectionPage.PID,
//     NestingType = OptionPageNestingType.Inline,
//     IsAlignedWithParent = true,
//     Sequence = 0.1d)]
public class MvvmPluginOptionsPage : BeSimpleOptionsPage
{
    private const string PID = nameof(MvvmPluginOptionsPage);
    private const string PageTitle = "Mvvm Plugin";

    private readonly Lifetime _lifetime;

    public MvvmPluginOptionsPage(Lifetime lifetime,
        OptionsPageContext optionsPageContext,
        OptionsSettingsSmartContext optionsSettingsSmartContext,
        IconHostBase iconHost,
        ICommonFileDialogs dialogs)
        : base(lifetime, optionsPageContext, optionsSettingsSmartContext)
    {
        _lifetime = lifetime;

        // Add additional search keywords
        //AddKeyword("Sample", "Example", "Preferences"); // TODO: only works for ReSharper?

        AddText("These are options for the Mvvm Plugin");
        AddSpacer();
        AddCommentText("Values are saved in a .dotSettings file.");
        
        

        AddHeader("CommunityToolkit Options");

        //AddTextBox((MvvmPluginSettings x) => x.ObservableBaseObject, "Base object for an observable object");
        // AddIntOption((SampleSettings x) => x.Integer, "Integer value");
        // AddBoolOption((SampleSettings x) => x.Boolean, "Boolean value");
        //
        // AddHeader("Advanced Options");
        //
        AddRadioOption((MvvmPluginSettings x) => x.PreferredBaseObservable, "Enum value",
            Enum.GetValues(typeof(ObservableObjectBaseType)).Cast<int>()
                .Select(x => (Value: x, Name: Enum.GetName(typeof(ObservableObjectBaseType), x)))
                .Select(x => new RadioOptionPoint(x.Value, x.Name)).ToArray());
        
        AddCommentText("If you choose other. When making an object observable the code will look for a base class that matches the names defined below (seperated by , ) ");
        
        AddTextBox((MvvmPluginSettings x) => x.OtherValuesString, "Names of base classes (case insensitive)");
        
        
        // AddComboEnum((SampleSettings x) => x.ComboSelection, "Combo enum value", x => x.ToString());
        //
        // // var property = new Property<string>(lifetime, $"{nameof(SampleSettings)}:{nameof(SampleSettings.FolderPath)}");
        // // optionsSettingsSmartContext.SetBinding(lifetime, (SampleSettings x) => x.FolderPath, property);
        // AddFolderChooserOption(
        //     (SampleSettings x) => x.FolderPath,
        //     id: nameof(SampleSettings.FolderPath),
        //     initialValue: FileSystemPath.Empty,
        //     iconHost,
        //     dialogs);
    }

    private BeTextBox AddTextBox<TKeyClass>(Expression<Func<TKeyClass, string>> lambdaExpression, string description)
    {
        var property = new Property<string>(description);
        OptionsSettingsSmartContext.SetBinding(_lifetime, lambdaExpression, property);
        var control = property.GetBeTextBox(_lifetime);
        AddControl(control.WithDescription(description, _lifetime));
        return control;
    }
}