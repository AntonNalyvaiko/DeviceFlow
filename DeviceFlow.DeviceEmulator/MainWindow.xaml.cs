using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using IdentityModel;
using IdentityModel.Client;

namespace DeviceFlow.DeviceEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
            disco = new DiscoveryCache("https://localhost:44311");
        
        }

        IDiscoveryCache disco;
        
        async Task<DeviceAuthorizationResponse> AuthorizationAsync()
        {
            var result = await disco.GetAsync();
            if (result.IsError) throw new Exception(result.Error);

            var client = new HttpClient();
            var response = await client.RequestDeviceAuthorizationAsync(new DeviceAuthorizationRequest
            {
                Address = result.DeviceAuthorizationEndpoint,
                ClientId = "device"
            });

            if (response.IsError) throw new Exception(response.Error);

            TextUserCode.Text = $"user code : {response.UserCode}";
            TextDeviceCode.Text = $"device code : {response.DeviceCode}";
            TextURL.Text = $"URL : {response.VerificationUri}";
            TextURLFull.Text = response.VerificationUriComplete;

            return response;
        }

        private async Task<TokenResponse> RequestTokenAsync(DeviceAuthorizationResponse authorizeResponse)
        {
            var result = await disco.GetAsync();
            if (result.IsError) throw new Exception(result.Error);

            var client = new HttpClient();

            while (true)
            {
                var response = await client.RequestDeviceTokenAsync(new DeviceTokenRequest
                {
                    Address = result.TokenEndpoint,
                    ClientId = "device",
                    DeviceCode = authorizeResponse.DeviceCode
                });

                if (response.IsError)
                {
                    if (response.Error == OidcConstants.TokenErrors.AuthorizationPending ||
                        response.Error == OidcConstants.TokenErrors.SlowDown)
                        await Task.Delay(authorizeResponse.Interval * 1000);
                    else
                        throw new Exception(response.Error);
                }
                else
                    return response;
            }
        }

        async Task CallApiAsync(string token)
        {
            var baseAddress = "https://localhost:44312";
            var client = new HttpClient
            {
                BaseAddress = new Uri(baseAddress)
            };

            client.SetBearerToken(token);
            var response = await client.GetAsync("api/values");
            Textresult.Text ="Result is :"+ await response.Content.ReadAsStringAsync();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var authorizeResponse = await AuthorizationAsync();
            var tokenResponse = await RequestTokenAsync(authorizeResponse);
            await CallApiAsync(tokenResponse.AccessToken);
        }
    }
}
