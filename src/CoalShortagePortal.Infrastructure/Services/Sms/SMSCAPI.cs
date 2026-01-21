using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CoalShortagePortal.Infrastructure.Services.Sms
{
    public class SMSCAPI
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "http://www.smscountry.com/SMSCwebservice_bulk.aspx";

        // Constructor for dependency injection (recommended)
        public SMSCAPI(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Default constructor for backward compatibility
        public SMSCAPI()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        // Original synchronous methods - keep same signatures for backward compatibility
        public string SendSMS(string user, string password, string mobileNumber, string message)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "User", user },
                    { "passwd", password },
                    { "mobilenumber", mobileNumber },
                    { "message", message }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();

                response.EnsureSuccessStatusCode();

                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string SendSMS(string user, string password, string mobileNumber, string message, string mType)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "User", user },
                    { "passwd", password },
                    { "mobilenumber", mobileNumber },
                    { "message", message },
                    { "MType", mType }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();

                response.EnsureSuccessStatusCode();

                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string SendSMS(string user, string password, string mobileNumber, string message, string mType, string dr)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "User", user },
                    { "passwd", password },
                    { "mobilenumber", mobileNumber },
                    { "message", message },
                    { "MType", mType },
                    { "DR", dr }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();

                response.EnsureSuccessStatusCode();

                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string SendSMS(string user, string password, string mobileNumber, string message, string mType, string dr, string sid)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "User", user },
                    { "passwd", password },
                    { "mobilenumber", mobileNumber },
                    { "message", message },
                    { "MType", mType },
                    { "DR", dr },
                    { "SID", sid }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();

                response.EnsureSuccessStatusCode();

                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        // Async versions (optional - use these for better performance in async contexts)
        public async Task<string> SendSMSAsync(string user, string password, string mobileNumber, string message)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "User", user },
                    { "passwd", password },
                    { "mobilenumber", mobileNumber },
                    { "message", message }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync(_apiUrl, content);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> SendSMSAsync(string user, string password, string mobileNumber, string message, string mType)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "User", user },
                    { "passwd", password },
                    { "mobilenumber", mobileNumber },
                    { "message", message },
                    { "MType", mType }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync(_apiUrl, content);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> SendSMSAsync(string user, string password, string mobileNumber, string message, string mType, string dr)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "User", user },
                    { "passwd", password },
                    { "mobilenumber", mobileNumber },
                    { "message", message },
                    { "MType", mType },
                    { "DR", dr }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync(_apiUrl, content);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> SendSMSAsync(string user, string password, string mobileNumber, string message, string mType, string dr, string sid)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "User", user },
                    { "passwd", password },
                    { "mobilenumber", mobileNumber },
                    { "message", message },
                    { "MType", mType },
                    { "DR", dr },
                    { "SID", sid }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync(_apiUrl, content);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}