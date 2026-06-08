using BudgetManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BudgetManagement.Views
{
    /// <summary>
    /// SampleView.xaml の相互作用ロジック
    /// </summary>
    public partial class SampleView : Page
    {
        public SampleView()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetRequiredService<SampleViewModel>();
        }
    }
}
