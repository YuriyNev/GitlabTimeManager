using System.Net.Http.Headers;

namespace GitLabTimeManager.Services
{
    public static class BasicAuthentication
    {
        public static AuthenticationHeaderValue GetAuthenticationHeaderValue(this string token)
        {
            return new AuthenticationHeaderValue("Bearer", token);
        }
    }
}