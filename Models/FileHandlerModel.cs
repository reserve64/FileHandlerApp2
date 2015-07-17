using System.Web.Mvc;

namespace FileHandlerApp2.Models
{
    public class FileHandlerModel
    {
        public FileHandlerModel(FileHandlerActivationParameters activationParameters, string fileContent, MvcHtmlString errorMessage)
        {
            this.ActivationParameters = activationParameters;
            this.FileContent = fileContent;
            this.ErrorMessage = errorMessage;
        }

        public FileHandlerActivationParameters ActivationParameters { get; set; }
        public string FileContent { get; set; }
        public MvcHtmlString ErrorMessage { get; set; }
    }
}