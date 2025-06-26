using System.Windows.Input;

namespace FluorescenceFullAutomatic.ViewModels
{
    public class MenuItemViewModel
    {
        public string Header { get; set; }
        public ICommand Command { get; set; }
    }
} 