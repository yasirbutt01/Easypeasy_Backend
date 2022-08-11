using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Common
{
    public class General<T>
    {
        public T Id { get; set; }
        public string Name { get; set; }
    }
}
