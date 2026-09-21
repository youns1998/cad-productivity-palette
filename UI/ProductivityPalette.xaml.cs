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
    }

    public void Refresh()
    {
        _viewModel.RefreshAll();
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        DataContext = null;
    }
}
