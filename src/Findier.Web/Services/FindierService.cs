﻿using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Findier.Core.Utilities.Interfaces;
using Findier.Web.Attributes;
using Findier.Web.Extensions;
using Findier.Web.Http;
using Findier.Web.Requests;
using Findier.Web.Responses;

namespace Findier.Web.Services
{
    public class FindierService : IFindierService
    {
        private readonly ICredentialUtility _credentialUtility;
        private string _accessToken;
        private string _currentUser;

        public FindierService(ICredentialUtility credentialUtility)
        {
            _credentialUtility = credentialUtility;
            var creds = _credentialUtility.GetCredentials("vycel");

            if (creds == null)
            {
                return;
            }

            CurrentUser = creds.Username;
            _accessToken = creds.Password;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentUser
        {
            get
            {
                return _currentUser;
            }
            private set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        public bool IsAuthenticated => _accessToken != null;

        public void Login(string username, string token)
        {
            CurrentUser = username;
            _accessToken = token;
            _credentialUtility.SaveCredentials("vycel", username, token);
        }

        public async Task<RestResponse<OAuthData>> LoginAsync(OAuthRequest request)
        {
            request.Header("Accept-Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
            var response = await request.ToResponseAsync();
            if (response.IsSuccessStatusCode)
            {
                Login(response.DeserializedResponse.Username, response.DeserializedResponse.AccessToken);
            }
            return response;
        }

        public void Logout()
        {
            CurrentUser = null;
            _accessToken = null;
            _credentialUtility.DeleteCredentials("vycel");
        }

        public async Task<RestResponse<OAuthData>> RegisterAsync(CreateUserRequest request)
        {
            request.Header("Accept-Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
            var response = await request.ToResponseAsync();
            if (response.IsSuccessStatusCode)
            {
                Login(response.DeserializedResponse.Username, response.DeserializedResponse.AccessToken);
            }
            return response;
        }

        public Task<RestResponse<FindierResponse<TT>>> SendAsync<T, TT>(T request) where T : FindierBaseRequest<TT>
        {
            if (typeof (T).GetTypeInfo().GetCustomAttribute<IncludeGeoLocationAttribute>() != null)
            {
                // TODO: use gps
                request.Header(FindierConstants.GeoHeader, "18.376629, -66.176630");
            }

            if (IsAuthenticated)
            {
                request.Header("Authorization", "Bearer " + _accessToken);
            }
            else if (request.Headers.ContainsKey("Authorization"))
            {
                request.Headers.Remove("Authorization");
            }

            request.Header("Accept-Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

            return request.ToResponseAsync();
        }

        public Task<RestResponse<FindierResponse>> SendAsync<T>(T request) where T : FindierBaseRequest
        {
            if (typeof (T).GetTypeInfo().GetCustomAttribute<IncludeGeoLocationAttribute>() != null)
            {
                // TODO: use gps
                request.Header(FindierConstants.GeoHeader, "18.376629, -66.176630");
            }

            if (IsAuthenticated)
            {
                request.Header("Authorization", "Bearer " + _accessToken);
            }
            else if (request.Headers.ContainsKey("Authorization"))
            {
                request.Headers.Remove("Authorization");
            }

            request.Header("Accept-Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

            return request.ToResponseAsync();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}