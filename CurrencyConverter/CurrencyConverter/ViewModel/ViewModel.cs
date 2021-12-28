using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CurrencyConverter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CurrencyConverter.ViewModels
{
    class CViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Currency> _currencies;
        private Currency _selectedTopCurrency;
        private Currency _selectedBottomCurrency;
        private string _topValue;
        private string _bottomValue;
        private string _selectedDate;
        private HttpClient httpClient { get; }

        public CViewModel()
        {
            httpClient = new HttpClient();
            Currencies = new ObservableCollection<Currency>();
            LoadCurrencies(DateTime.Now);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
            }
        }

        public string TopValue
        {
            get => _topValue;
            set
            {
                if (_topValue == value)
                    return;

                _topValue = value;

                CalculateBottomValue(value);
                OnPropertyChanged();
            }
        }

        public string BottomValue
        {
            get => _bottomValue;
            set
            {
                if (_bottomValue == value)
                    return;

                _bottomValue = value;

                CalculateTopValue(value);
                OnPropertyChanged();
            }
        }

        public Currency SelectedTopCurrency
        {
            get => _selectedTopCurrency;
            set
            {
                _selectedTopCurrency = value;

                CalculateTopValue(BottomValue);
                OnPropertyChanged();
            }
        }

        public Currency SelectedBottomCurrency
        {
            get => _selectedBottomCurrency;
            set
            {
                _selectedBottomCurrency = value;

                CalculateBottomValue(TopValue);
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Currency> Currencies
        {
            get => _currencies;
            set
            {
                _currencies = value;
                OnPropertyChanged();
            }
        }

        private void CalculateTopValue(string value)
        {
            if (SelectedTopCurrency == null || SelectedBottomCurrency == null || string.IsNullOrWhiteSpace(value))
            {
                _topValue = "0";
            }
            else
            {
                var res = Convert.ToDouble(value) *
                            SelectedBottomCurrency.Value /
                            SelectedBottomCurrency.Nominal /
                            (SelectedTopCurrency.Value / SelectedTopCurrency.Nominal);

                _topValue = res.ToString();
            }

            OnPropertyChanged(nameof(TopValue));
        }

        private void CalculateBottomValue(string value)
        {
            if (SelectedTopCurrency == null || SelectedBottomCurrency == null || string.IsNullOrWhiteSpace(value))
            {
                _bottomValue = "0";
            }
            else
            {
                var res = Convert.ToDouble(value) *
                            SelectedTopCurrency.Value /
                            SelectedTopCurrency.Nominal /
                            (SelectedBottomCurrency.Value / SelectedBottomCurrency.Nominal);

                _bottomValue = res.ToString();
            }
            OnPropertyChanged(nameof(BottomValue));
        }

        public DateTime CheckDateTime(DateTime date)
        {
            if (date > DateTime.Now)
                date = DateTime.Now;
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                date = date.DayOfWeek == DayOfWeek.Sunday ? date.AddDays(-2) : date.AddDays(-1);

            return date;
        }
        public async Task LoadCurrencies(DateTime date)
        {
            var dateString = CheckDateTime(date).ToString("yyyy/MM/dd");
            var response = await httpClient.GetAsync($"https://www.cbr-xml-daily.ru/archive/{dateString}/daily_json.js");
            var currenciesJson = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(currenciesJson);
            var currency = JsonConvert.DeserializeObject<Dictionary<string, Currency>>(jsonObject["Valute"]?.ToString() ?? "");

            Currencies = new ObservableCollection<Currency>(currency?.Select(x => x.Value) ?? new List<Currency>());
            Currencies.Add(new Currency("Российский рубль", 1, 1, "RUB"));
            OnPropertyChanged();
        }



        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
