using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIO.Core.DTOs.StudentDTO;
using MIO.Core.DTOs.StudentDtos;
using MIO.Core.Entities;
using MIO.Core.Interfaces;
using MIO.Infrastructure.Services;

namespace MIO.API.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    [Authorize] 
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly EmailService _emailService;

        public StudentController(IStudentService studentService, EmailService emailService)
        {
            _studentService = studentService;
            _emailService = emailService;
        }

            [HttpPost]
            [Route("")]
            public async Task<ActionResult> CreateStudent(CreateStudentDTO studentDTO)
            {
              var student=  await _studentService.CreateStudent(studentDTO);
                return new OkObjectResult(new
                {
                    message = "Student Created successfully",
                    userId = student.Id
                });
           
            }

        [HttpGet]
        [Route("")]
        public async Task<ActionResult<IEnumerable<StudentResponseDTO>>> GetStudents()
        {
            var records = await _studentService.GetStudentsService();
            return Ok(records);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult> GetStudentById(int id)
        {
            var student = await _studentService.GetStudentByIDService(id);
            return Ok(student);
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<ActionResult> UpdateStudent(int id, UpdateStudentDTO studentDTO)
        {
            if (studentDTO == null)
                return BadRequest("Student data cannot be null");

            if (id <= 0)
                return BadRequest("Invalid student ID");

            var updatedStudent = await _studentService.UpdateStudentService(id, studentDTO);

            if (updatedStudent == null)
                return NotFound($"Student with ID {id} not found");

            return Ok(new
            {
                message = "Student updated successfully",
                studentId = updatedStudent.Id
            });
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<ActionResult> DeleteStudent(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid student ID");

            var deletedStudent = await _studentService.DeleteStudentService(id);


            if (deletedStudent == null)
                return NotFound($"Student with ID {id} not found");

            return Ok(new
            {
                message = "Student deleted successfully",
                studentId = deletedStudent.Id
            });
        }

        

        [HttpGet]
        [Route("accepted")]
        public async Task<ActionResult<IEnumerable<StudentResponseDTO>>> GetAcceptedStudents()
        {
            var acceptedStudents = await _studentService.GetAcceptedStudentsService();
            return Ok(acceptedStudents);
        }

        [HttpGet]
        [Route("city/{city}")]
        public async Task<ActionResult<IEnumerable<StudentResponseDTO>>> GetStudentsByCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest("City name cannot be empty");

            var students = await _studentService.GetStudentsByCityService(city);
            return Ok(students);
        }

        [HttpPatch]
        [Route("{id}/accept")]
        public async Task<ActionResult> AcceptStudent(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid student ID");

            var acceptedStudent = await _studentService.AcceptStudentService(id);

            if (acceptedStudent == null)
                return NotFound($"Student with ID {id} not found");

           

            return Ok(new
            {
                message = "Student accepted successfully",
                studentId = acceptedStudent.Id
            });
        }
    }
}