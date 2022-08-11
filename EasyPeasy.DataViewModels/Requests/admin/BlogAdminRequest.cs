using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.admin
{
  public  class BlogAdminRequest
    {
       

        public Guid Id { get; set; }

        public string Title { get; set; }

        public string ImageUrl { get; set; }

        public string ImageThumbnailUrl { get; set; }


        public bool IsLiked { get; set; }

        public string Date { get; set; }
        public DateTime PublishDate { get; set; }
        public string description { get; set; }
        public bool isEnabled { get; set; }

        public string metaTitle { get; set; }
        public string metaDescription { get; set; }
        public string slugUrl { get; set; }

    }
}
