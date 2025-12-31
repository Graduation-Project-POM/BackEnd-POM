using MIO.Core.DTOs.UserDtos;

namespace MIO.Core.DTOs.TrainerDtos
{
    public class TrainerResponseDTO
    {
        public int Id { get; set; }
        public string Specialization { get; set; }
        public BasicUserDTO BasicUser { get; set; }

    }
}
