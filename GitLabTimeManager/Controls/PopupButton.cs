using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace GitLabTimeManager.UICommon.Controls;

[TemplatePart(Name = "PART_ToggleButton", Type = typeof(ToggleButton))][TemplatePart(Name = "PART_ButtonContent", Type = typeof(ContentControlEx))][TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
public sealed class PopupButton : ContentControl
{
    private ToggleButton _toggleButton;
    private Popup _popup;

    private void SetContextMenuPlacementTarget(Popup popup)
    {
        if (_toggleButton != null)
        {
            popup.PlacementTarget = _toggleButton;
        }
    }

    #region DependencyProperty
    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(PopupButton), new FrameworkPropertyMetadata(IsExpandedPropertyChangedCallback));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(PopupButton));
    public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(nameof(CommandTarget), typeof(IInputElement), typeof(PopupButton));
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(PopupButton));

    public static readonly DependencyProperty ButtonStyleProperty = DependencyProperty.Register(nameof(ButtonStyle), typeof(Style), typeof(PopupButton), new FrameworkPropertyMetadata(default(Style), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty PopupContentProperty = DependencyProperty.Register(nameof(PopupContent), typeof(UIElement), typeof(PopupButton), new PropertyMetadata(default(UIElement)));
    public static readonly DependencyProperty AllowsTransparencyProperty = DependencyProperty.Register(nameof(AllowsTransparency), typeof(bool), typeof(PopupButton), new PropertyMetadata(false));
    public static readonly DependencyProperty PopupAnimationProperty = DependencyProperty.Register(nameof(PopupAnimation), typeof(PopupAnimation), typeof(PopupButton), new PropertyMetadata(PopupAnimation.None));
    public static readonly DependencyProperty PlacementProperty = DependencyProperty.Register(nameof(Placement), typeof(PlacementMode), typeof(PopupButton), new PropertyMetadata(PlacementMode.Bottom));
    public static readonly DependencyProperty StaysOpenProperty = DependencyProperty.Register(nameof(StaysOpen), typeof(bool), typeof(PopupButton), new PropertyMetadata(false));
    public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(PopupButton), new PropertyMetadata(.0));
    public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.Register(nameof(VerticalOffset), typeof(double), typeof(PopupButton), new PropertyMetadata(.0));

    #endregion

    #region PROPERTIES
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
    public IInputElement CommandTarget
    {
        get => (IInputElement)GetValue(CommandTargetProperty);
        set => SetValue(CommandTargetProperty, value);
    }
    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public Style ButtonStyle
    {
        get => (Style)GetValue(ButtonStyleProperty);
        set => SetValue(ButtonStyleProperty, value);
    }
    public UIElement PopupContent
    {
        get => (UIElement)GetValue(PopupContentProperty);
        set => SetValue(PopupContentProperty, value);
    }
    public bool AllowsTransparency
    {
        get => (bool)GetValue(AllowsTransparencyProperty);
        set => SetValue(AllowsTransparencyProperty, value);
    }
    public PopupAnimation PopupAnimation
    {
        get => (PopupAnimation)GetValue(PopupAnimationProperty);
        set => SetValue(PopupAnimationProperty, value);
    }
    public PlacementMode Placement
    {
        get => (PlacementMode)GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }
    public bool StaysOpen
    {
        get => (bool)GetValue(StaysOpenProperty);
        set => SetValue(StaysOpenProperty, value);
    }

    public double HorizontalOffset
    {
        get => (double) GetValue(HorizontalOffsetProperty);
        set => SetValue(HorizontalOffsetProperty, value);
    }

    public double VerticalOffset
    {
        get => (double) GetValue(VerticalOffsetProperty);
        set => SetValue(VerticalOffsetProperty, value);
    }

    #endregion

    static PopupButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupButton), new FrameworkPropertyMetadata(typeof(PopupButton)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _toggleButton = EnforceInstance<ToggleButton>("PART_ToggleButton");
        _popup = EnforceInstance<Popup>("PART_Popup");
        InitializeVisualElementsContainer();
    }

    private T EnforceInstance<T>(string partName) where T : FrameworkElement, new()
    {
        T element = GetTemplateChild(partName) as T ?? new T();
        return element;
    }

    private void InitializeVisualElementsContainer()
    {
        MouseRightButtonUp -= DropDownButtonMouseRightButtonUp;
        MouseRightButtonUp += DropDownButtonMouseRightButtonUp;
    }

    private void DropDownButtonMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private static void IsExpandedPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
        PopupButton button = (PopupButton)dependencyObject;
        button.SetContextMenuPlacementTarget(button._popup);
    }
}