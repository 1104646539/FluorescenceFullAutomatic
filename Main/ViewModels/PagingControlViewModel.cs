using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class PagingControlViewModel : ObservableObject
    {
        private int _currentPage = 1;
        [ObservableProperty]
        private int totalPages = 1;

        [ObservableProperty]
        private int pageSize = 20;

        [ObservableProperty]
        private string targetPage = "1";

        [ObservableProperty]
        private int page1;

        [ObservableProperty]
        private int page2;

        [ObservableProperty]
        private int page3;

        [ObservableProperty]
        private int page4;

        [ObservableProperty]
        private int page5;

        [ObservableProperty]
        private bool showPage1;

        [ObservableProperty]
        private bool showPage2;

        [ObservableProperty]
        private bool showPage3;

        [ObservableProperty]
        private bool showPage4;

        [ObservableProperty]
        private bool showPage5;

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                int newPage = value;
                if (newPage < 1)
                {
                    newPage = 1;
                }
                else if (newPage > TotalPages)
                {
                    newPage = TotalPages;
                }

                if (_currentPage != newPage)
                {
                    _currentPage = newPage;
                    OnPropertyChanged();
                    PageChanged?.Invoke(_currentPage);
                    TargetPage = ""+_currentPage;
                }
            }
        }

        public event Action<int> PageChanged;

        public PagingControlViewModel()
        {
            UpdatePageNumbers();
        }

        [RelayCommand]
        private void FirstPage()
        {
            CurrentPage = 1;
            UpdatePageNumbers();
            PageChanged?.Invoke(CurrentPage);
        }

        [RelayCommand]
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdatePageNumbers();
                PageChanged?.Invoke(CurrentPage);
            }
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                UpdatePageNumbers();
                PageChanged?.Invoke(CurrentPage);
            }
        }

        [RelayCommand]
        private void LastPage()
        {
            CurrentPage = TotalPages;
            UpdatePageNumbers();
            PageChanged?.Invoke(CurrentPage);
        }

        [RelayCommand]
        private void GoToPage()
        {
            if (int.TryParse(TargetPage, out int page))
            {
                if (page < 1)
                {
                    page = 1;
                }
                else if (page > TotalPages)
                {
                    page = TotalPages;
                }
                
                CurrentPage = page;
                TargetPage = ""+ CurrentPage;
                PageChanged?.Invoke(CurrentPage);
            }
        }

        private void UpdatePageNumbers()
        {
            int startPage = Math.Max(1, CurrentPage - 2);
            int endPage = Math.Min(TotalPages, startPage + 4);
            startPage = Math.Max(1, endPage - 4);

            Page1 = startPage;
            Page2 = startPage + 1;
            Page3 = startPage + 2;
            Page4 = startPage + 3;
            Page5 = startPage + 4;

            ShowPage1 = Page1 <= TotalPages;
            ShowPage2 = Page2 <= TotalPages;
            ShowPage3 = Page3 <= TotalPages;
            ShowPage4 = Page4 <= TotalPages;
            ShowPage5 = Page5 <= TotalPages;
        }

        public void SetTotalPages(int totalPages)
        {
            TotalPages = totalPages;
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
            UpdatePageNumbers();
        }

        public void SetPageSize(int pageSize)
        {
            PageSize = pageSize;
        }
    }
}
