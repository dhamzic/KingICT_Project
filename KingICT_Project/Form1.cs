﻿using KingICT_Project.Flight_classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KingICT_Project
{
    public partial class UiMainForm : Form
    {
        public string selectedOrigin;
        public string selectedDestination;
        private string accessToken;
        private Flights flightConditions = new Flights();
        DataBaseOp dbAccess = new DataBaseOp();
        public UiMainForm()
        {
            accessToken = GetAuthorizationData().Result;

            InitializeComponent();

            CurrencyFilling();
            IataTableFilling();
            ControLInitialSettings();
        }

        /// <summary>
        /// This function sets a minimum value of the date picker control to today's date
        /// </summary>
        private void ControLInitialSettings()
        {
            UiDepartureDateTimePicker.MinDate = DateTime.Now;
            UiReturnDateTimePicker.MinDate = DateTime.Now;
        }

        /// <summary>
        /// This function fills combo box control with allowed currency values
        /// </summary>
        private void CurrencyFilling()
        {
            List<string> CurrencyList = new List<string>();
            CurrencyList.Add("USD");
            CurrencyList.Add("EUR");
            CurrencyList.Add("HRK");
            UiCurrencyComboBox.DataSource = null;
            UiCurrencyComboBox.DataSource = CurrencyList;
        }

        /// <summary>
        /// This function fills table control with fetched Iata codes values
        /// </summary>
        private void IataTableFilling()
        {
            UiOriginIataCodeGridView.DataSource = null;
            UiOriginIataCodeGridView.DataSource = dbAccess.FetchSpecificIataCodes(String.Empty);
        }

        /// <summary>
        /// This function fills table control with fetched flights from the Amadeus Web Service
        /// </summary>
        /// <param name="flightsObject"></param>
        private void TableFillingWithWebServiceData(Flight flightsObject)
        {
            UiSearchResultDataGridView.DataSource = null;

            if (flightsObject.Data != null)
            {
                List<Flights> listOfFlights = new List<Flights>();
                foreach (var flight in flightsObject.Data)
                {
                    listOfFlights.Add(new Flights
                    {
                        Origin = flightConditions.Origin.ToString(),
                        Destination = flightConditions.Destination.ToString(),
                        StopsIngoing = flight.OfferItems[0].Services[1].Segments.Count() - 1,
                        StopsOutgoing = flight.OfferItems[0].Services[0].Segments.Count() - 1,
                        DepartureDate = UiDepartureDateTimePicker.Value.Date,
                        ReturnDate = UiReturnDateTimePicker.Value.Date,
                        AdultAvailabilityOutgoing = flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerAdult == null ? "empty" : flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerAdult.Availability.ToString(),
                        ChildrenAvailabilityOutgoing = flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerChild == null ? "empty" : flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerChild.Availability.ToString(),
                        InfantsAvailabilityOutgoing = flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerInfant == null ? "empty" : flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerInfant.Availability.ToString(),
                        SeniorsAvailabilityOutgoing = flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerSenior == null ? "empty" : flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerSenior.Availability.ToString(),
                        AdultAvailabilityIngoing = flight.OfferItems[0].Services[1].Segments[0].PricingDetailPerAdult == null ? "empty" : flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerAdult.Availability.ToString(),
                        ChildrenAvailabilityIngoing = flight.OfferItems[0].Services[1].Segments[0].PricingDetailPerChild == null ? "empty" : flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerChild.Availability.ToString(),
                        InfantsAvailabilityIngoing = flight.OfferItems[0].Services[1].Segments[0].PricingDetailPerInfant == null ? "empty" : flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerInfant.Availability.ToString(),
                        SeniorsAvailabilityIngoing = flight.OfferItems[0].Services[1].Segments[0].PricingDetailPerSenior == null ? "empty" : flight.OfferItems[0].Services[0].Segments[0].PricingDetailPerSenior.Availability.ToString(),
                        Price = double.Parse(flight.OfferItems[0].Price.Total, CultureInfo.InvariantCulture),
                        Currency = UiCurrencyComboBox.SelectedItem.ToString(),
                        InsertedAdults = int.Parse(UiAdultsTextBox.Text),
                        InsertedChildren = int.Parse(UiChildrenTextBox.Text),
                        InsertedInfants = int.Parse(UiInfantsTextBox.Text),
                        InsertedSeniors = int.Parse(UiSeniorsTextBox.Text)
                    });



                }
                UiSearchResultDataGridView.DataSource = listOfFlights.OrderBy(o => o.Price).ToList();
                UiTotalRowsLabel.Text = "Total rows: " + listOfFlights.Count + "";
                UiTotalRowsLabel.Visible = true;
                dbAccess.InsertFlights(listOfFlights);
                UiFetchedFromLabel.Text = "== Fetched from an Amadeus Web Service ==";
                UiFetchedFromLabel.Visible = true;
            }
        }

        /// <summary>
        /// This function fills table control with fetched flights from a local database
        /// </summary>
        /// <param name="listOfFlightsFetchedFromDB"></param>
        private void TableFillingWithDatabaseData(List<Flights> listOfFlightsFetchedFromDB)
        {
            UiSearchResultDataGridView.DataSource = null;
            UiSearchResultDataGridView.DataSource = listOfFlightsFetchedFromDB.OrderBy(o => o.Price).ToList();

            UiFetchedFromLabel.Text = "== Fetched from a Local Database ==";
            UiFetchedFromLabel.Visible = true;
        }


        /// <summary>
        /// Access Token has an expiring deadline so, every time before fetching data from Amadeus web service, the app needs to fetch newest access token.
        /// </summary>
        private async Task<string> GetAuthorizationData()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, @"https://test.api.amadeus.com/v1/security/oauth2/token");

            var keyValues = new List<KeyValuePair<string, string>>();
            keyValues.Add(new KeyValuePair<string, string>("client_id", "NmcNIXuL3qvZ8QdruHbAAnKMyAdtRrKP"));
            keyValues.Add(new KeyValuePair<string, string>("client_secret", "7oGpMoWMCug04nUi"));
            keyValues.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));

            request.Content = new FormUrlEncodedContent(keyValues);
            var response = await client.SendAsync(request).Result.Content.ReadAsStringAsync();
            Authorization userAuthorization = JsonConvert.DeserializeObject<Authorization>(response);
            return userAuthorization.access_token;
        }

        /// <summary>
        /// The function that fetches the JSON string from Amadeus web service and stores that JSON to an object of the class.
        /// </summary>
        private async Task<Flight> GetDataFromAmadeusWebService(string accessToken)
        {
            Flight flightsObject = new Flight();
            List<string> listOfParameters = new List<string>();

            //Adding parameters to list of parameters
            if (flightConditions.Origin != null)
            {
                listOfParameters.Add("origin=" + flightConditions.Origin + "");
            }
            if (flightConditions.Destination != null)
            {
                listOfParameters.Add("destination=" + flightConditions.Destination + "");
            }
            if (UiDepartureDateTimePicker.Value != null)
            {
                listOfParameters.Add("departureDate=" + flightConditions.DepartureDate.ToString("yyyy-MM-dd") + "");
            }
            if (UiReturnDateTimePicker.Value != null)
            {
                listOfParameters.Add("returnDate=" + flightConditions.ReturnDate.ToString("yyyy-MM-dd") + "");
            }
            if (UiAdultsTextBox.Text != "" && int.Parse(UiAdultsTextBox.Text) > 0)
            {
                listOfParameters.Add("adults=" + UiAdultsTextBox.Text + "");
            }
            if (UiChildrenTextBox.Text != "" && int.Parse(UiChildrenTextBox.Text) > 0)
            {
                listOfParameters.Add("children=" + UiChildrenTextBox.Text + "");
            }
            if (UiInfantsTextBox.Text != "" && int.Parse(UiInfantsTextBox.Text) > 0)
            {
                listOfParameters.Add("infants=" + UiInfantsTextBox.Text + "");
            }
            if (UiSeniorsTextBox.Text != "" && int.Parse(UiSeniorsTextBox.Text) > 0)
            {
                listOfParameters.Add("seniors=" + UiSeniorsTextBox.Text + "");
            }
            if (flightConditions.Currency != "")
            {
                listOfParameters.Add("currency=" + flightConditions.Currency + "");
            }

            string urlParameters = String.Join("&", listOfParameters);
            string pageUrl = @"https://test.api.amadeus.com/v1/shopping/flight-offers?" + urlParameters + "";


            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using (HttpResponseMessage response = await client.GetAsync(pageUrl))
            using (HttpContent content = response.Content)
            {
                string json = await content.ReadAsStringAsync();
                if (json != null)
                {
                    flightsObject = JsonConvert.DeserializeObject<Flight>(json);
                    if (flightsObject.Errors != null)
                    {
                        List<string> listOfErrors = new List<string>();
                        int counter = 1;
                        foreach (var error in flightsObject.Errors)
                        {
                            listOfErrors.Add(counter + ". " + error.Detail + " (" + error.Source.Parameter + ")");
                            counter++;
                        }
                        string errors = string.Join("\n", listOfErrors);
                        MessageBox.Show(errors, "Insert Error");
                    }
                }
            }
            return flightsObject;
        }

        /// <summary>
        /// This function refreshes Listbox with origin and destination values.
        /// </summary>
        private void RefreshRouteListBox()
        {
            UiCurrentSelectionListBox.Items.Clear();
            UiCurrentSelectionListBox.Items.Add(flightConditions.Origin + " >> " + flightConditions.Destination);
        }


        private void UiOriginButton_Click(object sender, EventArgs e)
        {
            Iata_airport_codes selectedGridItem = (Iata_airport_codes)UiOriginIataCodeGridView.CurrentRow.DataBoundItem;
            selectedOrigin = selectedGridItem.Code;
            flightConditions.Origin = selectedOrigin;
            RefreshRouteListBox();
        }

        private void UiDestinationButton_Click(object sender, EventArgs e)
        {
            Iata_airport_codes selectedGridItem = (Iata_airport_codes)UiOriginIataCodeGridView.CurrentRow.DataBoundItem;
            selectedDestination = selectedGridItem.Code;
            flightConditions.Destination = selectedDestination;
            RefreshRouteListBox();
        }

        private void UiSearchButton_Click(object sender, EventArgs e)
        {
            //Check Passenger values. Check if input is integer.
            int value;
            if (int.TryParse(UiAdultsTextBox.Text, out value))
            {


            }
            else
            {
                MessageBox.Show("Value of the adults number must be integer number.");
                return;
            }
            if (int.TryParse(UiSeniorsTextBox.Text, out value))
            {


            }
            else
            {
                MessageBox.Show("Value of the seniors number must be integer number.");
                return;
            }
            if (int.TryParse(UiInfantsTextBox.Text, out value))
            {


            }
            else
            {
                MessageBox.Show("Value of the infants number must be integer number.");
                return;
            }
            if (int.TryParse(UiChildrenTextBox.Text, out value))
            {


            }
            else
            {
                MessageBox.Show("Value of the children number must be integer number.");
                return;
            }




            string selectedCurrency = UiCurrencyComboBox.SelectedValue.ToString();
            TaskFactory tf = new TaskFactory();

            flightConditions.Currency = selectedCurrency;
            flightConditions.DepartureDate = UiDepartureDateTimePicker.Value.Date;
            flightConditions.ReturnDate = UiReturnDateTimePicker.Value.Date;
            flightConditions.InsertedAdults = int.Parse(UiAdultsTextBox.Text);
            flightConditions.InsertedChildren = int.Parse(UiChildrenTextBox.Text);
            flightConditions.InsertedInfants = int.Parse(UiInfantsTextBox.Text);
            flightConditions.InsertedSeniors = int.Parse(UiSeniorsTextBox.Text);

            //Local DataBase check, first
            List<Flights> fetchedFlights = dbAccess.FetchFlights(flightConditions);
            if (fetchedFlights.Count == 0)
            {
                //Fetching from the Amadeus Web Service
                TableFillingWithWebServiceData(tf.StartNew(() => GetDataFromAmadeusWebService(accessToken)).Result.Result);
            }
            else
            {
                //Fetching from a local database
                TableFillingWithDatabaseData(fetchedFlights);

            }



        }

        private void UiOriginSearchTextBox_TextChanged(object sender, EventArgs e)
        {
            UiOriginIataCodeGridView.DataSource = null;
            UiOriginIataCodeGridView.DataSource = dbAccess.FetchSpecificIataCodes(UiOriginSearchTextBox.Text);
        }
    }
}
