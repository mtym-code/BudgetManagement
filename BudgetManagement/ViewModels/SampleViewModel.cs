using BudgetManagement.Common.Helper;
using BudgetManagement.Models;
using BudgetManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;

namespace BudgetManagement.ViewModels
{
    public partial class SampleViewModel : ViewModelBase
    {
        private readonly UserService _service;

        public SampleViewModel(UserService service)
        {
            _service = service;
        }

        [ObservableProperty]
        private ObservableCollection<User> users = new();

        [RelayCommand]
        private async Task LoadUsers()
        {
            try
            {
                var users = await _service.GetUsersAsync();

                Users = new ObservableCollection<User>(users);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "ユーザ取得失敗");
                MessageBox.Show("ユーザ取得に失敗しました");
            }
        }
    }
}