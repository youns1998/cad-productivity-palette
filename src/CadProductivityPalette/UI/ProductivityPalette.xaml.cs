using System.Windows;
using System.Windows.Controls;

namespace CadProductivityPalette.UI;

public partial class ProductivityPalette : UserControl, IDisposable
{
    private readonly ProductivityViewModel _viewModel;

    public ProductivityPalette()
    {
        InitializeComponent();
        _viewModel = new ProductivityViewModel();
        DataContext = _viewModel;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    public void SetActive(bool active)
    {
        _viewModel.SetActive(active);
    }

    public void Dispose()
    {
        IsVisibleChanged -= OnIsVisibleChanged;
        _viewModel.Dispose();
        DataContext = null;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
    {
        _viewModel.SetActive(IsVisible);
    }
}
