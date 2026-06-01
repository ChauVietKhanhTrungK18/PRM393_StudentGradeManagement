using BusinessLayer.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StudentGradeManagement.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StudentGradeManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubjectsController : ControllerBase
    {
        private readonly ISubjectService _subjectService;

        public SubjectsController(ISubjectService subjectService)
        {
            _subjectService = subjectService
                ?? throw new ArgumentNullException(nameof(subjectService));
        }

        /// <summary>
        /// Lấy danh sách tất cả môn/lớp.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<SubjectListResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _subjectService.GetAllAsync(cancellationToken);

            var response = result.Select(x => new SubjectListResponseDto
            {
                SubjectCode = x.SubjectCode,
                ClassName = x.ClassName
            }).ToList();

            return Ok(response);
        }

        /// <summary>
        /// Lấy bảng điểm của một môn/lớp cụ thể.
        /// </summary>
        [HttpGet("{subject}/{class}")]
        [ProducesResponseType(typeof(SubjectGradeTableResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGradeTable(
            [FromRoute] string subject,
            [FromRoute(Name = "class")] string @class,
            CancellationToken cancellationToken)
        {
            var result = await _subjectService.GetGradeTableAsync(
                subject,
                @class,
                cancellationToken);

            if (result == null)
                return NotFound(new { message = $"Không tìm thấy lớp '{@class}' của môn '{subject}'." });

            var response = new SubjectGradeTableResponseDto
            {
                SubjectCode = result.SubjectCode,
                ClassName = result.ClassName,
                Components = result.Components,
                Students = result.Students.Select(s => new StudentGradeRowResponseDto
                {
                    RollNumber = s.RollNumber,
                    FullName = s.FullName,
                    Comment = s.Comment,
                    Marks = s.Marks
                }).ToList()
            };

            return Ok(response);
        }
    }
}
