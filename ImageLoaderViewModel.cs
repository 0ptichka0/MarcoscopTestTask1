using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MarcoscopTestTask
{
    public class ImageLoaderViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Action<ImageLoaderViewModel> _onProgressChanged;

        private string _imageUrl = "";
        private BitmapImage _loadedImage;
        private string _statusText = "Готов к загрузке";
        private double _progress = 0;
        private bool _isLoading = false;
        private Visibility _imageVisibility = Visibility.Collapsed;
        private Visibility _statusVisibility = Visibility.Visible;
        private Visibility _progressVisibility = Visibility.Collapsed;

        public ImageLoaderViewModel(Action<ImageLoaderViewModel> onProgressChanged)
        {
            _httpClient = new HttpClient();
            _onProgressChanged = onProgressChanged;

            StartLoadingCommand = new RelayCommand(async () => await StartLoadingAsync(), () => CanStartLoading);
            StopLoadingCommand = new RelayCommand(StopLoading, () => CanStopLoading);
            ImageDelCommand = new RelayCommand(ImageDel, () => CanImageDel);
        }

        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                _imageUrl = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStartLoading));
                OnPropertyChanged(nameof(CanStopLoading));
                ((RelayCommand)StartLoadingCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopLoadingCommand).RaiseCanExecuteChanged();
            }
        }

        public BitmapImage LoadedImage
        {
            get => _loadedImage;
            private set
            {
                _loadedImage = value;
                OnPropertyChanged(nameof(CanImageDel));
                OnPropertyChanged();
                ((RelayCommand)ImageDelCommand).RaiseCanExecuteChanged();
            }
        }

        public string StatusText
        {
            get => _statusText;
            private set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public double Progress
        {
            get => _progress;
            private set
            {
                _progress = value;
                OnPropertyChanged();
                _onProgressChanged?.Invoke(this);
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotLoading));
                OnPropertyChanged(nameof(CanStartLoading));
                OnPropertyChanged(nameof(CanStopLoading));
                ((RelayCommand)StartLoadingCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopLoadingCommand).RaiseCanExecuteChanged();
            }
        }

        public bool IsNotLoading => !IsLoading;

        public Visibility ImageVisibility
        {
            get => _imageVisibility;
            private set
            {
                _imageVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility StatusVisibility
        {
            get => _statusVisibility;
            private set
            {
                _statusVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            private set
            {
                _progressVisibility = value;
                OnPropertyChanged();
            }
        }

        public bool CanStartLoading => !IsLoading && !string.IsNullOrWhiteSpace(ImageUrl);
        public bool CanStopLoading => IsLoading;
        public bool CanImageDel => LoadedImage != null;

        public ICommand StartLoadingCommand { get; }
        public ICommand StopLoadingCommand { get; }
        public ICommand ImageDelCommand { get; }

        private async Task StartLoadingAsync()
        {
            try
            {
                IsLoading = true;
                Progress = 0;
                StatusText = "Начинается загрузка...";
                ImageVisibility = Visibility.Collapsed;
                StatusVisibility = Visibility.Visible;
                ProgressVisibility = Visibility.Visible;

                _cancellationTokenSource = new CancellationTokenSource();

                using var response = await _httpClient.GetAsync(ImageUrl, HttpCompletionOption.ResponseHeadersRead,
                    _cancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var memoryStream = new MemoryStream();

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length,
                    _cancellationTokenSource.Token)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead, _cancellationTokenSource.Token);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        Progress = (double)downloadedBytes / totalBytes * 100;
                        StatusText = $"Загружено: {downloadedBytes / 1024:N0} KB / {totalBytes / 1024:N0} KB";
                    }
                    else
                    {
                        StatusText = $"Загружено: {downloadedBytes / 1024:N0} KB";
                    }
                }

                memoryStream.Position = 0;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = memoryStream;
                bitmap.EndInit();
                bitmap.Freeze();

                LoadedImage = bitmap;
                StatusText = "Загрузка завершена";
                ImageVisibility = Visibility.Visible;
                StatusVisibility = Visibility.Collapsed;
                Progress = 100;
            }
            catch (OperationCanceledException)
            {
                StatusText = "Загрузка отменена";
                Progress = 0;
                ImageVisibility = Visibility.Collapsed;
                StatusVisibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка: {ex.Message}";
                Progress = 0;
                ImageVisibility = Visibility.Collapsed;
                StatusVisibility = Visibility.Visible;
            }
            finally
            {
                IsLoading = false;
                ProgressVisibility = Visibility.Collapsed;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void StopLoading()
        {
            _cancellationTokenSource?.Cancel();
        }
        private void ImageDel()
        {
            LoadedImage = null; // Теперь CanImageDel вернёт false, и кнопка отключится
            ImageVisibility = Visibility.Collapsed;
            StatusVisibility = Visibility.Visible;
            StatusText = "Изображение удалено";
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _httpClient?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}