using MacroBackendTest_Backend.Models.Entities;
using Microsoft.AspNetCore.Http;
using NuGet.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FrontEnd;


/*
 
  In real app, we shoud have:
  * data paging supported (paging, not full data, as full database can be very big);
  * login/logout
  * 

 */


public partial class MainWindow : Window
{

    public MainWindow()
    {
        InitializeComponent();
    }

    private void uiBaseUri_TextChanged(object sender, TextChangedEventArgs e)
    {
        bool hasLink = uiBaseUri.Text.Length > 10;
        uiGetDefault.IsEnabled = hasLink;
        uiGetVersion.IsEnabled = hasLink;
        uiGetCount.IsEnabled = hasLink;
        uiGetPage.IsEnabled = hasLink;
        uiAddButton.IsEnabled = hasLink;
    }

    private string GetBaseUri()
    {
        string temp = uiBaseUri.Text;
        if (string.IsNullOrEmpty(temp)) return "";
        if (temp.Length < 10) return "";

        if (temp.EndsWith("api/")) return temp;
        if (temp.EndsWith("api")) return temp + "/";
        return temp + "api/";
    }

    


    #region "Basic commands"
    private async void uiGetDefault_Clicked(object sender, RoutedEventArgs e)
    {
        string uri = GetBaseUri();
        if(string.IsNullOrEmpty(uri)) return;
        MessageBox.Show(await _client.GetStringAsync(uri ));
    }

    private async void uiGetVersion_Clicked(object sender, RoutedEventArgs e)
    {
        string uri = GetBaseUri();
        if (string.IsNullOrEmpty(uri)) return;
        MessageBox.Show(await _client.GetStringAsync(uri + "vers"));
    }

    private async void uiGetCount_Clicked(object sender, RoutedEventArgs e)
    {
        string uri = GetBaseUri();
        if (string.IsNullOrEmpty(uri)) return;
        MessageBox.Show(await _client.GetStringAsync(uri + "count"));

    }
    #endregion



    #region "datapage operations"

    private List<Person>? _personsDB;
    private List<PersonUI>? _personsUI;

    private async void uiGetPage_Clicked(object sender, RoutedEventArgs e)
    {
        string uri = GetBaseUri();
        if (string.IsNullOrEmpty(uri)) return;

        string json = await _client.GetStringAsync(uri + "page/0");
        if(string.IsNullOrEmpty(json)) return;
        if (json.Length < 10) return;

        try
        {
            _personsDB = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Person>>(json);
            _personsUI = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PersonUI>>(json);
        }
        catch (Exception ex)
        {
        MessageBox.Show($"Error: {ex.Message}");
        }

        if(_personsUI is null) return;

        uiListItems.ItemsSource = _personsUI.ToList();

    }

    private async void uiSave_Clik(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Save changes now?", Application.Current.MainWindow.GetType().Assembly.GetName().Name, MessageBoxButton.YesNo) == MessageBoxResult.No) return;

        string uri = GetBaseUri();
        if (string.IsNullOrEmpty(uri)) return;

        foreach(Person orgPerson in _personsDB!)
        {
            PersonUI? personUI = _personsUI!.Where(p=>p.ID == orgPerson.ID).FirstOrDefault();
            if(personUI is null)
            { // delete this item
                var result = await _client.GetAsync(uri + $"del/{orgPerson.ID}");
                if (result.IsSuccessStatusCode) continue;

                // some error handling here...
            }
        }


        bool someErrors = false;


        foreach (PersonUI personUI in _personsUI!)   // it cannot be null here, as we are called as event from this
        {
            if (!personUI.editable) continue;

            string json = personUI.ToJson();

            if (personUI.ID == -1)
            {
                // user is to be added 

                var result = await _client.PostAsJsonAsync(uri, personUI);
                if (result.IsSuccessStatusCode)
                {
                    string res = await result.Content.ReadAsStringAsync();
                    int id = 0;
                    if (int.TryParse(res, out id))
                    {
                        personUI.editable = false;
                        personUI.ID = id;
                        continue;
                    }
                }

                someErrors = true;
            }
            else
            {
                // user is to be changed
                var result = await _client.PutAsJsonAsync(uri, personUI);
                if (result.IsSuccessStatusCode)
                    personUI.editable = false;
                else
                    someErrors = true;
            }

        }

        if (someErrors)
            MessageBox.Show("Some errors occured - you can try Save again");
        else
            SaveCancelEnable(false);

        uiListItems.ItemsSource = _personsUI.ToList();  // some items would be still editable

    }

    private void uiCancel_Clik(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Really cancel all changes?", Application.Current.MainWindow.GetType().Assembly.GetName().Name, MessageBoxButton.YesNo) == MessageBoxResult.No) return;

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(_personsDB);
        _personsUI = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PersonUI>>(json);
        uiListItems.ItemsSource = _personsUI!.ToList(); // it cannot be null here
    }
    #endregion

    #region "http helper"
    private HttpClient _client = new HttpClient();
    #endregion

    #region "context menu"

    private void SaveCancelEnable(bool isenabled)
    {
        uiCancel.IsEnabled = isenabled;
        uiSave.IsEnabled = isenabled;
    }

    private void uiDelete_Click(object sender, RoutedEventArgs e)
    {
        FrameworkElement fe = (FrameworkElement)sender;
        PersonUI? person = fe.DataContext as PersonUI;
        if(person == null) return;

        _personsUI!.Remove(person); // it cannot be null here - it is called as event from _personsUI
        uiListItems.ItemsSource = _personsUI!.ToList(); // it cannot be null here

        SaveCancelEnable(true);
    }

    private void uiAllowEdits_Click(object sender, RoutedEventArgs e)
    {
        FrameworkElement fe = (FrameworkElement)sender;
        PersonUI? person = fe.DataContext as PersonUI;
        if (person == null) return;

        person.editable= true;
        uiListItems.ItemsSource = _personsUI!.ToList(); // it cannot be null here

        SaveCancelEnable(true);
    }

    private void uiAddPerson_Click(object sender, RoutedEventArgs e)
    {
        // -10 years should make selecting date quicker than starting from DateTime.Min, but also we probably doesn't add infants (as we require phone number)
        _personsUI!.Add(new PersonUI() { ID=-1, editable=true, DateOfBirth=DateTime.Now.AddYears(-10)});
        uiListItems.ItemsSource = _personsUI!.ToList();

        SaveCancelEnable(true);
    }

    #endregion


}

public class PersonUI : Person
{
    // adding one field, it would be helper for UI and also for recognizing edited elements
    public bool editable { get; set; } = false;
}

#region "XAML value converters"

public class ConvertAge : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int temp = (int)value;

        return $"({temp} years)";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ConvertNegBool : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool temp = (bool)value;
        return !temp;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

}
#endregion