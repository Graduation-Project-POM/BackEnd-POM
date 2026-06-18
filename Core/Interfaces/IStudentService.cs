using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIO.Core.DTOs.StudentDTO;
using MIO.Core.DTOs.StudentDtos;
using MIO.Core.Entities;
using MIO2.Core.DTOs.StudentDtos;

namespace MIO.Core.Interfaces
{
    public interface IStudentService
    {
        Task<Student?> CreateStudent(CreateStudentDTO studentDTO);
        Task<StudentResponseDTO?> GetStudentByIDService(int studentId);
        Task<IEnumerable<StudentResponseDTO>> GetStudentsService();
        Task<MultipleStudentsResponseDTO> GetMultipleStudentsService(IEnumerable<int> ids);
        Task<Student?> UpdateStudentService(int id, UpdateStudentDTO studentDTO);
        Task<Student?> DeleteStudentService(int id);

        Task<StudentResponseDTO?> GetMyStudentService();
        Task<Student?> UpdateMyStudentService(UpdateStudentDTO studentDTO);

       
        Task<IEnumerable<StudentResponseDTO>> GetAcceptedStudentsService();
        Task<IEnumerable<StudentResponseDTO>> GetStudentsByCityService(string city);
        Task<StudentResponseDTO?> AcceptStudentService(int studentId);
    }
}