using System;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace OpenSilverApplication1.ViewModels
{
    public class MainViewModel: ObservableObject
    {
        private int _counter;
        public int Counter
        {
            get => _counter;
            private set => SetProperty(ref _counter, value);
        }
        public ICommand IncrementCounterCommand { get; }

        private string _weather;
        public string Weather
        {
            get => _weather;
            set => SetProperty(ref _weather, value);
        }
        public ICommand GetWeatherCommand { get; }


        public MainViewModel(HttpClient httpClient)
        {
            IncrementCounterCommand = new RelayCommand(() => Counter++);
            GetWeatherCommand = new AsyncRelayCommand(async () =>
            {
                var response = await
                    httpClient.GetAsync(
                        new Uri("https://www.jma.go.jp/bosai/forecast/data/overview_forecast/130000.json"));
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ForecastResult>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                Weather = result.Text;
            });
        }
    }

    public class ForecastResult
    {
        public string PublishingOffice { get; set; }
        public DateTime ReportDatetime { get; set; }
        public string TargetArea { get; set; }
        public string HeadlineText { get; set; }
        public string Text { get; set; }
    }

    
}
