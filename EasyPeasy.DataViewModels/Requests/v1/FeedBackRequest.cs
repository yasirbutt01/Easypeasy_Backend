using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests
{
    public class FeedBackRequest
    {
        public string Text { get; set; } = "";
        public  int Rating { get; set; }
        public string AppVersion { get; set; } = "";
        public string ApiVersion { get; set; } = "";
        public int OsType { get; set; }
    }
}
