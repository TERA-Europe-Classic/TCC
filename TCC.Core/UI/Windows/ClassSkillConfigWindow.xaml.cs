using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
using TCC.Data.Skills;
using TCC.ViewModels.Widgets;

namespace TCC.UI.Windows;

public partial class ClassSkillConfigWindow
{
    private static ClassSkillConfigWindow? _instance;
    public static ClassSkillConfigWindow Instance => _instance ?? new ClassSkillConfigWindow();

    private ClassWindowViewModel VM { get; }

    public ClassSkillConfigWindow()
        : base(true)
    {
        _instance = this;

        InitializeComponent();
        DataContext = WindowManager.ViewModels.ClassVM;
        VM = (ClassWindowViewModel)DataContext;
    }

    public ClassExtraSkillDropHandler ExtraSkillDropHandler => new();

    public override void HideWindow()
    {
        FocusManager.ForceFocused = false;
        if (VM.Settings != null)
        {
            VM.Settings.ForcedClickable = false;
            VM.Settings.ForcedVisible = false;
        }

        base.HideWindow();
        VM.SaveExtraSkills();
    }

    public override void ShowWindow()
    {
        FocusManager.ForceFocused = true;
        if (VM.Settings != null)
        {
            VM.Settings.ForcedClickable = true;
            VM.Settings.ForcedVisible = true;
        }

        base.ShowWindow();
    }

    private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void ClosewWindow(object? sender, RoutedEventArgs? e)
    {
        HideWindow();
    }

    private void SkillSearch_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (VM.SkillsView == null) return;
        var view = (ICollectionView)VM.SkillsView;
        view.Filter = o => ((Skill)o).ShortName.IndexOf(((TextBox)sender).Text, StringComparison.InvariantCultureIgnoreCase) != -1;
        view.Refresh();
    }

    private void RemoveExtraSkill(object sender, RoutedEventArgs e)
    {
        VM.RemoveExtraSkill((Cooldown)((Button)sender).DataContext);
    }
}

public class ClassExtraSkillDropHandler : IDropTarget
{
    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is Skill or Cooldown)
        {
            dropInfo.Effects = DragDropEffects.Move;
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        switch (dropInfo.Data)
        {
            case Skill skill:
                WindowManager.ViewModels.ClassVM.AddExtraSkill(skill, dropInfo.InsertIndex);
                break;
            case Cooldown cooldown:
                WindowManager.ViewModels.ClassVM.MoveExtraSkill(cooldown, dropInfo.InsertIndex);
                break;
        }
    }
}
