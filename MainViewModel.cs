using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MarcoscopTestTask
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const int MinFields = 1;
        private const int MaxFields = 10;

        private string _overallStatusText = "Готов к работе";
        private double _overallProgress = 0;
        private Visibility _overallProgressVisibility = Visibility.Collapsed;
        private string _statusBarText = "Готов к работе";

        public MainViewModel()
        {
            ImageLoaders = new ObservableCollection<ImageLoaderViewModel>();

            // Добавляем начальные поля
            for (int i = 0; i < 3; i++)
            {
                AddImageLoader();
            }

            AddFieldCommand = new RelayCommand(AddField, () => CanAddField);
            RemoveFieldCommand = new RelayCommand(RemoveField, () => CanRemoveField);
            LoadAllCommand = new RelayCommand(async () => await LoadAllAsync(), () => CanLoadAll);
            StopAllCommand = new RelayCommand(StopAll, () => CanStopAll);
        }

        public ObservableCollection<ImageLoaderViewModel> ImageLoaders { get; }

        public string OverallStatusText
        {
            get => _overallStatusText;
            private set
            {
                _overallStatusText = value;
                OnPropertyChanged();
            }
        }

        public double OverallProgress
        {
            get => _overallProgress;
            private set
            {
                _overallProgress = value;
                OnPropertyChanged();
            }
        }

        public Visibility OverallProgressVisibility
        {
            get => _overallProgressVisibility;
            private set
            {
                _overallProgressVisibility = value;
                OnPropertyChanged();
            }
        }

        public string StatusBarText
        {
            get => _statusBarText;
            private set
            {
                _statusBarText = value;
                OnPropertyChanged();
            }
        }

        public bool CanAddField => ImageLoaders.Count < MaxFields;
        public bool CanRemoveField => ImageLoaders.Count > MinFields;
        public bool CanLoadAll => ImageLoaders.Any(loader => loader.CanStartLoading);
        public bool CanStopAll => ImageLoaders.Any(loader => loader.CanStopLoading);

        public ICommand AddFieldCommand { get; }
        public ICommand RemoveFieldCommand { get; }
        public ICommand LoadAllCommand { get; }
        public ICommand StopAllCommand { get; }

        private void AddField()
        {
            if (CanAddField)
            {
                AddImageLoader();
                UpdateCommandStates();
            }
        }

        private void RemoveField()
        {
            if (CanRemoveField)
            {
                var lastLoader = ImageLoaders.LastOrDefault();
                if (lastLoader != null)
                {
                    lastLoader.PropertyChanged -= OnImageLoaderPropertyChanged;
                    lastLoader.Dispose();
                    ImageLoaders.Remove(lastLoader);
                    UpdateCommandStates();
                    UpdateOverallProgress();
                }
            }
        }

        private void AddImageLoader()
        {
            var loader = new ImageLoaderViewModel(OnLoaderProgressChanged);
            loader.PropertyChanged += OnImageLoaderPropertyChanged;
            ImageLoaders.Add(loader);
        }

        private void OnImageLoaderPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageLoaderViewModel.ImageUrl) ||
                e.PropertyName == nameof(ImageLoaderViewModel.IsLoading) ||
                e.PropertyName == nameof(ImageLoaderViewModel.CanStartLoading) ||
                e.PropertyName == nameof(ImageLoaderViewModel.CanStopLoading) ||
                e.PropertyName == nameof(ImageLoaderViewModel.CanImageDel))
            {
                UpdateCommandStates();
            }
        }

        private async Task LoadAllAsync()
        {
            var loadableLoaders = ImageLoaders
                .Where(loader => loader.CanStartLoading)
                .ToList();

            foreach (var loader in loadableLoaders)
            {
                loader.StartLoadingCommand.Execute(null);
            }

            await Task.CompletedTask;
        }

        private void StopAll()
        {
            foreach (var loader in ImageLoaders.Where(loader => loader.CanStopLoading))
            {
                loader.StopLoadingCommand.Execute(null);
            }
        }

        private void OnLoaderProgressChanged(ImageLoaderViewModel loader)
        {
            UpdateOverallProgress();
            UpdateCommandStates();
        }

        private void UpdateOverallProgress()
        {
            var loadingLoaders = ImageLoaders.Where(loader => loader.IsLoading).ToList();

            if (loadingLoaders.Any())
            {
                var averageProgress = loadingLoaders.Average(loader => loader.Progress);
                OverallProgress = averageProgress;
                OverallProgressVisibility = Visibility.Visible;
                OverallStatusText = $"Загружается {loadingLoaders.Count} из {ImageLoaders.Count} изображений";
                StatusBarText = $"Общий прогресс: {averageProgress:F1}%";
            }
            else
            {
                OverallProgress = 0;
                OverallProgressVisibility = Visibility.Collapsed;

                var completedCount = ImageLoaders.Count(loader => loader.LoadedImage != null);
                OverallStatusText = $"Загружено {completedCount} из {ImageLoaders.Count} изображений";
                StatusBarText = completedCount == ImageLoaders.Count ? "Все изображения загружены" : "Готов к работе";
            }
        }

        private void UpdateCommandStates()
        {
            OnPropertyChanged(nameof(CanAddField));
            OnPropertyChanged(nameof(CanRemoveField));
            OnPropertyChanged(nameof(CanLoadAll));
            OnPropertyChanged(nameof(CanStopAll));

            ((RelayCommand)AddFieldCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RemoveFieldCommand).RaiseCanExecuteChanged();
            ((RelayCommand)LoadAllCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopAllCommand).RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}