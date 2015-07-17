﻿using System.Collections.Specialized;

namespace FileHandlerApp2.Models
{
    public class FileHandlerActivationParameters
    {
        public FileHandlerActivationParameters(NameValueCollection collection)
        {
            this.ResourceId = collection["resourceId"];
            this.CultureName = collection["cultureName"];
            this.FileGet = collection["fileGet"];
            this.FilePut = collection["filePut"];
            this.FileId = collection["fileId"];
            this.Client = collection["client"];
        }

        public string ResourceId { get; set; }
        public string CultureName { get; set; }
        public string FileGet { get; set; }
        public string FilePut { get; set; }
        public string FileId { get; set; }
        public string Client { get; set; }
    }
}