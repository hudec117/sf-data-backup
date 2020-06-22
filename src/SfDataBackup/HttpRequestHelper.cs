using System;
using System.Net;
using System.Net.Http;

namespace SfDataBackup
{
    public static class HttpRequestHelper
    {
        public static HttpRequestMessage CreateRequestWithSalesforceCookie(Uri requestUrl, string orgId, string accessToken)
        {
            var cookieContainer = new CookieContainer();
            var oidCookie = new Cookie("oid", orgId);
            var sidCookie = new Cookie("sid", accessToken);
            cookieContainer.Add(requestUrl, oidCookie);
            cookieContainer.Add(requestUrl, sidCookie);

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Cookie", cookieContainer.GetCookieHeader(requestUrl));

            return request;
        }
    }
}