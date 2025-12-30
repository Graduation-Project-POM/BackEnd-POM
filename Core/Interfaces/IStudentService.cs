using MIO.Core.DTOs.StudentDTO;
using MIO.Core.DTOs.StudentDtos;
using MIO.Core.Entities;

namespace MIO.Core.Interfaces
{
    public interface IStudentService
    {
        Task CreateStudent(CreateStudentDTO studentDTO);
        Task<StudentResponseDTO?> GetStudentByIDService(int studentId);
        Task<IEnumerable<StudentResponseDTO>> GetStudentsService();
        Task<Student?> UpdateStudentService(int id, UpdateStudentDTO studentDTO);
        Task<Student?> DeleteStudentService(int id);

       
        Task<IEnumerable<StudentResponseDTO>> GetAcceptedStudentsService();
        Task<IEnumerable<StudentResponseDTO>> GetStudentsByCityService(string city);
        Task<StudentResponseDTO?> AcceptStudentService(int studentId);
    }
}