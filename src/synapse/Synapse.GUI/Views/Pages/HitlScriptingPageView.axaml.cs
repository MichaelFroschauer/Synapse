using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace Synapse.GUI.Views.Pages;

public partial class HitlScriptingPageView : UserControl
{
    public HitlScriptingPageView()
    {
        InitializeComponent();
        this.Loaded += (_,_) => InitializeCodeEditor();
    }
    
    private void InitializeCodeEditor()
    {
        var textEditor = this.FindControl<TextEditor>("Editor");
        if (textEditor == null) return;
        
        //var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var registryOptions = new RegistryOptions(ThemeName.LightPlus);
        var textMateInstallation = textEditor.InstallTextMate(registryOptions);
        textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".cs").Id));
        textEditor.WordWrap = true;
    }
}
