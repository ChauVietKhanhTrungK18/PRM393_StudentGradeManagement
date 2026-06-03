using BusinessLayer.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApiMarkBulkUpdateRequestDto = StudentGradeManagement.DTOs.MarkBulkUpdateRequestDto;
using ApiMarkBulkUpdateResponseDto = StudentGradeManagement.DTOs.MarkBulkUpdateResponseDto;
using ApiMarkUpdateRequestDto = StudentGradeManagement.DTOs.MarkUpdateRequestDto;
using ApiMarkUpdateResponseDto = StudentGradeManagement.DTOs.MarkUpdateResponseDto;
using ServiceMarkBulkUpdateRequestDto = BusinessLayer.DTOs.MarkBulkUpdateRequestDto;
using ServiceMarkClearByNameRequestDto = BusinessLayer.DTOs.MarkClearByNameRequestDto;
using ServiceMarkUpdateRequestDto = BusinessLayer.DTOs.MarkUpdateRequestDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StudentGradeManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarksController : ControllerBase
    {
        private readonly IMarkService _markService;

        public MarksController(IMarkService markService)
        {
            _markService = markService
                ?? throw new ArgumentNullException(nameof(markService));
        }

        

        /// <summary>
        /// Update all marks for a student with full component schema.
        /// </summary>
        [HttpPut("{subjectCode}/{className}/{rollNumber}")]
        //[ProducesResponseType(typeof(ApiMarkBulkUpdateResponseDto), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ApiMarkBulkUpdateResponseDto), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ApiMarkBulkUpdateResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAllMarks(
            [FromRoute] string subjectCode,
            [FromRoute] string className,
            [FromRoute] string rollNumber,
            [FromBody] ApiMarkBulkUpdateRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new ApiMarkBulkUpdateResponseDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Invalid request.",
                    Results = new()
                });
            }

            var result = await _markService.UpdateMarksAsync(
                new ServiceMarkBulkUpdateRequestDto
                {
                    SubjectCode = subjectCode,
                    ClassName = className,
                    RollNumber = rollNumber,
                    Comment = request.Comment,
                    Marks = NormalizeMarks(request.Marks)
                },
                cancellationToken);

            var response = new ApiMarkBulkUpdateResponseDto
            {
                Success = result.Success,
                IsValid = result.IsValid,
                Message = result.Message,
                Results = result.Results.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new DTOs.MarkCellResponseDto
                    {
                        IsValid = kvp.Value.IsValid,
                        Message = kvp.Value.Message,
                        Value = kvp.Value.Value
                    })
            };

            if (!result.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Clear a single mark cell by component name.
        /// </summary>
        [HttpDelete("{subjectCode}/{className}/{rollNumber}/{componentName}")]
        [ProducesResponseType(typeof(ApiMarkUpdateResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMarkUpdateResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiMarkUpdateResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Clear(
            [FromRoute] string subjectCode,
            [FromRoute] string className,
            [FromRoute] string rollNumber,
            [FromRoute] string componentName,
            CancellationToken cancellationToken)
        {
            var result = await _markService.ClearMarkAsync(
                new ServiceMarkClearByNameRequestDto
                {
                    SubjectCode = subjectCode,
                    ClassName = className,
                    RollNumber = rollNumber,
                    ComponentName = componentName
                },
                cancellationToken);

            var response = new ApiMarkUpdateResponseDto
            {
                Success = result.Success,
                IsValid = result.IsValid,
                Message = result.Message,
                Value = result.Value
            };

            if (!result.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        private static Dictionary<string, string?> NormalizeMarks(
            Dictionary<string, JsonElement>? marks)
        {
            var result = new Dictionary<string, string?>(StringComparer.Ordinal);

            if (marks == null)
            {
                return result;
            }

            foreach (var kvp in marks)
            {
                var value = kvp.Value;
                string? normalized;

                switch (value.ValueKind)
                {
                    case JsonValueKind.String:
                        normalized = value.GetString();
                        break;
                    case JsonValueKind.Number:
                        normalized = value.GetRawText();
                        break;
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        normalized = null;
                        break;
                    default:
                        normalized = value.GetRawText();
                        break;
                }

                result[kvp.Key] = normalized;
            }

            return result;
        }
    }
}
