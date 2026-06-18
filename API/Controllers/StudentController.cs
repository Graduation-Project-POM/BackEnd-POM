using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIO.Core.DTOs.StudentDTO;
using MIO.Core.DTOs.StudentDtos;
using MIO.Core.Entities;
using MIO.Core.Interfaces;
using MIO.Infrastructure.Services;
using MIO2.Core.DTOs.StudentDtos;

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
            [Authorize(Roles ="Admin")]
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
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult<IEnumerable<StudentResponseDTO>>> GetStudents()
        {
            var records = await _studentService.GetStudentsService();
            return Ok(records);
        }

        [HttpGet]
        [Route("me")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<StudentResponseDTO>> GetMyProfile()
        {
            return Ok(await _studentService.GetMyStudentService());
        }

        [HttpPut]
        [Route("me")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult> UpdateMyProfile(UpdateStudentDTO studentDTO)
        {
            if (studentDTO == null)
                return BadRequest("Student data cannot be null");

            var updated = await _studentService.UpdateMyStudentService(studentDTO);
            return Ok(new
            {
                message = "Profile updated successfully",
                studentId = updated!.Id
            });
        }

        [HttpGet]
        [Route("{id}")]
        [Authorize(Policy = "StudentOwnerOrAdminOrTrainer")]
        public async Task<ActionResult> GetStudentById(int id)
        {
            var student = await _studentService.GetStudentByIDService(id);
            return Ok(student);
        }

        [HttpPost("batch")]
        public async Task<IActionResult> GetMultipleStudents([FromBody] GetMultipleStudentsDTO dto)
        {
            var result = await _studentService.GetMultipleStudentsService(dto.Ids);
            return Ok(result);
        }
        
        [HttpPut]
        [Route("{id}")]
        [Authorize(Roles ="Admin")]
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
        [Authorize(Roles ="Admin")]
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
        [Authorize(Roles ="Admin,Trainer")]
        public async Task<ActionResult<IEnumerable<StudentResponseDTO>>> GetAcceptedStudents()
        {
            var acceptedStudents = await _studentService.GetAcceptedStudentsService();
            return Ok(acceptedStudents);
        }

        [HttpGet]
        [Route("city/{city}")]
        [Authorize(Roles ="Admin,Trainer")]
        public async Task<ActionResult<IEnumerable<StudentResponseDTO>>> GetStudentsByCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest("City name cannot be empty");

            var students = await _studentService.GetStudentsByCityService(city);
            return Ok(students);
        }

        [HttpPatch]
        [Route("{id}/accept")]
        [Authorize(Roles ="Admin")]
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