using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Txt.Ui.Helpers;
using Txt.Ui.Models;
using Txt.Ui.Services.HttpClients.Interfaces;
using Txt.Ui.Services.Interfaces;

namespace Txt.Ui.Services;

public class AuthService(
    IPublicClientService publicClientService,
    ILocalStorageService localStorage,
    NavigationManager navigationManager,
    IServiceProvider serviceProvider
    ) : IAuthService
{
    private HttpClient HttpClient { get; init; } = publicClientService.HttpClient;

    public async Task LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.PostAsJsonAsync("/authorization/login", new
        {
            email,
            password,
        }, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken);
            if (result == null)
            {
                return;
            }

            _ = SaveAndNotifySession(result, cancellationToken);

            var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
            var queryStrings = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var returnUrl = queryStrings.Get("returnUrl");

            if (!string.IsNullOrEmpty(returnUrl))
            {
                var returnUri = new Uri(returnUrl);
                navigationManager.NavigateTo(returnUri.AbsolutePath);
            }
            else
            {
                navigationManager.NavigateTo("/");
            }
        }

    }
    public async Task RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.PostAsJsonAsync("/authorization/register", new
        {
            email,
            password,
        }, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            await LoginAsync(email, password, cancellationToken);
        }
    }

    public async Task<string?> RefreshSession(string refreshToken, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.PostAsJsonAsync("/authorization/refresh-token", new
        {
            RefreshToken = refreshToken
        }, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken);
            if (result != null)
            {
                _ = SaveAndNotifySession(result, cancellationToken);

                return result.AccessToken;
            }
        }

        return null;
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
        => localStorage.RemoveItemsAsync(["accessToken", "refreshToken", "expiresOn"], cancellationToken).AsTask();

    public async Task SaveAndNotifySession(AccessTokenResponse accessTokenResponse, CancellationToken cancellationToken = default)
    {
        await localStorage.SetItemAsync("accessToken", accessTokenResponse.AccessToken, cancellationToken);
        await localStorage.SetItemAsync("refreshToken", accessTokenResponse.RefreshToken, cancellationToken);
        await localStorage.SetItemAsync("expiresOn",
        DateTime.Now.Add(TimeSpan.FromSeconds(accessTokenResponse.ExpiresIn)), cancellationToken);

        var authStateProvider = (AuthenticationStateProvider?)serviceProvider.GetService(typeof(Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider));
        authStateProvider?.NotifyUserAuthentication();
    }
}