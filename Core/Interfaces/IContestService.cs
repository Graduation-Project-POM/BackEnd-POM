using MIO.Core.DTOs.ContestDTOs;
using MIO2.Core.Entities;

namespace MIO2.Core.Interfaces;

public interface IContestService
{
     Task<Contest> CreateContest(createContestDTO createContestDTO);
     Task<AddStudentsToContestResponseDTO> AddStudentToContest(int contestId, AddStudentsToContestDTO dto);
     Task<List<ContestDTO>> GetContests();
     Task<ContestDTO> GetContestById(int contestId);
}