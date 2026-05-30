#nullable enable
using System;
using System.Collections.Generic;
using DataAccessLayer.Entities;

namespace BusinessLayer.Mapping
{
    /// <summary>
    /// Small container produced by FGMapper containing the mapped EF entities to persist.
    /// The service can further dedupe or link these into the DbContext as needed.
    /// </summary>
    public class FGImportResult
    {
        public FGImportResult()
        {
            SubjectClasses = new List<SubjectClass>();
            Students = new List<Student>();
            Components = new List<GradingComponent>();
            Marks = new List<Mark>();
            Sessions = new List<Session>();
            Snapshots = new List<Snapshot>();
        }

        public List<SubjectClass> SubjectClasses { get; }
        public List<Student> Students { get; }
        public List<GradingComponent> Components { get; }
        public List<Mark> Marks { get; }
        public List<Session> Sessions { get; }
        public List<Snapshot> Snapshots { get; }
    }
}
