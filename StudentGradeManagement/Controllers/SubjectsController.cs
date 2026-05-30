using BusinessLayer.DTOs;
using BusinessLayer.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
        [ProducesResponseType(typeof(List<SubjectListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _subjectService.GetAllAsync(cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Lấy bảng điểm của một môn/lớp cụ thể.
        /// </summary>
        [HttpGet("{subject}/{class}")]
        [ProducesResponseType(typeof(SubjectGradeTableDto), StatusCodes.Status200OK)]
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

            return Ok(result);
        }
    }
}
