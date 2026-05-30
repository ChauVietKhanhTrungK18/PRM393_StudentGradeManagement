#nullable enable

using System;
using System.Collections.Generic;
using FuGradeLib;
using DataAccessLayer.Entities;

namespace BusinessLayer.Mapping
{
    public class FGMapper
    {
        public FGImportResult MapTeacherGrade(
            TeacherGrade teacherGrade,
            string sourcePath)
        {
            if (teacherGrade == null)
                throw new ArgumentNullException(nameof(teacherGrade));

            var result = new FGImportResult();

            // Session
            result.Sessions.Add(new Session
            {
                FGPath = sourcePath,
                TeacherName = teacherGrade.Login ?? string.Empty,
                Semester = teacherGrade.Semester ?? string.Empty,
                OpenedAt = DateTimeOffset.UtcNow
            });

            if (teacherGrade.SubjectClassGrades == null)
                return result;

            foreach (var fgClass in teacherGrade.SubjectClassGrades)
            {
                var subjectClass = new SubjectClass
                {
                    SubjectCode = fgClass.Subject ?? string.Empty,
                    ClassName = fgClass.Class ?? string.Empty
                };

                result.SubjectClasses.Add(subjectClass);

                var componentLookup =
                    new Dictionary<string, GradingComponent>(
                        StringComparer.OrdinalIgnoreCase);

                // Components
                if (fgClass.Components != null)
                {
                    foreach (var componentName in fgClass.Components)
                    {
                        if (string.IsNullOrWhiteSpace(componentName))
                            continue;

                        var component = new GradingComponent
                        {
                            SubjectClass = subjectClass,
                            Name = componentName,

                            // FuGradeLib không có các thông tin này
                            MaxMark = 0,
                            Weight = 0,
                            IsCondition = false
                        };

                        result.Components.Add(component);

                        // tránh duplicate key
                        if (!componentLookup.ContainsKey(componentName))
                        {
                            componentLookup.Add(
                                componentName,
                                component);
                        }
                    }
                }

                // Students
                if (fgClass.Students == null)
                    continue;

                foreach (var fgStudent in fgClass.Students)
                {
                    var student = new DataAccessLayer.Entities.Student
                    {
                        SubjectClass = subjectClass,
                        RollNumber = fgStudent.Roll ?? string.Empty,
                        FullName = fgStudent.Name ?? string.Empty,
                        Comment = fgStudent.Comment
                    };

                    result.Students.Add(student);

                    if (fgStudent.Grades == null)
                        continue;

                    foreach (var fgGrade in fgStudent.Grades)
                    {
                        if (string.IsNullOrWhiteSpace(fgGrade.Component))
                            continue;

                        if (!componentLookup.TryGetValue(
                                fgGrade.Component,
                                out var component))
                        {
                            continue;
                        }

                        var mark = new Mark
                        {
                            Student = student,
                            Component = component,
                            Value = (decimal)(fgGrade.Grade ?? 0),
                            Comment = null
                        };

                        result.Marks.Add(mark);
                    }
                }
            }

            return result;
        }
    }
}