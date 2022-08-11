using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.admin
{
    public class CreateCategoryRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string SealColor { get; set; }
        public string Icon { get; set; }
        public string IconFileUrl { get; set; }
        public string IconFileThumbnailUrl { get; set; }
        public string WebInactiveIconFileUrl { get; set; }
        public string WebInactiveIconFileThumbnailUrl { get; set; }
        public string WebActiveIconFileUrl { get; set; }
        public string WebActiveIconFileThumbnailUrl { get; set; }
        public string Animation { get; set; }
        public string GifFileUrl { get; set; }
        public string JsonFileUrl { get; set; }
        public DateTime? EventDate { get; set; }
        public List<MusicFile> MusicFiles { get; set; }
    }

    public class MusicFile
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Mp3FileUrl { get; set; }
        public string WavFileUrl { get; set; }
    }
}
