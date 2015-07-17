using FileHandlerApp2.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using FileHandlerApp2.Utils;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace FileHandlerApp2.Controllers
{
    [Authorize]
    public class FileHandlerController : Controller
    {
        public async Task<ActionResult> Preview()
        {
            FileHandlerActivationParameters activationParameters = LoadActivationParameters();

            if (string.IsNullOrWhiteSpace(activationParameters.ResourceId)
                || string.IsNullOrWhiteSpace(activationParameters.FileGet))
            {
                return View(new FileHandlerModel(activationParameters, null, new MvcHtmlString("Required parameters (resourceId and fileGet) are missing")));
            }

            return View(await GetModel(activationParameters, false));
        }

        public async Task<ActionResult> Open()
        {
            FileHandlerActivationParameters activationParameters = LoadActivationParameters();

            if (string.IsNullOrWhiteSpace(activationParameters.ResourceId)
                || string.IsNullOrWhiteSpace(activationParameters.FileGet)
                || string.IsNullOrWhiteSpace(activationParameters.FilePut))
            {
                return View(new FileHandlerModel(activationParameters, null, new MvcHtmlString("Required parameters (resourceId, fileGet and filePut) are missing")));
            }

            return View(await GetModel(activationParameters, true));
        }

        private async Task<FileHandlerModel> GetModel(FileHandlerActivationParameters activationParameters, bool shouldUpdateFile)
        {
            // Acquire access token
            string accessToken = null;
            try
            {
                accessToken = await GetAccessToken(activationParameters.ResourceId);
            }
            catch (Exception exception)
            {
                return new FileHandlerModel(activationParameters, null, new MvcHtmlString("Error acquiring access token. Exception: " + exception.ToString()));
            }

            if (shouldUpdateFile)
            {
                string originalFileContent = null;
                // Get file content
                try
                {
                    originalFileContent = GetFileContent(activationParameters.FileGet, accessToken);
                }
                catch (Exception exception)
                {
                    return new FileHandlerModel(activationParameters, null, new MvcHtmlString("Error reading file first time. Exception: " + exception.ToString()));
                }

                // Update file content
                try
                {
                    UpdateFile(activationParameters.FilePut, accessToken, originalFileContent);
                }
                catch (Exception exception)
                {
                    return new FileHandlerModel(activationParameters, null, new MvcHtmlString("Error writing to file. Exception: " + exception.ToString()));
                }
            }

            // Get file content
            string updatedFileContent = null;
            try
            {
                updatedFileContent = GetFileContent(activationParameters.FileGet, accessToken);
            }
            catch (Exception exception)
            {
                return new FileHandlerModel(activationParameters, null, new MvcHtmlString("Error reading file second time. Exception: " + exception.ToString()));
            }

            return new FileHandlerModel(activationParameters, updatedFileContent, null);
        }

        private FileHandlerActivationParameters LoadActivationParameters()
        {
            FileHandlerActivationParameters activationParameters = null;

            if (Request.Form != null && Request.Form.AllKeys.Count<string>() != 0)
            {
                // Get from current request's form data
                activationParameters = new FileHandlerActivationParameters(Request.Form);
            }
            else
            {
                // If form data does not exist, it must be because of the sign in redirection. 
                // Read the cookie we saved before the redirection in RedirectToIdentityProvider callback in Startup.Auth.cs 
                activationParameters = new FileHandlerActivationParameters(CookieStorage.Load());
                // Clear the cookie after using it
                CookieStorage.Clear();
            }

            return activationParameters;
        }

        private async Task<string> GetAccessToken(string resourceId)
        {
            string accessToken = null;
            AuthenticationContext authContext = null;
            try
            {
                var signInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
                var userObjectId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                var tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

                authContext = new AuthenticationContext(string.Format("{0}/{1}", SettingsHelper.AuthorizationUri, tenantId), new ADALTokenCache(signInUserId));
                AuthenticationResult authResult = await authContext.AcquireTokenSilentAsync(resourceId, new ClientCredential(SettingsHelper.ClientId, SettingsHelper.AppKey), new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
                accessToken = authResult.AccessToken;

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new AuthenticationException("access token is null");
                }
            }
            catch (Exception)
            {
                authContext.TokenCache.Clear();
                throw;
            }

            return accessToken;
        }

        private string GetFileContent(string fileGet, string accessToken)
        {
            HttpWebRequest fileGetRequest = (HttpWebRequest)WebRequest.Create(fileGet);
            fileGetRequest.Host = new Uri(fileGet).Host;
            fileGetRequest.Headers["Authorization"] = "Bearer " + accessToken;
            fileGetRequest.Method = "Get";
            fileGetRequest.AllowAutoRedirect = false;

            HttpWebResponse fileGetResponse = (HttpWebResponse)fileGetRequest.GetResponse();
            if (fileGetResponse.StatusCode == HttpStatusCode.OK)
            {
                return new StreamReader(fileGetResponse.GetResponseStream()).ReadToEnd();
            }
            else
            {
                throw new WebException("Expected OK status code, but got " + fileGetResponse.StatusCode.ToString());
            }
        }

        private void UpdateFile(string filePut, string accessToken, string fileContent)
        {
            int number = 0;
            if (int.TryParse(fileContent, out number))
            {
                number++;
            }

            HttpWebRequest filePutRequest = (HttpWebRequest)WebRequest.Create(filePut);
            filePutRequest.Host = new Uri(filePut).Host;
            filePutRequest.Headers["Authorization"] = "Bearer " + accessToken;
            filePutRequest.Method = "Post";
            filePutRequest.AllowAutoRedirect = false;

            byte[] bytes = Encoding.ASCII.GetBytes(number.ToString());
            filePutRequest.ContentLength = bytes.Length;

            Stream oStreamOut = filePutRequest.GetRequestStream();
            oStreamOut.Write(bytes, 0, bytes.Length);
            oStreamOut.Close();

            HttpWebResponse filePutResponse = (HttpWebResponse)filePutRequest.GetResponse();
            if (filePutResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException("Expected OK status code, but got " + filePutResponse.StatusCode.ToString());
            }
        }
    }
}