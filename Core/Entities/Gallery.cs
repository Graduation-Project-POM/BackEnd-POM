using MIO.Core.Entities;

namespace MIO2.Core.Entities
{
    public class Gallery
    {
        public int Id { get; set; }
        public string Url { get; set; } = null!;
        public string FileKey { get; set; } 
        public string? Title { get; set; }
        public char Type { get; set; }
        public string? Description { get; set; }
        public int UploadedByID { get; set; }
        public User User { get; set; }
        public string RefType { get; set; }
        public int RefID { get; set; }  

        public bool IsDeleted { get; set; }=false;

    }
}
